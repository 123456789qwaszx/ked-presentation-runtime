using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // 백로그 항목 → 그 장면 안 라인 좌표. 등장 순번은 그 장면의 백로그 안에서 같은 (노드, 라인)을 센 것 —
    // 롤백 포인트의 occurrence와 같은 좌표계다(둘 다 장면 시작에서 0부터 센다).
    public bool TryMakeLineTarget(in DialogueLogEntry entry, out int sceneIndex, out SaveLineTarget target)
    {
        target = null;
        sceneIndex = FindSceneIndexBySerial(entry.lineSerial);

        if (sceneIndex < 0)
            return false;

        LocalSaveFile current = _localStore.LoadActive();

        if (current?.Backlog == null)
            return false;

        int start = _scenes[sceneIndex].Checkpoint.BacklogSerialStart;
        int occurrence = 0;

        for (int i = 0; i < current.Backlog.Count; i++)
        {
            DialogueLogEntry e = current.Backlog[i];

            if (e.lineSerial < start || e.lineSerial > entry.lineSerial)
                continue;

            if (string.Equals(e.nodeName, entry.nodeName, StringComparison.Ordinal) &&
                string.Equals(e.lineId, entry.lineId, StringComparison.Ordinal))
            {
                occurrence++;
            }
        }

        if (occurrence == 0)
            return false;

        target = new SaveLineTarget { NodeName = entry.nodeName, LineId = entry.lineId, Occurrence = occurrence };
        return true;
    }

    // ── 갈라지기 ─────────────────────────────────────────────────────────────

    // 백로그 순번이 속한 장면 기록. 현재 장면(아직 기록 전)이나 다른 챕터면 -1.
    public int FindSceneIndexBySerial(int lineSerial)
    {
        for (int i = 0; i < _scenes.Count; i++)
        {
            SceneCheckpoint checkpoint = _scenes[i].Checkpoint;

            if (lineSerial >= checkpoint.BacklogSerialStart && lineSerial < _scenes[i].BacklogSerialEnd)
                return i;
        }

        return -1;
    }

    public bool CanForkTo(int lineSerial) => FindSceneIndexBySerial(lineSerial) >= 0;

    // 이력의 장면 기록 하나를 물려받아 새 회차를 쓰고 활성으로 세운다. 호출자는 드라이버를 멈춘 뒤
    // 부르고, 그 뒤 런처를 다시 띄운다 — 재개 경로가 새 회차를 그 장면 루트에서 연다.
    //
    // target이 있으면 그 장면의 경로·Yarn 선택과 함께 로드 계획(PendingLoad)으로 실린다 — 첫 장면이
    // 루트에서 표적까지 달린다. 물려받는 것: 그 장면 앞까지의 기록·백로그·누적 시간. 옛 회차 파일과 큐는 그대로.
    //
    // 갈라지기 전에 옛 회차 큐를 한 번 비워 본다(best-effort) — 부모의 마지막 장면 이력이 서버에 먼저 닿도록.
    // 실패해도 갈라진다. 남은 것은 시작 시 큐 순회가 살린다.
    public async Task ForkFromScene(int sceneIndex, SaveLineTarget target = null)
    {
        if (sceneIndex < 0 || sceneIndex >= _scenes.Count)
            throw new ArgumentOutOfRangeException(nameof(sceneIndex));

        await FlushBeforeForkAsync();

        SceneRecord origin = _scenes[sceneIndex];
        SceneCheckpoint checkpoint = origin.Checkpoint;

        LocalSaveFile current = _localStore.LoadActive();

        var backlog = new List<DialogueLogEntry>();

        if (current?.Backlog != null)
        {
            for (int i = 0; i < current.Backlog.Count; i++)
            {
                if (current.Backlog[i].lineSerial < checkpoint.BacklogSerialStart)
                    backlog.Add(current.Backlog[i]);
            }
        }

        string fromId = _playthroughId;
        string newId = NewPlaythroughId();

        var file = new LocalSaveFile
        {
            PlaythroughId = newId,
            ForkedFrom = new ForkOrigin { PlaythroughId = fromId, SceneIndex = sceneIndex, Target = target },
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,
            Stats = new Dictionary<string, int>(checkpoint.Stats, StringComparer.Ordinal),
            Variables = checkpoint.Variables,
            ChapterCompleted = false,
            Scenes = _scenes.Take(sceneIndex).ToList(),
            Backlog = backlog,
            PendingLoad = target == null
                ? null
                : new SavedLoadPlan
                {
                    Path = origin.Path.Select(c => new SavedChoice
                        { FromEpisodeId = c.FromEpisodeId, OptionIndex = c.OptionIndex }).ToList(),
                    YarnChoices = new List<VNChoiceRecord>(origin.YarnChoices),
                    Target = target,
                },
            InheritedPlaySeconds = checkpoint.PlaySecondsAtEntry,
            OwnPlaySeconds = 0,
            PlaySeconds = checkpoint.PlaySecondsAtEntry,
            SavedAtUtc = NowUtc(),
        };

        _localStore.Save(file);

        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset();

        Debug.Log(
            $"[저장] 갈라지기 — {fromId} 장면 {sceneIndex}({checkpoint.EpisodeId}) → 새 회차 {newId}. " +
            $"물려받은 기록 {file.Scenes.Count}개, 백로그 {backlog.Count}줄, 시간 {checkpoint.PlaySecondsAtEntry}s" +
            (target == null ? " — 장면 루트에서" : $" — {target.NodeName}/{target.LineId}#{target.Occurrence}까지 달린다"));
    }

    // 즐겨찾기를 물려받아 새 회차로. 출처 회차 파일이 있으면 앞의 장면 기록도 물려받는다.
    // 호출자는 드라이버를 멈춘 뒤 부르고, 그 뒤 런처를 다시 띄운다.
    public async Task ForkFromBookmark(Bookmark bookmark)
    {
        await FlushBeforeForkAsync();

        SceneCheckpoint checkpoint = bookmark.Checkpoint;

        LocalSaveFile origin = string.IsNullOrEmpty(bookmark.PlaythroughId)
            ? null
            : _localStore.LoadPlaythrough(bookmark.PlaythroughId);

        List<SceneRecord> inherited = origin?.Scenes != null
            ? origin.Scenes.Take(Math.Min(bookmark.SceneIndex, origin.Scenes.Count)).ToList()
            : new List<SceneRecord>();

        string newId = NewPlaythroughId();

        var file = new LocalSaveFile
        {
            PlaythroughId = newId,
            ForkedFrom = new ForkOrigin
            {
                PlaythroughId = bookmark.PlaythroughId,
                SceneIndex = bookmark.SceneIndex,
                Target = bookmark.Load?.Target,
            },
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,
            Stats = new Dictionary<string, int>(checkpoint.Stats, StringComparer.Ordinal),
            Variables = checkpoint.Variables,
            ChapterCompleted = false,
            Scenes = inherited,
            Backlog = new List<DialogueLogEntry>(bookmark.Backlog),
            PendingLoad = bookmark.Load,
            InheritedPlaySeconds = bookmark.PlaySecondsAtBookmark,
            OwnPlaySeconds = 0,
            PlaySeconds = bookmark.PlaySecondsAtBookmark,
            SavedAtUtc = NowUtc(),
        };

        _localStore.Save(file);

        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset();

        Debug.Log(
            $"[저장] 즐겨찾기로 갈라지기 — \"{bookmark.Preview}\" → 새 회차 {newId}. " +
            $"물려받은 기록 {inherited.Count}개, 백로그 {file.Backlog.Count}줄, 시간 {bookmark.PlaySecondsAtBookmark}s");
    }
}
