using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private SaveLoadMenuMode _saveLoadMode;

    // UI의 slotIndex를 실제 SaveSlotEntry에 연결하는 화면용 목록.
    private readonly List<SaveSlotEntry> _saveSlots = new();

    private SaveLoadMenuUIPanel _saveLoadPanel;
    private ManualSaveFlow _manualSaveFlow;

    private bool _isLoadingSave;

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        _saveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(panel =>
        {
            _saveLoadPanel = panel;

            BindPanel(panel, ApplyBindings);

            RefreshSaveLoadMenu();
            panel.ResetPage();
        });
    }

    private void ApplyBindings(SaveLoadMenuUIPanel panel)
    {
        AddBinding(
            panel,
            p => p.SlotClicked += HandleSlotClicked,
            p => p.SlotClicked -= HandleSlotClicked);

        AddBinding(
            panel,
            p => p.ModeChanged += HandleSaveLoadModeChanged,
            p => p.ModeChanged -= HandleSaveLoadModeChanged);

        AddBinding(
            panel,
            p => p.CloseClicked += HandleSaveLoadCloseClicked,
            p => p.CloseClicked -= HandleSaveLoadCloseClicked);
    }

    #region Handlers

    private async void HandleSlotClicked(int slotIndex)
    {
        if (_isLoadingSave)
            return;

        int index = slotIndex - 1;

        if (index < 0)
            return;

        switch (_saveLoadMode)
        {
            case SaveLoadMenuMode.Save:
                SaveToSlot(index);
                break;

            case SaveLoadMenuMode.Load:
                await LoadFromSlotAsync(index);
                break;
        }
    }

    private void HandleSaveLoadModeChanged(SaveLoadMenuMode mode)
    {
        if (mode == SaveLoadMenuMode.Save &&
            !_manualSaveFlow.CanSave)
        {
            Debug.Log(
                "[수동 저장] 진행 중이 아니므로 저장 모드로 전환할 수 없다.");

            RefreshSaveLoadMenu();
            return;
        }

        _saveLoadMode = mode;

        RefreshSaveLoadMenu();
        _saveLoadPanel?.ResetPage();
    }

    private void HandleSaveLoadCloseClicked()
    {
        CloseSaveLoadMenu();
    }

    #endregion

    #region Save / Load

    private void SaveToSlot(int index)
    {
        bool saved;

        if (index < _saveSlots.Count)
        {
            saved = _manualSaveFlow.TryOverwrite(
                _saveSlots[index]);
        }
        else if (index == _saveSlots.Count)
        {
            saved = _manualSaveFlow.TryCreate();
        }
        else
        {
            return;
        }

        if (saved)
            RefreshSaveLoadMenu();
    }

    private async System.Threading.Tasks.Task LoadFromSlotAsync(int index)
    {
        if (index < 0 || index >= _saveSlots.Count)
            return;

        SaveSlotEntry slot = _saveSlots[index];

        _isLoadingSave = true;

        try
        {
            await _manualSaveFlow.LoadAsync(
                slot,
                CloseSaveLoadMenu);
        }
        catch (Exception error)
        {
            Debug.LogError(
                $"[수동 저장] 불러오기 실패\n{error}");
        }
        finally
        {
            _isLoadingSave = false;
        }
    }

    #endregion

    #region Refresh

    private void RefreshSaveLoadMenu()
    {
        // CollectSaveSlots
        _saveSlots.Clear();

        IReadOnlyList<SaveSlotEntry> slots =
            _saveCoordinator.SaveSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            SaveSlotEntry slot = slots[i];

            if (slot != null)
                _saveSlots.Add(slot);
        }
        
        // BuildSaveSlotMetas
        int count = _saveSlots.Count;

        if (_saveLoadMode == SaveLoadMenuMode.Save)
            count++;

        var metas = new VNSaveSlotMeta[count];

        for (int i = 0; i < _saveSlots.Count; i++)
            metas[i] = VNSaveSlotMeta.From(_saveSlots[i]);

        if (_saveLoadMode == SaveLoadMenuMode.Save)
            metas[^1] = VNSaveSlotMeta.Empty();

        _saveLoadPanel.Rebuild(_saveLoadMode, metas);
    }

    private void CloseSaveLoadMenu()
    {
        _saveLoadPanel = null;
        ClosePanel();
    }

    #endregion
}