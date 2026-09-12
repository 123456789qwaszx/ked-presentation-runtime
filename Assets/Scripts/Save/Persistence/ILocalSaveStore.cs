using System.Collections.Generic;

public interface ILocalSaveStore
{
    void Initialize();
    PlaythroughSession Open(string id);
    PlaythroughSession Create(LocalSaveFile save);
    void SetActive(string id);
    LocalSaveFile LoadActive();
    string ActiveId { get; }
    LocalSaveFile LoadPlaythrough(string id);
    IReadOnlyList<string> ListPlaythroughIds();
    BookmarkFile LoadBookmarks();
    void SaveBookmarks(BookmarkFile file);
    Bookmark LoadBookmark(string id);
    IReadOnlyList<PlaythroughSummary> ListPlaythroughSummaries();
    int CollectUnusedPlaythroughs();
}
