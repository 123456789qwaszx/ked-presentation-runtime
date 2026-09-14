using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private SavePage _savePage;
    private LoadPage _loadPage;
    private bool _isLoadingSave;

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
                    _savePage = page;

                    BindView(page, ApplyBindings);

                    RefreshSavePage();
                    page.ResetPage();
                },
                afterClosed: HandleSaveLoadPageClosed);
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
                    _loadPage = page;

                    BindView(page, ApplyBindings);

                    RefreshLoadPage();
                    page.ResetPage();
                },
                afterClosed: HandleSaveLoadPageClosed);
        });
    }

    private void ApplyBindings(SavePage page)
    {
        AddBinding(
            page,
            p => p.SlotClicked += HandleSaveSlotClicked,
            p => p.SlotClicked -= HandleSaveSlotClicked);

        AddBinding(
            page,
            p => p.CloseClicked += CloseSaveLoadMenu,
            p => p.CloseClicked -= CloseSaveLoadMenu);
    }

    private void ApplyBindings(LoadPage page)
    {
        AddBinding(
            page,
            p => p.SlotClicked += HandleLoadSlotClicked,
            p => p.SlotClicked -= HandleLoadSlotClicked);

        AddBinding(
            page,
            p => p.CloseClicked += CloseSaveLoadMenu,
            p => p.CloseClicked -= CloseSaveLoadMenu);
    }

    #region Handlers

    private void HandleSaveSlotClicked(VNSaveSlotMeta meta)
    {
        if (meta == null)
            return;

        SaveToSlot(meta);
    }

    private async void HandleLoadSlotClicked(VNSaveSlotMeta meta)
    {
        if (meta == null || _isLoadingSave)
            return;

        await LoadFromSlotAsync(meta);
    }

    private void HandleSaveLoadPageClosed(UIBase page)
    {
        Unbind(page);

        if (_savePage == page)
            _savePage = null;

        if (_loadPage == page)
            _loadPage = null;
    }

    #endregion

    #region Save / Load

    private void SaveToSlot(VNSaveSlotMeta meta)
    {
        bool saved = meta.IsNewSlot
            ? _manualSaveFlow.TryCreate()
            : _manualSaveFlow.TryOverwrite(meta.Id);

        if (saved)
            RefreshSavePage();
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

    private void RefreshSavePage()
    {
        if (_savePage == null)
            return;

        IReadOnlyList<SaveSlotEntry> slots =
            _saveCoordinator.SaveSlots;

        VNSaveSlotMeta[] metas =
            new VNSaveSlotMeta[slots.Count + 1];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        metas[^1] = VNSaveSlotMeta.NewSlot();

        _savePage.Rebuild(metas);
    }

    private void RefreshLoadPage()
    {
        if (_loadPage == null)
            return;

        IReadOnlyList<SaveSlotEntry> slots =
            _saveCoordinator.SaveSlots;

        VNSaveSlotMeta[] metas =
            new VNSaveSlotMeta[slots.Count];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        _loadPage.Rebuild(metas);
    }

    private void CloseSaveLoadMenu()
    {
        _savePage = null;
        _loadPage = null;
        ClosePanel();
    }

    #endregion
}
