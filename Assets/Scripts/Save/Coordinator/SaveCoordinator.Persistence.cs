using System;
using System.Collections.Generic;
using System.Linq;
using Ked.Progression;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // Scene 진입 시점에 이미 캡처된 진행 상태와 Backlog 경계를 체크포인트로 확정한다.
    public void EnterScene(
        string chapterId,
        ChapterState entryState,
        int backlogSequenceStart)
    {
        _currentEntry = new SceneCheckpoint
        {
            ChapterId = chapterId,
            EpisodeId = entryState.CurrentEpisodeId,
            Stats = entryState.Stats.ToDictionary(
                p => p.Key,
                p => p.Value,
                StringComparer.Ordinal),
            BacklogSerialStart = backlogSequenceStart,
            PlaySecondsAtEntry = TotalSeconds,
            EnteredAtUtc = NowUtc(),
        };

        if (_playthroughSession == null)
        {
            var initial = new LocalSaveFile
            {
                PlaythroughId = _playthroughId,
                ContentVersion = _contentVersion,
                ChapterId = chapterId,
                CurrentEpisodeId = entryState.CurrentEpisodeId,
                Stats = new Dictionary<string, int>(_currentEntry.Stats),
                SavedAtUtc = NowUtc(),
                PlaySeconds = TotalSeconds,
            };

            PlaythroughSession session =
                _localStore.Open(_playthroughId) 
                ?? _localStore.Create(initial);

            _localStore.SetActive(_playthroughId);
            
            _playthroughSession = session;
        }
    }

    // 정상 Scene 완료 시점에 캡처된 Progression 결과와 Host 기록을 하나의 snapshot으로 확정한다.
    // 디스크 확정 성공 뒤에만 메모리 Scene 기록을 교체한다.
    public void CommitScene(
        string chapterId,
        SceneCommitResult result,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        IReadOnlyList<DialogueLogEntry> backlog,
        int backlogSequenceEnd,
        SceneRunOutcome outcome)
    {
        var path = new List<SavedChoice>(result.Choices.Count);

        for (int i = 0; i < result.Choices.Count; i++)
        {
            path.Add(new SavedChoice
            {
                FromEpisodeId = result.Choices[i].FromEpisodeId,
                OptionIndex = result.Choices[i].OptionIndex,
            });
        }

        var scenes = new List<SceneRecord>(_scenes);
        
        scenes.Add(new SceneRecord
        {
            Checkpoint = _currentEntry,
            Path = path,
            YarnChoices = new List<VNChoiceRecord>(yarnChoices),
            BacklogSerialEnd = backlogSequenceEnd,
        });

        var snapshot = new LocalSaveFile
        {
            PlaythroughId = _playthroughId,
            ContentVersion = _contentVersion,
            ChapterId = chapterId,
            CurrentEpisodeId = result.State.CurrentEpisodeId,
            Stats = result.State.Stats.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            ChapterCompleted = outcome == SceneRunOutcome.ChapterEnded,
            Scenes = scenes,
            Backlog = new List<DialogueLogEntry>(backlog),
            PendingLoad = null,
            PlaySeconds = TotalSeconds,
            SavedAtUtc = NowUtc(),
        };

        _playthroughSession.Commit(snapshot);

        _scenes.Clear();
        _scenes.AddRange(scenes);
        _currentEntry = null;
    }
}
