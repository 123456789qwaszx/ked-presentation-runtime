using System.Collections.Generic;

public interface ILocalSaveStore
{
    void Initialize();
    PlaythroughSession Open(string id);
    PlaythroughSession Create(LocalSaveFile save);
    void ImportRestored(LocalSaveFile save, long serverId, long revision, int nextSeq);
    void SetActive(string id);
    void SelectLocalPlaythrough();
    bool TryActivateRestored(string id);
    LocalSaveFile LoadActive();
    string ActiveId { get; }
    long SelectionVersion { get; }
    string SelectionScopeId { get; }
    LocalSaveFile LoadPlaythrough(string id);
    IReadOnlyList<string> ListPlaythroughIds();
    BookmarkFile LoadBookmarks();
    void SaveBookmarks(BookmarkFile file);
    Bookmark LoadBookmark(string id);
    IReadOnlyList<PlaythroughSummary> ListPlaythroughSummaries();
    int CollectUnusedPlaythroughs(bool requireSynced);
    RestoreProgress LoadRestoreProgress();
    void SaveRestoreProgress(RestoreProgress progress);
    PlaythroughSession ForkConflict(string sourceId);
}
