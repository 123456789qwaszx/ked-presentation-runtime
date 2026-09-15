using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private void OpenSaveMenu()
    {
        if (!_manualSaveFlow.CanSave)
        {
            Debug.Log("[수동 저장] 현재는 저장할 수 없다.");
            return;
        }

        UI.PushPanel<SystemMenuPanel>(owner =>
        {
            UI.SwitchPage<SavePage>(
                owner,
                afterPatched: page =>
                {
                    BindView(page, ApplyBindings);
                    RefreshSavePage(page);
                    page.ResetPage();
                },
                afterClosed: page => Unbind(page));
        });
    }

    private void OpenLoadMenu()
    {
        UI.PushPanel<SystemMenuPanel>(owner =>
        {
            UI.SwitchPage<LoadPage>(
                owner,
                afterPatched: page =>
                {
                    BindView(page, ApplyBindings);
                    RefreshLoadPage(page);
                    page.ResetPage();
                },
                afterClosed: page => Unbind(page));
        });
    }

    private void ApplyBindings(SavePage page)
    {
        Action<VNSaveSlotMeta> handleSlotClicked =
            meta => HandleSaveSlotClicked(page, meta);

        AddBinding(
            page,
            p => p.SlotClicked += handleSlotClicked,
            p => p.SlotClicked -= handleSlotClicked);

        AddBinding(
            page,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);
    }

    private void ApplyBindings(LoadPage page)
    {
        AddBinding(
            page,
            p => p.SlotClicked += HandleLoadSlotClicked,
            p => p.SlotClicked -= HandleLoadSlotClicked);

        AddBinding(
            page,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);
    }

    #region Handlers

    private void HandleSaveSlotClicked(SavePage page, VNSaveSlotMeta meta)
    {
        if (meta == null)
            return;

        bool saved = meta.IsNewSlot
            ? _manualSaveFlow.TryCreate()
            : _manualSaveFlow.TryOverwrite(meta.Id);

        if (saved)
            RefreshSavePage(page);
    }

    private void HandleLoadSlotClicked(VNSaveSlotMeta meta)
    {
        if (meta == null || meta.IsNewSlot)
            return;
        
        _ = _manualSaveFlow.LoadAsync(meta.Id, ClosePanel);
    }

    #endregion

    #region Refresh

    private void RefreshSavePage(SavePage page)
    {
        IReadOnlyList<SaveSlotEntry> slots = _saveCoordinator.SaveSlots;
        VNSaveSlotMeta[] metas = new VNSaveSlotMeta[slots.Count + 1];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        metas[^1] = VNSaveSlotMeta.NewSlot();

        page.Rebuild(metas);
    }

    private void RefreshLoadPage(LoadPage page)
    {
        IReadOnlyList<SaveSlotEntry> slots = _saveCoordinator.SaveSlots;
        VNSaveSlotMeta[] metas = new VNSaveSlotMeta[slots.Count];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        page.Rebuild(metas);
    }

    #endregion
}
