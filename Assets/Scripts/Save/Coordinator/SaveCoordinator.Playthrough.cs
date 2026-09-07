using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

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

    // 회차 목록 (이력 UI용 query)
    // - UI 작성에 필요한 것을 전달.
    // (활성 회차와 즐겨찾기가 걸린 회차를 펼치고 나머지는 접는 것은 UI의 일)
    public IReadOnlyList<PlaythroughSummary> ListPlaythroughs()
    {
        var summaries = new List<PlaythroughSummary>();

        string activeId = _localStore.ActiveId;
        BookmarkFile bookmarks = _localStore.LoadBookmarks();

        foreach (string id in _localStore.ListPlaythroughIds())
        {
            LocalSaveFile file = _localStore.LoadPlaythrough(id);

            if (file == null)
                continue;

            summaries.Add(new PlaythroughSummary
            {
                PlaythroughId = id,
                IsActive = string.Equals(id, activeId, StringComparison.Ordinal),
                ForkedFrom = file.ForkedFrom,
                ChapterId = file.ChapterId,
                CurrentEpisodeId = file.CurrentEpisodeId,
                ChapterCompleted = file.ChapterCompleted,
                SceneCount = file.Scenes?.Count ?? 0,
                BookmarkCount =
                    bookmarks.Bookmarks.Count(b 
                        => string.Equals(b.PlaythroughId, id, StringComparison.Ordinal)),
                InheritedPlaySeconds = file.InheritedPlaySeconds,
                OwnPlaySeconds = file.OwnPlaySeconds,
                SavedAtUtc = file.SavedAtUtc,
            });
        }

        summaries.Sort((a, b)
            => string.CompareOrdinal(b.SavedAtUtc, a.SavedAtUtc));

        return summaries;
    }
}