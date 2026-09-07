using System;
using System.IO;

public sealed class LocalFileSaveStore : ILocalSaveStore
{
    // active.json의 JSON 형태를 정의하기 위한 작은 DTO
    // 실제 세이브 파일이 아니라 포인터.
    // active.json (ActiveId = B)-> playthroughs/B.json
    private sealed class ActiveFile
    {
        public string ActiveId;
    }

    private readonly string _directory;

    public LocalFileSaveStore(string directory)
    {
        _directory = directory;
    }

    public string ActiveId
    {
        get
        {
            string json = AtomicFile.ReadAllTextOrNull(ActivePath);

            return json == null 
                ? null 
                : SaveJson.Deserialize<ActiveFile>(json)?.ActiveId;
        }
    }

    // 회차 저장 + 그 회차를 active로 지정.
    public void SaveAndSetActive(LocalSaveFile save)
    {
        if (string.IsNullOrEmpty(save.PlaythroughId))
            throw new ArgumentException("회차 id가 비어 있다 " +
                                        "LocalSaveFile을 저장하려면 PlaythroughId가 반드시 있어야 함.", nameof(save));

        Directory.CreateDirectory(PlaythroughsDirectory);
        
        AtomicFile.WriteAllText(PlaythroughPathOf(save.PlaythroughId), SaveJson.SerializePretty(save));
        
        SetActive(save.PlaythroughId);
    }

    public LocalSaveFile LoadActive()
    {
        string activeId = ActiveId;

        return LoadPlaythrough(activeId);
    }

    public void ClearActive()
    {
        File.Delete(ActivePath);
    }

    public LocalSaveFile LoadPlaythrough(string playthroughId)
    {
        string json = AtomicFile.ReadAllTextOrNull(PlaythroughPathOf(playthroughId));

        return json == null
            ? null
            : SaveJson.Deserialize<LocalSaveFile>(json);
    }

    public string QueuePathOf(string playthroughId) =>
        Path.Combine(PlaythroughsDirectory, $"{playthroughId}.queue.json");

    public BookmarkFile LoadBookmarks()
    {
        string json = AtomicFile.ReadAllTextOrNull(BookmarksPath);

        return json == null
            ? new BookmarkFile()
            : SaveJson.Deserialize<BookmarkFile>(json) ?? new BookmarkFile();
    }

    public void SaveBookmarks(BookmarkFile bookmarks) =>
        AtomicFile.WriteAllText(BookmarksPath, SaveJson.SerializePretty(bookmarks));

    public System.Collections.Generic.IReadOnlyList<string> ListPlaythroughIds()
    {
        var ids = new System.Collections.Generic.List<string>();

        if (!Directory.Exists(PlaythroughsDirectory))
            return ids;

        foreach (string path in Directory.GetFiles(PlaythroughsDirectory, "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);

            // {id}.queue.json은 큐다.
            if (name.EndsWith(".queue", StringComparison.Ordinal))
                continue;

            ids.Add(name);
        }

        return ids;
    }

    private string BookmarksPath => Path.Combine(_directory, "bookmarks.json");

    private void SetActive(string playthroughId) =>
        AtomicFile.WriteAllText(ActivePath, SaveJson.SerializePretty(new ActiveFile { ActiveId = playthroughId }));

    private string ActivePath => Path.Combine(_directory, "active.json");
    private string PlaythroughsDirectory => Path.Combine(_directory, "playthroughs");
    private string PlaythroughPathOf(string id) => Path.Combine(PlaythroughsDirectory, $"{id}.json");
}
