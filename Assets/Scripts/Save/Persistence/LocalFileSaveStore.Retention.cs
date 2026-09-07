using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed partial class LocalFileSaveStore
{
    private sealed class CatalogEntry
    {
        public long WriteTicks;
        public long Length;
        public PlaythroughSummary Summary;
    }
    private Dictionary<string, CatalogEntry> _catalog;
    private bool _catalogDirty;
    private string CatalogPath => Path.Combine(_directory, "playthrough-catalog.json");

    // catalog는 파생 캐시다. 원본 파일의 크기·수정 시각이 달라지면 해당 항목만 재구성한다.
    // Query는 디스크에 쓰지 않고, maintenance에서 캐시를 보존한다.
    public IReadOnlyList<PlaythroughSummary> ListPlaythroughSummaries()
    {
        if (_catalog == null)
        {
            try { _catalog = Read<Dictionary<string, CatalogEntry>>(CatalogPath); }
            catch (Newtonsoft.Json.JsonException) { /* 파생 캐시는 원본으로 재구성한다. */ }
            _catalog ??= new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        }
        string active = ActiveId;
        var bookmarks = LoadBookmarks().Bookmarks.GroupBy(b => b.PlaythroughId ?? "").ToDictionary(g => g.Key, g => g.Count());
        var summaries = new List<PlaythroughSummary>();
        foreach (string id in ListPlaythroughIds())
        {
            var info = new FileInfo(PathOf(id));
            if (!_catalog.TryGetValue(id, out CatalogEntry entry) || entry?.Summary == null || entry.WriteTicks != info.LastWriteTimeUtc.Ticks || entry.Length != info.Length)
            {
                entry = new CatalogEntry { WriteTicks = info.LastWriteTimeUtc.Ticks, Length = info.Length,
                    Summary = Open(id).GetSummary() };
                _catalog[id] = entry;
                _catalogDirty = true;
            }
            PlaythroughSummary summary = PlaythroughSession.Copy(entry.Summary);
            summary.IsActive = id == active;
            summary.BookmarkCount = bookmarks.TryGetValue(id, out int count) ? count : 0;
            summaries.Add(summary);
        }
        return summaries.OrderByDescending(s => s.SavedAtUtc, StringComparer.Ordinal).ToArray();
    }

    // 수동 슬롯 출처·오토세이브·미완료 작업을 제외한 내부 회차만 정리한다.
    // 날짜나 개수로 수동 저장을 만료시키지 않는다.
    public int CollectUnusedPlaythroughs(bool requireSynced)
    {
        if (File.Exists(TransferPath)) return 0;
        var roots = LoadBookmarks().Bookmarks.Select(b => b.PlaythroughId).Where(id => id != null).ToHashSet(StringComparer.Ordinal);
        if (ActiveId != null) roots.Add(ActiveId);
        int removed = 0;
        foreach (string id in ListPlaythroughIds())
        {
            if (roots.Contains(id)) continue;
            PlaythroughSession session = Open(id);
            if (!session.CanCollect(requireSynced)) continue;
            File.Delete(PathOf(id));
            session.Close();
            _sessions.Remove(id);
            _catalog?.Remove(id);
            _catalogDirty = true;
            removed++;
        }
        CollectBookmarkSnapshots();
        ListPlaythroughSummaries();
        var existing = ListPlaythroughIds().ToHashSet(StringComparer.Ordinal);
        foreach (string id in _catalog.Keys.ToArray())
            if (!existing.Contains(id)) { _catalog.Remove(id); _catalogDirty = true; }
        if (_catalogDirty)
        {
            Write(CatalogPath, _catalog);
            _catalogDirty = false;
        }
        return removed;
    }
}
