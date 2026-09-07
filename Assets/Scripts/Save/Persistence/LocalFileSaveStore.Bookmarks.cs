using System;
using System.IO;
using System.Linq;

public sealed partial class LocalFileSaveStore
{
    private string BookmarkDirectory => Path.Combine(_directory, "bookmark-snapshots");
    private string BookmarkPath(string key)
    {
        ValidateId(key);
        return Path.Combine(BookmarkDirectory, key + ".json");
    }

    // 목록은 작은 메타데이터만 반환한다. 재생·전송 시에만 한 snapshot을 연다.
    public Bookmark LoadBookmark(string id)
    {
        Bookmark metadata = LoadBookmarks().Bookmarks.Find(b => b.Id == id);
        if (metadata == null) return null;
        if (metadata.Checkpoint != null) return metadata;
        if (metadata.SnapshotKey == null) return null; // 서버 목록만 복구된 슬롯
        Bookmark bookmark = Read<Bookmark>(BookmarkPath(metadata.SnapshotKey));
        if (bookmark == null || bookmark.Id != id) throw new InvalidDataException("수동 저장 snapshot이 없다: " + id);
        bookmark.Label = metadata.Label;
        bookmark.LocalVersion = metadata.LocalVersion;
        bookmark.SyncedVersion = metadata.SyncedVersion;
        bookmark.SyncedAtUtc = metadata.SyncedAtUtc;
        bookmark.SyncError = metadata.SyncError;
        bookmark.SnapshotKey = metadata.SnapshotKey;
        return bookmark;
    }

    public void SaveBookmarks(BookmarkFile file)
    {
        BookmarkFile next = PlaythroughSession.Copy(file);
        foreach (Bookmark bookmark in next.Bookmarks)
        {
            if (bookmark.Checkpoint != null)
            {
                // 새 내용부터 확보한다. index 교체 실패 시 기존 슬롯은 그대로 남는다.
                string key = Guid.NewGuid().ToString("N");
                bookmark.SnapshotKey = key;
                Write(BookmarkPath(key), bookmark);
            }
            bookmark.Checkpoint = null;
            bookmark.Load = null;
            bookmark.Backlog = new();
            bookmark.Scenes = new();
        }
        Write(Path.Combine(_directory, "bookmarks.json"), next);
        // 실패한 쓰기의 orphan도 다음 정상 저장/maintenance 때 정리된다.
        CollectBookmarkSnapshots();
    }

    private void CollectBookmarkSnapshots()
    {
        if (!Directory.Exists(BookmarkDirectory)) return;
        var live = LoadBookmarks().Bookmarks.Select(b => b.SnapshotKey).Where(k => k != null).ToHashSet(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(BookmarkDirectory, "*.json"))
        {
            if (live.Contains(Path.GetFileNameWithoutExtension(path))) continue;
            try { File.Delete(path); }
            catch (IOException) { /* index는 이미 확정됐다. 다음 maintenance에서 재시도. */ }
            catch (UnauthorizedAccessException) { }
        }
    }
}
