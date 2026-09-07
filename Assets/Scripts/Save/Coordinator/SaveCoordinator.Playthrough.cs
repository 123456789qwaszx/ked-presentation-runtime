using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // 네트워크 대기 없이 새 회차를 예약한다. 첫 Scene 진입 snapshot을 쓴 뒤 active를 바꾼다.
    public Task PrepareNewPlaythroughAsync()
    {
        if (_newPrepared) return Task.CompletedTask;
        _localStore.SelectLocalPlaythrough();
        BecomePlaythrough(NewPlaythroughId(), null, 0, 0, null);
        _newPrepared = true;
        return Task.CompletedTask;
    }

    // active pointer가 가리키는 저장 파일을 읽어옴.
    // 예) 만약 'active = playthrough-B' -> playthrough-B.json
    public ProgressionResumePoint LoadActiveResumePoint()
    {
        if (_newPrepared) return null;
        LocalSaveFile save = _localStore.LoadActive();

        if (save == null)
            return null;

        string id = string.IsNullOrEmpty(save.PlaythroughId)
            ? NewPlaythroughId()
            : save.PlaythroughId;

        int playSeconds = save.InheritedPlaySeconds == 0 
                          && save.OwnPlaySeconds == 0 
            ? save.PlaySeconds
            : save.OwnPlaySeconds;

        BecomePlaythrough(
            id,
            save.ForkedFrom,
            save.InheritedPlaySeconds,
            playSeconds,
            save.Scenes);

        return new ProgressionResumePoint(
            save.ChapterId,
            save.CurrentEpisodeId,
            save.Stats,
            save.Variables,
            save.Backlog,
            save.PendingLoad,
            save.ChapterCompleted);
    }

    public IReadOnlyList<PlaythroughSummary> ListPlaythroughs() => _localStore.ListPlaythroughSummaries();
}
