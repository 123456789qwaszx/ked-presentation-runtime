using System;
using System.Collections.Generic;
using UnityEngine;

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
    string CurrentCharacterKey { get; }

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


public interface IVNSaveSafetyPolicy
{
    bool CanManualSaveNow(out string reason);
    bool CanAutoSaveNow(out string reason);
    bool CanLoadNow(out string reason);
}


public sealed class VNSaveService
{
    private readonly IVNSaveRepository _saveRepo;
    private readonly IVNGlobalProgressRepository _globalRepo;
    private readonly VNGlobalProgressData _globalData;
    private readonly IVNRuntimeStateProvider _stateProvider;
    private readonly IVNFlagStore _flagStore;
    private readonly IVNSaveSafetyPolicy _safetyPolicy;

    public bool UpdateContinueOnManualSave = true;
    public bool UpdateContinueOnAutoSave = true;

    public VNSaveService(
        IVNSaveRepository saveRepo,
        IVNGlobalProgressRepository globalRepo,
        VNGlobalProgressData globalData,
        IVNRuntimeStateProvider stateProvider,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy)
    {
        _saveRepo = saveRepo;
        _globalRepo = globalRepo;
        _globalData = globalData;
        _stateProvider = stateProvider;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
    }

    public bool SaveManual(int slotIndex)
    {
        string slotId = _saveRepo.GetSlotId(slotIndex);
        return SaveToSlot(slotId, isAutoSave: false);
    }

    public bool SaveAuto()
    {
        return SaveToSlot(_saveRepo.AutoSlot, isAutoSave: true);
    }

    public bool SaveToSlot(string slotId, bool isAutoSave)
    {
        if (!CanSaveNow(isAutoSave, out string reason))
        {
            Debug.LogWarning($"[VNSaveService] Save blocked. slot='{slotId}', reason='{reason}'");
            return false;
        }

        VNSaveData data = CaptureSaveData(slotId);

        if (!data.HasValidTarget())
        {
            Debug.LogWarning($"[VNSaveService] Save aborted. Invalid target. node='{data.nodeName}', line='{data.lineId}'");
            return false;
        }

        if (!_saveRepo.Save(data))
            return false;

        UpdateGlobalAfterSave(slotId, isAutoSave);

        return true;
    }

    public bool CanSaveNow(bool isAutoSave, out string reason)
    {
        if (_safetyPolicy == null)
        {
            reason = "";
            return true;
        }

        return isAutoSave
            ? _safetyPolicy.CanAutoSaveNow(out reason)
            : _safetyPolicy.CanManualSaveNow(out reason);
    }

    private VNSaveData CaptureSaveData(string slotId)
    {
        DateTime now = DateTime.Now;

        List<VNFlagEntry> flags = _flagStore.Capture();

        var data = new VNSaveData
        {
            slotId = slotId,

            nodeName = _stateProvider.CurrentNodeName,
            lineId = _stateProvider.CurrentLineId,

            visitedIndex = _stateProvider.CurrentVisitedIndex,
            lineVisitCountInNode = _stateProvider.CurrentLineVisitCountInNode,

            chapterLabel = _stateProvider.CurrentChapterLabel,
            linePreview = TrimPreview(_stateProvider.CurrentLinePreview, 80),

            savedAt = now.ToString("yyyy-MM-dd HH:mm"),
            savedAtTicks = now.Ticks,

            playtimeSeconds = Mathf.Max(0, _stateProvider.CurrentPlaytimeSeconds),

            flags = flags ?? new List<VNFlagEntry>()
        };

        data.Normalize();
        return data;
    }

    private void UpdateGlobalAfterSave(string slotId, bool isAutoSave)
    {
        _globalData.Normalize();

        if (isAutoSave)
        {
            _globalData.latestAutoSlotId = slotId;

            if (UpdateContinueOnAutoSave)
                _globalData.continueSlotId = slotId;
        }
        else
        {
            _globalData.latestManualSlotId = slotId;

            if (UpdateContinueOnManualSave)
                _globalData.continueSlotId = slotId;
        }

        _globalRepo.Save(_globalData);
    }

    private string TrimPreview(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Replace("\r", " ").Replace("\n", " ").Trim();

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "…";
    }
}