using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    // 북마크:
    // - 단순 즐겨찾기가 아닌,
    // - 재생 가능한 save fragment.(복원 시 필요한 재생 상태를 묶어둠)
    public IReadOnlyList<Bookmark> Bookmarks => _localStore.LoadBookmarks().Bookmarks;

    public Task<Bookmark> GetBookmarkAsync(string id) =>
        Task.FromResult(_localStore.LoadBookmark(id));

    public async Task<bool> RenameBookmarkAsync(string id, string label)
    {
        if (await GetBookmarkAsync(id) == null) return false;
        return RenameBookmark(id, label);
    }

    // 현재 슬롯을 새 ID로 복제한다. 원본은 유지한다.
    public async Task<Bookmark> DuplicateBookmarkAsync(string id, string label = null)
    {
        Bookmark bookmark = await GetBookmarkAsync(id);
        if (bookmark == null) return null;
        bookmark.Id = NewPlaythroughId();
        bookmark.LocalVersion = 1;
        bookmark.SnapshotKey = null;
        bookmark.CreatedAtUtc = NowUtc();
        if (label != null) bookmark.Label = label;
        BookmarkFile file = _localStore.LoadBookmarks();
        file.Bookmarks.Add(bookmark);
        _localStore.SaveBookmarks(file);
        return bookmark;
    }

    public Bookmark CreateBookmark(
        IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label = null) =>
        WriteBookmark(null, path, yarnChoices, target, preview, label);

    // 같은 수동 슬롯 ID에 현재 도달한 지점을 저장한다. 실패하면 기존 슬롯은 유지된다.
    public Bookmark OverwriteBookmark(
        string id, IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label = null) =>
        WriteBookmark(id ?? throw new ArgumentNullException(nameof(id)), path, yarnChoices, target, preview, label);

    private Bookmark WriteBookmark(
        string id, IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label)
    {
        if (_currentEntry == null || target == null)
            return null;

        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark previous = id == null ? null : file.Bookmarks.Find(b => b.Id == id);
        if (id != null && previous == null) throw new InvalidOperationException("덮어쓸 수동 저장이 없다.");
        LocalSaveFile current = _localStore.LoadActive();

        var bookmark = new Bookmark
        {
            Id = id ?? NewPlaythroughId(),
            LocalVersion = (previous?.LocalVersion ?? 0) + 1,
            Scenes = PlaythroughSession.Copy(_scenes),
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

        if (previous != null) file.Bookmarks.Remove(previous);
        file.Bookmarks.Add(bookmark);
        _localStore.SaveBookmarks(file);

        Debug.Log(
            $"[저장] 즐겨찾기 — \"{bookmark.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {bookmark.Load.Path.Count}개, Yarn 선택 {bookmark.Load.YarnChoices.Count}개 (총 {file.Bookmarks.Count}개)");

        return bookmark;
    }

    // 슬롯 index를 확정한 뒤 미참조 본문을 정리한다.
    public bool DeleteBookmark(string id)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        int removed = file.Bookmarks.RemoveAll(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (removed == 0)
            return false;

        _localStore.SaveBookmarks(file);

        return true;
    }

    // 슬롯의 이름만 바꾼다.
    public bool RenameBookmark(string id, string label)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark bookmark = file.Bookmarks.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));

        if (bookmark == null || _localStore.LoadBookmark(id) == null)
            return false;

        bookmark.Label = string.IsNullOrEmpty(label) ? bookmark.Preview : label;
        bookmark.LocalVersion++;
        _localStore.SaveBookmarks(file);

        return true;
    }
}
