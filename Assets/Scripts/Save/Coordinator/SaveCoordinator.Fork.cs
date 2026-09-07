using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#region 용어 설명

// SceneCheckpoint
// = 재시작할 상태
//
// SceneRecord
// = 과거 한 Scene의 완성된 기록
//
// SaveLineTarget
// = Scene 내부의 목적지
//
// SavedLoadPlan
// = 목적지까지 재생할 방법
//
// ForkOrigin
// = 새 회차의 부모 정보
//
// Bookmark
// = 나중에 Fork하기 위해 보존해 둔 복원 패키지
//
// SaveForkTarget
// = 백로그 항목으로부터 해석된 갈라지기 목적지.
//   SceneIndex는 항상 유효하고,
//   LineTarget이 null이면 해당 장면 루트에서 시작한다.

#endregion

public readonly struct SaveForkTarget
{
    public int SceneIndex { get; }
    public SaveLineTarget LineTarget { get; }

    public SaveForkTarget(int sceneIndex, SaveLineTarget lineTarget)
    {
        SceneIndex = sceneIndex;
        LineTarget = lineTarget;
    }
}

public sealed partial class SaveCoordinator
{
    public bool CanForkFrom(in DialogueLogEntry entry) 
        => FindSceneIndexBySerial(entry.lineSerial) >= 0;
    
    public Task ForkFromScene(SaveForkTarget forkTarget) 
        => ForkFromScene(forkTarget.SceneIndex, forkTarget.LineTarget);

    public bool TryResolveForkTarget(in DialogueLogEntry entry, out SaveForkTarget forkTarget)
    {
        forkTarget = default;

        int sceneIndex = FindSceneIndexBySerial(entry.lineSerial);

        if (sceneIndex < 0)
            return false;

        TryMakeLineTarget(entry, sceneIndex, out SaveLineTarget lineTarget);

        forkTarget = new SaveForkTarget(sceneIndex, lineTarget);
        return true;
    }
    
    // 백로그 serial이 속한 완료된 Scene을 찾는다.
    // 현재 Scene처럼 아직 SceneRecord로 확정되지 않았으면 -1.
    private int FindSceneIndexBySerial(int lineSerial)
    {
        for (int i = 0; i < _scenes.Count; i++)
        {
            SceneRecord scene = _scenes[i];
            SceneCheckpoint checkpoint = scene.Checkpoint;

            if (lineSerial >= checkpoint.BacklogSerialStart 
                && lineSerial < scene.BacklogSerialEnd)
                return i;
        }

        return -1;
    }

    // 특정 백로그 한 줄을 Scene 내부의 재생 가능한 라인 좌표로 변환.
    //
    // occurrence는 (NodeName, LineId)가 해당 Scene에서 몇 번째인지.
    // (첫 번째 = 1, 두 번째 = 2, ...)
    private bool TryMakeLineTarget(
        in DialogueLogEntry entry,
        int sceneIndex,
        out SaveLineTarget target)
    {
        target = null;

        LocalSaveFile current = _localStore.LoadActive();

        if (current?.Backlog == null)
            return false;

        int sceneStartSerial =
            _scenes[sceneIndex].Checkpoint.BacklogSerialStart;

        int occurrence = 0;

        for (int i = 0; i < current.Backlog.Count; i++)
        {
            DialogueLogEntry candidate = current.Backlog[i];

            // 이 Scene 이전의 대사와 선택한 대사 이후는 제외.
            if (candidate.lineSerial < sceneStartSerial 
                || candidate.lineSerial > entry.lineSerial)
                continue;

            if (!string.Equals(candidate.nodeName, entry.nodeName, StringComparison.Ordinal)
                || !string.Equals(candidate.lineId, entry.lineId, StringComparison.Ordinal)) 
                continue;

            occurrence++;
        }

        if (occurrence == 0)
            return false;

        target = new SaveLineTarget
        {
            NodeName = entry.nodeName,
            LineId = entry.lineId,
            Occurrence = occurrence,
        };

        return true;
    }

    // 과거 Scene 하나를 출발점으로 새로운 Playthrough를 만든다.
    //
    // 1. 로컬 회차 선택 확정
    // 2. 해당 Scene 진입 Checkpoint 복원
    // 3. 그 Scene 이전 기록만 상속
    // 4. target이 있으면 저장된 선택을 replay
    // 5. 새로운 Playthrough로 저장
    private Task ForkFromScene(
        int sceneIndex,
        SaveLineTarget target = null)
    {
        if (sceneIndex < 0 || sceneIndex >= _scenes.Count)
            throw new ArgumentOutOfRangeException(nameof(sceneIndex));

        _localStore.SelectLocalPlaythrough();

        SceneRecord origin = _scenes[sceneIndex];
        SceneCheckpoint checkpoint = origin.Checkpoint;

        LocalSaveFile current = _localStore.LoadActive();

        List<DialogueLogEntry> inheritedBacklog =
            BuildBacklogBefore(checkpoint.BacklogSerialStart, current);

        string fromId = _playthroughId;
        string newId = NewPlaythroughId();

        var file = new LocalSaveFile
        {
            PlaythroughId = newId,

            ForkedFrom = new ForkOrigin
            {
                PlaythroughId = fromId,
                SceneIndex = sceneIndex,
                Target = target,
            },

            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,

            Stats = new Dictionary<string, int>(
                checkpoint.Stats,
                StringComparer.Ordinal),

            Variables = checkpoint.Variables,

            ChapterCompleted = false,

            // 갈라질 Scene은 새 회차에서 다시 실행하므로
            // 그 이전 Scene까지만 물려받는다.
            Scenes = _scenes.Take(sceneIndex).ToList(),

            Backlog = inheritedBacklog,

            // target이 없으면 Scene 루트부터 일반 플레이.
            // 있으면 기존 선택을 재현해 target까지 이동한다.
            PendingLoad = CreateLoadPlan(origin, target),

            InheritedPlaySeconds = checkpoint.PlaySecondsAtEntry,
            OwnPlaySeconds = 0,
            PlaySeconds = checkpoint.PlaySecondsAtEntry,

            SavedAtUtc = NowUtc(),
        };

        SaveAndActivateFork(file);

        Debug.Log(
            $"[저장] 갈라지기 — " +
            $"{fromId} 장면 {sceneIndex}({checkpoint.EpisodeId}) → 새 회차 {newId}. " +
            $"물려받은 기록 {file.Scenes.Count}개, " +
            $"백로그 {inheritedBacklog.Count}줄, " +
            $"시간 {checkpoint.PlaySecondsAtEntry}s" +
            (target == null
                ? " — 장면 루트에서"
                : $" — {target.NodeName}/{target.LineId}#{target.Occurrence}까지 달린다"));
        return Task.CompletedTask;
    }

    private static List<DialogueLogEntry> BuildBacklogBefore(
        int sceneStartSerial,
        LocalSaveFile current)
    {
        var inherited = new List<DialogueLogEntry>();

        if (current?.Backlog == null)
            return inherited;

        for (int i = 0; i < current.Backlog.Count; i++)
        {
            DialogueLogEntry entry = current.Backlog[i];

            if (entry.lineSerial < sceneStartSerial)
                inherited.Add(entry);
        }

        return inherited;
    }

    private static SavedLoadPlan CreateLoadPlan(
        SceneRecord origin,
        SaveLineTarget target)
    {
        if (target == null)
            return null;

        return new SavedLoadPlan
        {
            Path = origin.Path
                .Select(choice => new SavedChoice
                {
                    FromEpisodeId = choice.FromEpisodeId,
                    OptionIndex = choice.OptionIndex,
                })
                .ToList(),

            YarnChoices =
                new List<VNChoiceRecord>(origin.YarnChoices),

            Target = target,
        };
    }

    // 즐겨찾기에 저장된 복원 정보를 기반으로 새로운 Playthrough를 만든다.
    //
    // 수동 슬롯 자체에 저장된 과거 SceneRecord를 물려받는다.
    public async Task ForkFromBookmark(Bookmark bookmark)
    {
        if (bookmark == null) throw new ArgumentNullException(nameof(bookmark));
        if (bookmark.Id != null)
        {
            string id = bookmark.Id;
            bookmark = _localStore.LoadBookmark(id);
            if (bookmark == null && _restore != null) bookmark = await _restore.HydrateBookmarkAsync(id);
        }
        if (bookmark?.Checkpoint == null)
            throw new InvalidOperationException("수동 저장 내용을 가져올 수 없다. 연결 후 다시 시도해야 한다.");
        _localStore.SelectLocalPlaythrough();

        SceneCheckpoint checkpoint = bookmark.Checkpoint;

        List<SceneRecord> inheritedScenes = PlaythroughSession.Copy(bookmark.Scenes ?? new List<SceneRecord>());

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

            Stats = new Dictionary<string, int>(
                checkpoint.Stats,
                StringComparer.Ordinal),

            Variables = checkpoint.Variables,

            ChapterCompleted = false,

            Scenes = inheritedScenes,
            Backlog = new List<DialogueLogEntry>(bookmark.Backlog),
            PendingLoad = bookmark.Load,

            InheritedPlaySeconds = bookmark.PlaySecondsAtBookmark,
            OwnPlaySeconds = 0,
            PlaySeconds = bookmark.PlaySecondsAtBookmark,

            SavedAtUtc = NowUtc(),
        };

        SaveAndActivateFork(file);

        Debug.Log(
            $"[저장] 즐겨찾기로 갈라지기 — " +
            $"\"{bookmark.Preview}\" → 새 회차 {newId}. " +
            $"물려받은 기록 {inheritedScenes.Count}개, " +
            $"백로그 {file.Backlog.Count}줄, " +
            $"시간 {bookmark.PlaySecondsAtBookmark}s");
    }

    private void SaveAndActivateFork(LocalSaveFile file)
    {
        _localStore.Create(file);
        _localStore.SetActive(file.PlaythroughId);
        BecomePlaythrough(file.PlaythroughId, file.ForkedFrom,
            file.InheritedPlaySeconds, file.OwnPlaySeconds, file.Scenes);
        _newPrepared = false;
        if (_server != null) _ = _server.RequestSyncAsync(file.PlaythroughId);
    }
}
