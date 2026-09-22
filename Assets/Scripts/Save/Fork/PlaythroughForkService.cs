using System;
using System.Collections.Generic;
using System.Linq;
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

// 확정된 과거 지점에서 새 회차 파일을 만들고 active로 세운다.
//
// 여기서 끝이다. 만들어진 회차에 SaveCoordinator를 붙이는 일은
// 곧이어 호출되는 LoadActiveResumePoint()가 한다.
public sealed class PlaythroughForkService
{
    private readonly ILocalSaveStore _store;
    private readonly string _contentVersion;

    public PlaythroughForkService(
        ILocalSaveStore store,
        string contentVersion)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("저장 콘텐츠 버전이 비어 있다.", nameof(contentVersion));

        _contentVersion = contentVersion;
    }

    public bool CanForkFrom(PlaythroughSaveSnapshot playthrough, in DialogueLogEntry entry)
        => FindSceneIndexBySerial(playthrough, entry.lineSequence) >= 0;

    public bool TryResolveForkTarget(
        PlaythroughSaveSnapshot playthrough,
        in DialogueLogEntry entry,
        out SaveForkTarget forkTarget)
    {
        forkTarget = default;

        int sceneIndex = FindSceneIndexBySerial(playthrough, entry.lineSequence);

        if (sceneIndex < 0)
            return false;

        TryMakeLineTarget(playthrough, entry, sceneIndex, out SaveLineTarget lineTarget);

        forkTarget = new SaveForkTarget(sceneIndex, lineTarget);
        return true;
    }

    // 백로그 serial이 속한 완료된 Scene을 찾는다.
    // 현재 Scene처럼 아직 SceneRecord로 확정되지 않았으면 -1.
    private static int FindSceneIndexBySerial(PlaythroughSaveSnapshot playthrough, int lineSerial)
    {
        IReadOnlyList<SceneRecord> scenes = playthrough.Scenes;

        for (int i = 0; i < scenes.Count; i++)
        {
            SceneRecord scene = scenes[i];
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
        PlaythroughSaveSnapshot playthrough,
        in DialogueLogEntry entry,
        int sceneIndex,
        out SaveLineTarget target)
    {
        target = null;

        LocalSaveFile current = _store.LoadActive();

        if (current?.Backlog == null)
            return false;

        int sceneStartSerial =
            playthrough.Scenes[sceneIndex].Checkpoint.BacklogSerialStart;

        int occurrence = 0;

        for (int i = 0; i < current.Backlog.Count; i++)
        {
            DialogueLogEntry candidate = current.Backlog[i];

            // 이 Scene 이전의 대사와 선택한 대사 이후는 제외.
            if (candidate.lineSequence < sceneStartSerial
                || candidate.lineSequence > entry.lineSequence)
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
    // 1. 해당 Scene 진입 Checkpoint 복원
    // 2. 그 Scene 이전 기록만 상속
    // 3. target이 있으면 저장된 선택을 replay
    // 4. 새로운 Playthrough 파일로 저장하고 active로 세움
    public void ForkFromScene(
        PlaythroughSaveSnapshot playthrough,
        SaveForkTarget forkTarget)
    {
        int sceneIndex = forkTarget.SceneIndex;
        SaveLineTarget target = forkTarget.LineTarget;

        if (sceneIndex < 0 || sceneIndex >= playthrough.Scenes.Count)
            throw new ArgumentOutOfRangeException(nameof(forkTarget));

        SceneRecord origin = playthrough.Scenes[sceneIndex];
        SceneCheckpoint checkpoint = origin.Checkpoint;

        LocalSaveFile current = _store.LoadActive();

        List<DialogueLogEntry> inheritedBacklog =
            BuildBacklogBefore(checkpoint.BacklogSerialStart, current);

        string fromId = playthrough.PlaythroughId;
        string newId = SaveStamp.NewId();

        var file = new LocalSaveFile
        {
            PlaythroughId = newId,
            ContentVersion = _contentVersion,
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,

            Stats = new Dictionary<string, int>(
                checkpoint.Stats,
                StringComparer.Ordinal),


            ChapterCompleted = false,

            // 갈라질 Scene은 새 회차에서 다시 실행하므로
            // 그 이전 Scene까지만 물려받는다.
            Scenes = playthrough.Scenes.Take(sceneIndex).ToList(),

            Backlog = inheritedBacklog,

            // target이 없으면 Scene 루트부터 일반 플레이.
            // 있으면 기존 선택을 재현해 target까지 이동한다.
            PendingLoad = CreateLoadPlan(origin, target),

            PlaySeconds = checkpoint.PlaySecondsAtEntry,

            SavedAtUtc = SaveStamp.NowUtc(),
        };

        SaveAndActivate(file);

        Debug.Log(
            $"[저장] 갈라지기 — " +
            $"{fromId} 장면 {sceneIndex}({checkpoint.EpisodeId}) → 새 회차 {newId}. " +
            $"물려받은 기록 {file.Scenes.Count}개, " +
            $"백로그 {inheritedBacklog.Count}줄, " +
            $"시간 {checkpoint.PlaySecondsAtEntry}s" +
            (target == null
                ? " — 장면 루트에서"
                : $" — {target.NodeName}/{target.LineId}#{target.Occurrence}까지 달린다"));
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

            if (entry.lineSequence < sceneStartSerial)
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

    // 수동 슬롯 본문을 독립된 새 회차로 연다. 슬롯 원본은 바꾸지 않는다.
    public void ForkFromSaveSlot(SaveSlotEntry entry, SaveSlotData data)
    {
        SceneCheckpoint checkpoint = data.Checkpoint;

        List<SceneRecord> inheritedScenes =
            PlaythroughSession.Copy(data.Scenes);

        string newId = SaveStamp.NewId();

        var file = new LocalSaveFile
        {
            PlaythroughId = newId,
            ContentVersion = _contentVersion,
            ChapterId = checkpoint.ChapterId,
            CurrentEpisodeId = checkpoint.EpisodeId,

            Stats = new Dictionary<string, int>(
                checkpoint.Stats,
                StringComparer.Ordinal),


            ChapterCompleted = false,

            Scenes = inheritedScenes,
            Backlog = new List<DialogueLogEntry>(data.Backlog),
            PendingLoad = PlaythroughSession.Copy(data.LoadPlan),
            PlaySeconds = data.PlaySeconds,

            SavedAtUtc = SaveStamp.NowUtc(),
        };

        SaveAndActivate(file);

        Debug.Log(
            $"[저장] 수동 슬롯 불러오기 — " +
            $"\"{entry.Preview}\" → 새 회차 {newId}. " +
            $"물려받은 기록 {inheritedScenes.Count}개, " +
            $"백로그 {file.Backlog.Count}줄, " +
            $"시간 {data.PlaySeconds}s");
    }

    private void SaveAndActivate(LocalSaveFile file)
    {
        _store.Create(file);
        _store.SetActive(file.PlaythroughId);
    }
}
