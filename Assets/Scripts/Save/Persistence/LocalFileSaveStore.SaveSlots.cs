using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed partial class LocalFileSaveStore
{
    private string SaveSlotIndexPath => Path.Combine(_directory, "save-slots.json");
    private string SaveSlotDirectory => Path.Combine(_directory, "save-slot-data");
    private string LegacyIndexPath => Path.Combine(_directory, "bookmarks.json");
    private string LegacySlotDirectory => Path.Combine(_directory, "bookmark-snapshots");

    private sealed class LegacySlotIndex
    {
        public List<LegacySlot> Bookmarks = new();
    }

    private sealed class LegacySlot
    {
        public string SnapshotKey { get; set; }
        public List<SceneRecord> Scenes { get; set; } = new();
        public string Id { get; set; }
        public string Label { get; set; }
        public string Preview { get; set; }
        public string CreatedAtUtc { get; set; }
        public string ChapterId { get; set; }
        public SceneCheckpoint Checkpoint { get; set; }
        public SavedLoadPlan Load { get; set; }
        public List<DialogueLogEntry> Backlog { get; set; } = new();
        public int PlaySecondsAtBookmark { get; set; }
    }

    private string SaveSlotPath(string key)
    {
        ValidateId(key);
        return Path.Combine(SaveSlotDirectory, key + ".json");
    }

    public SaveSlotIndexFile LoadSaveSlotIndex()
    {
        SaveSlotIndexFile index = Read<SaveSlotIndexFile>(SaveSlotIndexPath);
        if (index == null)
            index = LoadLegacyIndex();
        SaveDataValidator.ValidateSlotIndex(index);
        return index;
    }

    public SaveSlotData LoadSaveSlot(string id)
    {
        SaveSlotEntry entry = LoadSaveSlotIndex().Slots.Find(slot => slot.Id == id);
        if (entry == null) return null;

        string newPath = SaveSlotPath(entry.DataKey);
        SaveSlotFile file = Read<SaveSlotFile>(newPath);
        bool legacy = file == null;
        if (legacy)
        {
            string oldPath = Path.Combine(LegacySlotDirectory, entry.DataKey + ".json");
            LegacySlot old = Read<LegacySlot>(oldPath);
            file = old == null ? null : ConvertLegacyBody(old);
        }
        SaveDataValidator.ValidateSlot(file, id, allowMissingContentVersion: legacy);
        SaveDataValidator.ValidateSlotLink(entry, file.Data);
        return PlaythroughSession.Copy(file.Data);
    }

    private SaveSlotIndexFile LoadLegacyIndex()
    {
        var index = new SaveSlotIndexFile { FormatVersion = SaveFormat.SaveSlotVersion };
        LegacySlotIndex legacy = Read<LegacySlotIndex>(LegacyIndexPath);
        if (legacy?.Bookmarks == null) return index;

        foreach (LegacySlot old in legacy.Bookmarks)
        {
            // 서버에만 본문이 있던 목록 항목은 로컬에서 복원할 수 없으므로 가져오지 않는다.
            if (old == null || string.IsNullOrEmpty(old.SnapshotKey)) continue;
            index.Slots.Add(new SaveSlotEntry
            {
                Id = old.Id,
                DataKey = old.SnapshotKey,
                Label = old.Label,
                Preview = old.Preview,
                ChapterId = old.ChapterId,
                SavedAtUtc = old.CreatedAtUtc,
                PlaySeconds = old.PlaySecondsAtBookmark,
            });
        }
        return index;
    }

    private static SaveSlotFile ConvertLegacyBody(LegacySlot old) => new()
    {
        FormatVersion = SaveFormat.SaveSlotVersion,
        Data = new SaveSlotData
        {
            Id = old.Id,
            Checkpoint = old.Checkpoint,
            LoadPlan = old.Load,
            Scenes = old.Scenes ?? new List<SceneRecord>(),
            Backlog = old.Backlog ?? new List<DialogueLogEntry>(),
            PlaySeconds = old.PlaySecondsAtBookmark,
        },
    };

    // 본문을 먼저 확보하고 목록을 교체한다. 목록 쓰기가 실패하면 기존 슬롯은 그대로다.
    public void WriteSaveSlot(SaveSlotEntry entry, SaveSlotData data)
    {
        if (entry == null || data == null || entry.Id != data.Id)
            throw new ArgumentException("수동 저장의 목록과 본문 ID가 다르다.");

        SaveSlotEntry nextEntry = PlaythroughSession.Copy(entry);
        SaveSlotData nextData = PlaythroughSession.Copy(data);
        nextEntry.DataKey = Guid.NewGuid().ToString("N");

        var body = new SaveSlotFile
        {
            FormatVersion = SaveFormat.SaveSlotVersion,
            Data = nextData,
        };
        SaveDataValidator.ValidateSlot(body, nextEntry.Id);
        SaveDataValidator.ValidateSlotLink(nextEntry, nextData);

        SaveSlotIndexFile index = LoadSaveSlotIndex();
        index.Slots.RemoveAll(slot => slot.Id == nextEntry.Id);
        index.Slots.Add(nextEntry);
        SaveDataValidator.ValidateSlotIndex(index);

        Write(SaveSlotPath(nextEntry.DataKey), body);
        Write(SaveSlotIndexPath, index);
        entry.DataKey = nextEntry.DataKey;
        CollectSaveSlotFiles();
    }

    public void WriteSaveSlotIndex(SaveSlotIndexFile index)
    {
        SaveSlotIndexFile next = PlaythroughSession.Copy(index);
        SaveDataValidator.ValidateSlotIndex(next);
        Write(SaveSlotIndexPath, next);
        CollectSaveSlotFiles();
    }

    private void CollectSaveSlotFiles()
    {
        if (!Directory.Exists(SaveSlotDirectory)) return;

        var live = LoadSaveSlotIndex().Slots
            .Select(slot => slot.DataKey)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string path in Directory.GetFiles(SaveSlotDirectory, "*.json"))
        {
            if (live.Contains(Path.GetFileNameWithoutExtension(path))) continue;
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
