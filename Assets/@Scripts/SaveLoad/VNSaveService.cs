using System;
using System.Collections.Generic;
using UnityEngine;

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
        if (_stateProvider == null)
        {
            Debug.LogError("[VNSaveService] StateProvider is null.");
            return false;
        }

        if (_flagStore == null)
        {
            Debug.LogError("[VNSaveService] FlagStore is null.");
            return false;
        }

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

        Debug.Log($"[VNSaveService] Saved. slot='{slotId}', node='{data.nodeName}', line='{data.lineId}', visitedIndex={data.visitedIndex}");
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

            nodeName = SafeString(_stateProvider.CurrentNodeName),
            lineId = SafeString(_stateProvider.CurrentLineId),

            visitedIndex = _stateProvider.CurrentVisitedIndex,
            lineVisitCountInNode = _stateProvider.CurrentLineVisitCountInNode,

            chapterLabel = SafeString(_stateProvider.CurrentChapterLabel),
            linePreview = TrimPreview(SafeString(_stateProvider.CurrentLinePreview), 80),

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

    private string SafeString(string value)
    {
        return value ?? "";
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