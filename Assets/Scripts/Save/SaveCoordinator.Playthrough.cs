using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public sealed partial class SaveCoordinator
{
    // 새 회차 준비:
    // - 진행 중인 서버 동기화를 먼저 정리.
    // - 기존 활성 회차는 해제하지만 저장 파일은 보존됨.
    // - 새 PlaythroughId로 전환.
    //
    // 서버의 이전 회차 종료 처리는 아직 지원하지 않음.
    // (현재 쌓이는 중. 지연시간 증가)
    public async Task PrepareNewPlaythroughAsync()
    {
        if (_server != null)
            await _server.FlushAsync();

        _localStore.ClearActive();

        BecomePlaythrough(
            NewPlaythroughId(),
            forkedFrom: null,
            inheritedSeconds: 0,
            ownSeconds: 0,
            scenes: null);
        
        _queue.Reset();

        Debug.Log($"[저장] 새 게임 - 회차 {_playthroughId}");
    }

    // active pointer가 가리키는 저장 파일을 읽어옴.
    // 예) 만약 'active = playthrough-B' -> playthrough-B.json
    public ProgressionResumePoint LoadActiveResumePoint()
    {
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