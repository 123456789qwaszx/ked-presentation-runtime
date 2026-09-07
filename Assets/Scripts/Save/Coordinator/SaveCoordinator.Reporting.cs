using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    public void ReportSceneEntered(SceneEntryReport report)
    {
        if (_playthroughId == null)
            BecomePlaythrough(
                NewPlaythroughId(),
                forkedFrom: null,
                inheritedSeconds: 0,
                ownSeconds: 0,
                scenes: null);

        if (_scenes.Count > 0 && !string.Equals(_scenes[^1].Checkpoint.ChapterId, report.ChapterId)) 
            _scenes.Clear();
        
        _currentEntry = new SceneCheckpoint
        {
            ChapterId = report.ChapterId,
            EpisodeId = report.State.CurrentEpisodeId,
            Stats = report.State.Stats.ToDictionary(
                p => p.Key,
                p => p.Value,
                StringComparer.Ordinal),
            Variables = report.Variables,
            BacklogSerialStart = report.BacklogSerialStart,
            LastChoiceSeq = (_active?.NextSeq ?? 1) - 1,
            PlaySecondsAtEntry = TotalSeconds,
            EnteredAtUtc = NowUtc(),
        };

        if (_active == null)
        {
            var initial = new LocalSaveFile
            {
                PlaythroughId = _playthroughId,
                ForkedFrom = _forkedFrom,
                ChapterId = report.ChapterId, 
                CurrentEpisodeId = report.State.CurrentEpisodeId,
                Stats = new Dictionary<string, int>(_currentEntry.Stats),
                Variables = report.Variables,
                SavedAtUtc = NowUtc(),
                InheritedPlaySeconds = _inheritedSeconds,
                OwnPlaySeconds = OwnSeconds,
                PlaySeconds = TotalSeconds,
            };
            _active = _localStore.Create(initial);
            _localStore.SetActive(_playthroughId);
            _newPrepared = false;
            
            if (_server != null)
                _ = _server.RequestSyncAsync(_playthroughId);
        }
    }

    // 장면이 끝나 확정됐을 때 호출
    // [1]SceneRecord 완성
    // [2]LocalSaveFile 저장
    // [3]서버용 변경 기록 + 동기화
    public void ReportSceneCommitted(SceneCommitReport report)
    {
        if (_playthroughId == null)
            throw new InvalidOperationException("장면 진입 전에 회차가 만들어지지 않았다.");

        if (_currentEntry == null)
            throw new InvalidOperationException("장면 진입 보고 없이 장면 커밋이 호출됐다.");
        
        string now = NowUtc();

        var path = new List<SavedChoice>(report.Choices.Count);

        for (int i = 0; i < report.Choices.Count; i++)
        {
            path.Add(new SavedChoice
            {
                FromEpisodeId = report.Choices[i].FromEpisodeId,
                OptionIndex = report.Choices[i].OptionIndex,
            });
        }

        var scenes = new List<SceneRecord>(_scenes);
        scenes.Add(new SceneRecord
        {
            Checkpoint = _currentEntry,
            Path = path,
            YarnChoices = new List<VNChoiceRecord>(report.YarnChoices),
            BacklogSerialEnd = report.BacklogSerialStart,
        });

        int own = OwnSeconds;

        var snapshot = new LocalSaveFile
        {
            PlaythroughId = _playthroughId,
            ForkedFrom = _forkedFrom,
            ChapterId = report.ChapterId,
            CurrentEpisodeId = report.State.CurrentEpisodeId,
            Stats = report.State.Stats.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            Variables = report.Variables,
            ChapterCompleted = report.ChapterCompleted,
            Scenes = scenes,
            Backlog = new List<DialogueLogEntry>(report.Backlog),
            InheritedPlaySeconds = _inheritedSeconds,
            OwnPlaySeconds = own,
            PlaySeconds = _inheritedSeconds + own,
            SavedAtUtc = now,
        };

        var choices = report.Choices.Select(c => new PendingChoice
        {
            EpisodeId = c.FromEpisodeId, OptionIndex = c.OptionIndex, ChosenAt = now,
        }).ToList();
        
        var events = report.WatchedEpisodeIds.Select(id => new PendingEvent
        {
            EpisodeId = id, OccurredAt = now,
        }).ToList();
        
        _active.Commit(snapshot, choices, events);
        _scenes.Clear();
        _scenes.AddRange(scenes);
        _currentEntry = null;

        Debug.Log(
            $"[저장] 장면 확정 - 선택 {report.Choices.Count}, Yarn 선택 {report.YarnChoices.Count}, " +
            $"시청 {report.WatchedEpisodeIds.Count}, [3] {report.Variables?.Count ?? 0}개, 백로그 {report.Backlog.Count}줄, " +
            $"기록 {_scenes.Count}개, 시간 {_inheritedSeconds}+{own}s → {report.State.CurrentEpisodeId}" +
            (report.ChapterCompleted ? " (챕터 완료)" : string.Empty));

        if (_server != null)
            _ = _server.RequestSyncAsync(_playthroughId);
    }
}