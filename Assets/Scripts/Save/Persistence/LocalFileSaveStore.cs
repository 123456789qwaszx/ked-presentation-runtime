using System;
using System.IO;
using UnityEngine;

// {directory}/playthroughs/{id}.json — 회차 파일. {directory}/active.json — 활성 회차 포인터.
// 큐는 {directory}/playthroughs/{id}.queue.json.
//
// 옛 형식({directory}/slot1.json + sync_queue.json)은 처음 읽을 때 한 번 옮긴다.
// 회차 파일은 지우지 않는다 — 갈라진 옛 회차도 데이터는 남긴다(save-plan.md §3.3).
public sealed class LocalFileSaveStore : ISaveStore
{
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

            return json == null ? null : SaveJson.Deserialize<ActiveFile>(json)?.ActiveId;
        }
    }

    public void Save(LocalSaveFile save)
    {
        if (string.IsNullOrEmpty(save.PlaythroughId))
            throw new ArgumentException("회차 id가 비어 있다 — 회차 파일은 id로 산다.", nameof(save));

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
