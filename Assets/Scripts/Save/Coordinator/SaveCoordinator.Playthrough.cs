using System;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    public void BeginNewPlaythrough() 
        => BecomePlaythrough(SaveStamp.NewId(), 0, null);

    // active pointer가 가리키는 저장 파일을 읽어옴.
    // 예) 만약 'active = playthrough-B' -> playthrough-B.json
    public ProgressionResumePoint LoadActiveResumePoint()
    {
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
            ? SaveStamp.NewId()
            : save.PlaythroughId;

        BecomePlaythrough(
            id,
            save.PlaySeconds,
            save.Scenes);

        return new ProgressionResumePoint(
            save.ChapterId,
            save.CurrentEpisodeId,
            save.Stats,
            save.Backlog,
            save.PendingLoad,
            save.ChapterCompleted);
    }
}
