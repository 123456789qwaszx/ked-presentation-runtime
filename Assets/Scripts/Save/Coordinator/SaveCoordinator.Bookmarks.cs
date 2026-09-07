using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // 북마크:
    // - 단순 즐겨찾기가 아닌,
    // - 재생 가능한 save fragment.(복원 시 필요한 재생 상태를 묶어둠)
    public IReadOnlyList<Bookmark> Bookmarks => _localStore.LoadBookmarks().Bookmarks;

    public Bookmark CreateBookmark(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview,
        string label = null)
    {
        if (_currentEntry == null || target == null)
            return null;

        LocalSaveFile current = _localStore.LoadActive();

        var bookmark = new Bookmark
        {
            Id = NewPlaythroughId(),
            Label = string.IsNullOrEmpty(label) ? preview : label,
            Preview = preview,
            CreatedAtUtc = NowUtc(),
            PlaythroughId = _playthroughId,
            SceneIndex = _scenes.Count,
            ChapterId = _currentEntry.ChapterId,
            Checkpoint = _currentEntry,
            Load = new SavedLoadPlan
            {
                Path = path
                    .Select(c => new SavedChoice { FromEpisodeId = c.FromEpisodeId, OptionIndex = c.OptionIndex })
                    .ToList(),
                YarnChoices = new List<VNChoiceRecord>(yarnChoices),
                Target = target,
            },
            Backlog = current?.Backlog != null
                ? new List<DialogueLogEntry>(current.Backlog)
                : new List<DialogueLogEntry>(),
            PlaySecondsAtBookmark = TotalSeconds,
        };

        BookmarkFile file = _localStore.LoadBookmarks();
        file.Bookmarks.Add(bookmark);
        _localStore.SaveBookmarks(file);

        Debug.Log(
            $"[저장] 즐겨찾기 — \"{bookmark.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {bookmark.Load.Path.Count}개, Yarn 선택 {bookmark.Load.YarnChoices.Count}개 (총 {file.Bookmarks.Count}개)");

        // 서버엔 직접 PUT - 큐 없이. 실패하면 SyncedAtUtc가 비어 있어 다음 시작에 다시.
        if (_bookmarkSync != null)
            _ = _bookmarkSync.PushAsync(bookmark.Id);

        return bookmark;
    }

    // 로컬에서 빼고 서버 DELETE. 못 지우면 PendingDeletes에 남아 다음 시작에 다시.
    public bool DeleteBookmark(string id)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        int removed = file.Bookmarks.RemoveAll(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (removed == 0)
            return false;

        if (!file.DeletedIds.Contains(id)) file.DeletedIds.Add(id);

        if (_bookmarkSync != null && !file.PendingDeletes.Contains(id))
            file.PendingDeletes.Add(id);

        _localStore.SaveBookmarks(file);

        if (_bookmarkSync != null)
            _ = _bookmarkSync.DeleteAsync(id);

        return true;
    }

    // 이름이 바뀌면 서버 사본도 바뀌어야 한다 - 같은 id로 다시 PUT(멱등 upsert).
    public bool RenameBookmark(string id, string label)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark bookmark = file.Bookmarks.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (bookmark == null)
            return false;

        bookmark.Label = string.IsNullOrEmpty(label) ? bookmark.Preview : label;
        bookmark.SyncedAtUtc = null;
        _localStore.SaveBookmarks(file);

        if (_bookmarkSync != null)
            _ = _bookmarkSync.PushAsync(id);

        return true;
    }
}