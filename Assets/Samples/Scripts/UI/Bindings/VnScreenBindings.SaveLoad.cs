using Ked.Progression.Save;
using System;
using System.Collections.Generic;
using System.IO;
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
        // 목록을 못 읽으면 새 칸도 두지 않는다 — 새로 쓸 때도 같은 목록을 먼저 읽다가 실패한다.
        bool readable = TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);
        VNSaveSlotMeta[] metas = new VNSaveSlotMeta[slots.Count + (readable ? 1 : 0)];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        if (readable)
            metas[^1] = VNSaveSlotMeta.NewSlot();

        page.Rebuild(metas);
    }

    private void RefreshLoadPage(LoadPage page)
    {
        TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots);
        VNSaveSlotMeta[] metas = new VNSaveSlotMeta[slots.Count];

        for (int i = 0; i < slots.Count; i++)
            metas[i] = VNSaveSlotMeta.ExistingSlot(slots[i]);

        page.Rebuild(metas);
    }

    // 진행 저장은 읽을 수 없는 목록(옛 형식·손상)을 예외로 알린다. 화면은 빈 목록으로 열고 이유를 남긴다.
    // 옛 형식은 저장 분할(회차 4·슬롯 2) 전의 로컬 저장이다 — 이전하지 않는다.
    private bool TryReadSlots(out IReadOnlyList<SaveSlotEntry> slots)
    {
        try
        {
            slots = _saveSlots.Slots;
            return true;
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning(
                $"[수동 저장] 저장 목록을 읽지 못해 빈 목록으로 연다 — {error.Message} " +
                "옛 형식이면 저장 폴더의 save-slots.json과 save-slot-data/를 치우면 된다.");

            slots = Array.Empty<SaveSlotEntry>();
            return false;
        }
    }

    #endregion
}
