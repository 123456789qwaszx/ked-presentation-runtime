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

    SaveSlotIndexFile LoadSaveSlotIndex();
    SaveSlotData LoadSaveSlot(string id);
    void WriteSaveSlot(SaveSlotEntry entry, SaveSlotData data);
    void WriteSaveSlotIndex(SaveSlotIndexFile index);

    int CollectUnusedPlaythroughs();
}
