using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private SaveLoadMenuMode _saveLoadMode;
    private SaveLoadMenuUIPanel _saveLoadPanel;
    private bool _isLoadingSave;

    private void OpenSaveMenu()
    {
        if (!_manualSaveFlow.CanSave)
        {
            Debug.Log("[수동 저장] 현재는 저장할 수 없다.");
            return;
        }

        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void OpenLoadMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

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
            p => p.CloseClicked += CloseSaveLoadMenu,
            p => p.CloseClicked -= CloseSaveLoadMenu);
    }

    #region Handlers

    private async void HandleSlotClicked(VNSaveSlotMeta meta)
    {
        if (meta == null) return;
        
        if (_isLoadingSave)
            return;

        switch (_saveLoadMode)
        {
            case SaveLoadMenuMode.Save:
                SaveToSlot(meta);
                break;

            case SaveLoadMenuMode.Load:
                await LoadFromSlotAsync(meta);
                break;
        }
    }

    private void HandleSaveLoadModeChanged(SaveLoadMenuMode mode)
    {
        if (mode == SaveLoadMenuMode.Save && !_manualSaveFlow.CanSave)
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

    #endregion

    #region Save / Load

    private void SaveToSlot(VNSaveSlotMeta meta)
    {
        bool saved = meta.IsNewSlot
            ? _manualSaveFlow.TryCreate()
            : _manualSaveFlow.TryOverwrite(meta.Id);

        if (saved)
            RefreshSaveLoadMenu();
    }

    private async Task LoadFromSlotAsync(VNSaveSlotMeta meta)
    {
        if (meta.IsNewSlot)
            return;

        _isLoadingSave = true;

        try
        {
            await _manualSaveFlow.TryLoadAsync(
                meta.Id,
                CloseSaveLoadMenu);
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
        if (_saveLoadPanel == null)
            return;

        IReadOnlyList<SaveSlotEntry> slots =
            _saveCoordinator.SaveSlots;

        int count = slots.Count;

        if (_saveLoadMode == SaveLoadMenuMode.Save)
            count++;

        VNSaveSlotMeta[] metas =
            new VNSaveSlotMeta[count];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        if (_saveLoadMode == SaveLoadMenuMode.Save)
            metas[^1] = VNSaveSlotMeta.NewSlot();

        _saveLoadPanel.Rebuild(
            _saveLoadMode,
            metas);
    }

    private void CloseSaveLoadMenu()
    {
        _saveLoadPanel = null;
        ClosePanel();
    }

    #endregion
}