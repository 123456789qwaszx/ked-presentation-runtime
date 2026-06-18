using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class VNSaveService
{
    private readonly JsonVNSaveRepository _saveRepo;
    private readonly JsonVNGlobalProgressRepository _globalRepo;
    private readonly VNGlobalProgressData _globalData;
    private readonly IVNRuntimeStateProvider _stateProvider;
    private readonly IVNFlagStore _flagStore;

    private bool UpdateContinueOnManualSave = true;
    private bool UpdateContinueOnAutoSave = true;

    public VNSaveService(
        JsonVNSaveRepository saveRepo,
        JsonVNGlobalProgressRepository globalRepo,
        VNGlobalProgressData globalData,
        IVNRuntimeStateProvider stateProvider,
        IVNFlagStore flagStore)
    {
        _saveRepo = saveRepo;
        _globalRepo = globalRepo;
        _globalData = globalData;
        _stateProvider = stateProvider;
        _flagStore = flagStore;
    }

    public bool SaveManual(int slotIndex) => SaveToSlot(_saveRepo.GetSlotId(slotIndex), isAutoSave: false);
    public bool SaveAuto() => SaveToSlot(_saveRepo.AutoSlot, isAutoSave: true);
    
    private bool SaveToSlot(string slotId, bool isAutoSave)
    {
        VNSaveData data = CaptureSaveData(slotId);

        if (!_saveRepo.Save(data))
            return false;

        UpdateGlobalAfterSave(slotId, isAutoSave);

        return true;
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

            flags = flags,
            choices = _stateProvider.CreateChoiceSnapshot()
        };

        data.Normalize();
        return data;
    }

    private void UpdateGlobalAfterSave(string slotId, bool isAutoSave)
    {
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