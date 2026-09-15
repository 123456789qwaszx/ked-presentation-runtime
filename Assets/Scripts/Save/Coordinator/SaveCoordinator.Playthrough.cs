using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // 네트워크 대기 없이 새 회차를 예약한다. 첫 Scene 진입 snapshot을 쓴 뒤 active를 바꾼다.
    public Task PrepareNewPlaythroughAsync()
    {
        if (_newPrepared) 
            return Task.CompletedTask;
        
        BecomePlaythrough(NewPlaythroughId(), 0, null);
        
        _newPrepared = true;
        
        return Task.CompletedTask;
    }

    // active pointer가 가리키는 저장 파일을 읽어옴.
    // 예) 만약 'active = playthrough-B' -> playthrough-B.json
    public ProgressionResumePoint LoadActiveResumePoint()
    {
        if (_newPrepared)
            return null;
        
        LocalSaveFile save = _localStore.LoadActive();

        if (save == null)
            return null;

        // 버전 도입 전의 개발용 저장은 현재 콘텐츠로 한 번 승계한다.
        if (string.IsNullOrEmpty(save.ContentVersion))
            save.ContentVersion = _contentVersion;

        if (!string.Equals(save.ContentVersion, _contentVersion, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                $"[저장] 콘텐츠 버전이 달라 이어하지 않는다: {save.ContentVersion} → {_contentVersion}");
            return null;
        }

        string id = string.IsNullOrEmpty(save.PlaythroughId)
            ? NewPlaythroughId()
            : save.PlaythroughId;

        BecomePlaythrough(
            id,
            save.PlaySeconds,
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
}
