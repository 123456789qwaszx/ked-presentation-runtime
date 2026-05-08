using System;
using System.Collections.Generic;


public interface IVNSaveRepository
{
    int SlotCount { get; }
    string AutoSlot { get; }

    string GetSlotId(int slotIndex);

    bool TryLoad(string slotId, out VNSaveData data);
    bool Save(VNSaveData data);
    bool Delete(string slotId);
    bool Exists(string slotId);

    VNSaveSlotMeta GetMeta(int slotIndex);
    VNSaveSlotMeta GetMeta(string slotId);
    VNSaveSlotMeta[] GetAllMetas();
}

public interface IVNGlobalProgressRepository
{
    VNGlobalProgressData LoadOrCreate();
    bool Save(VNGlobalProgressData data);
}

public interface IVNRuntimeStateProvider
{
    string CurrentNodeName { get; }
    string CurrentLineId { get; }

    int CurrentVisitedIndex { get; }
    int CurrentLineVisitCountInNode { get; }

    string CurrentChapterLabel { get; }
    string CurrentLinePreview { get; }

    int CurrentPlaytimeSeconds { get; }
}

public interface IVNFlagStore
{
    List<VNFlagEntry> Capture();
    void Restore(List<VNFlagEntry> flags);
}

public interface IVNLoadSeekDriver
{
    void PrepareForLoad();

    void BeginSeek(
        VNSaveData saveData,
        Action onComplete,
        Action onFail);

    void OnLoadComplete(VNSaveData saveData);
}

public interface IVNSaveSafetyPolicy
{
    bool CanManualSaveNow(out string reason);
    bool CanAutoSaveNow(out string reason);
    bool CanLoadNow(out string reason);
}

public interface IVNGameStarter
{
    void StartNewGame(string startNodeName);
}