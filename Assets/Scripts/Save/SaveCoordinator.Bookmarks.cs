using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // ── 즐겨찾기 ─────────────────────────────────────────────────────────────

    public IReadOnlyList<Bookmark> Bookmarks => _localStore.LoadBookmarks().Items;

    // 지금 라인을 즐겨찾기로. 스스로 완결된 사본 — 진입 스냅샷·찍은 순간까지의 경로·Yarn 선택·표적·이전 백로그.
    // 장면 밖(진입 보고 전)이면 null.
    public Bookmark CreateBookmark(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview,
        string label = null)
    {
        if (_currentEntry == null || target == null)
            return null;

        LocalSaveFile current = _localStore.Load(_slotNo);

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
        file.Items.Add(bookmark);
        _localStore.SaveBookmarks(file);

        Debug.Log(
            $"[저장] 즐겨찾기 — \"{bookmark.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {bookmark.Load.Path.Count}개, Yarn 선택 {bookmark.Load.YarnChoices.Count}개 (총 {file.Items.Count}개)");

        // 서버엔 직접 PUT — 큐 없이. 실패하면 SyncedAtUtc가 비어 있어 다음 시작에 다시.
        if (_bookmarkSync != null)
            _ = _bookmarkSync.PushAsync(bookmark.Id);

        return bookmark;
    }

    // 로컬에서 빼고 서버 DELETE. 못 지우면 PendingDeletes에 남아 다음 시작에 다시.
    public bool DeleteBookmark(string id)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        int removed = file.Items.RemoveAll(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (removed == 0)
            return false;

        if (_bookmarkSync != null && !file.PendingDeletes.Contains(id))
            file.PendingDeletes.Add(id);

        _localStore.SaveBookmarks(file);

        if (_bookmarkSync != null)
            _ = _bookmarkSync.DeleteAsync(id);

        return true;
    }

    // 이름이 바뀌면 서버 사본도 바뀌어야 한다 — 같은 id로 다시 PUT(멱등 upsert).
    public bool RenameBookmark(string id, string label)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark bookmark = file.Items.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));

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
