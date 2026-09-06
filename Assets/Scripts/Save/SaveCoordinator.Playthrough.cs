using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public sealed partial class SaveCoordinator
{
    // Flushes pending sync work,
    // then clears the active pointer and starts a fresh playthrough.
    // 옛 회차 파일은 남는다.
    // The previous server-side playthrough remains open
    // until session-ending support is added.
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

    public ProgressionResumePoint LoadActiveResumePoint()
    {
        LocalSaveFile save = _localStore.LoadActive();

        if (save == null)
            return null;

        // 구세이브(회차 id 없음)는 지금 id를 받는다.
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

    // ── 회차 목록 (이력 화면 재료) ────────────────────────────────────────────

    // 보관 중인 회차 요약.
    // 활성 회차와 즐겨찾기가 걸린 회차를 펼치고 나머지는 접는 것은 UI의 일.
    // 여기서는 그 판단에 필요한 것만 준다.
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
                    bookmarks.Items.Count(b 
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