using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationViewUIBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();

    private readonly EpisodePlayState _episodePlayState;
    private readonly VnFeatureController _vnFeatures;
    private readonly VnUxState _uxState;
    private readonly VnRuntimeBridge _vnRuntimeBridge;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;

    private PresentationUIRoot _root;
    private SaveLoadMenuUIPanel _saveLoadPanel;
    private SaveLoadMenuMode _saveLoadMode;

    public PresentationViewUIBindings(
        EpisodePlayState episodePlayState,
        VnFeatureController vnFeatures,
        VnUxState uxState,
        VnRuntimeBridge vnSignalBridge,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher
    )
    {
        _episodePlayState = episodePlayState;
        _vnFeatures = vnFeatures;
        _uxState = uxState;
        _vnRuntimeBridge = vnSignalBridge;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
    }

    public void Bind(PresentationUIRoot root)
    {
        _ctx.Unbind(root);

        _root = root;

        _ctx.Bind(root,
            r => r.OnRollbackOneStepPressed += HandleRollbackPressed,
            r => r.OnRollbackOneStepPressed -= HandleRollbackPressed);

        _ctx.Bind(root,
            r => r.OnSpeedUpHoldStarted += HandleSpeedUpHoldStarted,
            r => r.OnSpeedUpHoldStarted -= HandleSpeedUpHoldStarted);

        _ctx.Bind(root,
            r => r.OnSpeedUpHoldEnded += HandleSpeedUpHoldEnded,
            r => r.OnSpeedUpHoldEnded -= HandleSpeedUpHoldEnded);

        _ctx.Bind(root,
            r => r.OnStepNextPressed += HandleStepNextPressed,
            r => r.OnStepNextPressed -= HandleStepNextPressed);

        _ctx.Bind(root,
            r => r.OnSkipPressed += HandleSkipPressed,
            r => r.OnSkipPressed -= HandleSkipPressed);

        _ctx.Bind(root,
            r => r.OnAutoPressed += HandleAutoPressed,
            r => r.OnAutoPressed -= HandleAutoPressed);

        _ctx.Bind(root,
            r => r.OnQuickMenuPressed += HandleQuickMenuPressed,
            r => r.OnQuickMenuPressed -= HandleQuickMenuPressed);

        _ctx.Bind(root,
            r => r.OnExpandPressed += HandleExpandPressed,
            r => r.OnExpandPressed -= HandleExpandPressed);

        _ctx.Bind(root,
            r => r.OnShowPreviousLogPressed += HandleShowPreviousLogPressed,
            r => r.OnShowPreviousLogPressed -= HandleShowPreviousLogPressed);

        _ctx.Bind(root,
            r => r.OnSetSpeedupPressed += HandleSetSpeedPressed,
            r => r.OnSetSpeedupPressed -= HandleSetSpeedPressed);

        _ctx.Bind(root,
            r => r.OnSaveMenuPressed += HandleSaveMenuPressed,
            r => r.OnSaveMenuPressed -= HandleSaveMenuPressed);

        _ctx.Bind(root,
            r => r.OnLoadMenuPressed += HandleLoadMenuPressed,
            r => r.OnLoadMenuPressed -= HandleLoadMenuPressed);
    }

    private void HandleRollbackPressed()
    {
        _vnFeatures.RequestRollbackOneStep();
    }

    private void HandleSpeedUpHoldStarted()
    {
        _vnFeatures.BeginHoldSpeedUp();
    }

    private void HandleSpeedUpHoldEnded()
    {
        _vnFeatures.EndHoldSpeedUp();
    }

    private void HandleStepNextPressed()
    {
        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();
            _root.SetAutoModeActive(false);
            return;
        }

        _dialogueAdvanceDispatcher.DispatchAdvance();
    }


    private void HandleSaveMenuPressed()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void HandleLoadMenuPressed()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        Debug.Log("1");
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;
        Debug.Log("2");

        VNServiceContainer svc = VNServiceContainer.Instance;

        VNSaveSlotMeta[] metas = svc.SaveRepository.GetAllMetas();

        // var existing = UIManager.Instance.GetUI<SaveLoadMenuUIPanel>();
        // if (existing != null)
        // {
        //     PresentSaveLoadPanel(existing, mode, metas);
        //     return;
        // }

        Debug.Log("3");
        UIManager.Instance.PushPanel<SaveLoadMenuUIPanel>(panel => { PresentSaveLoadPanel(panel, mode, metas); });
    }

    private void PresentSaveLoadPanel(
        SaveLoadMenuUIPanel panel,
        SaveLoadMenuMode mode,
        VNSaveSlotMeta[] metas)
    {
        if (panel == null)
            return;

        BindSaveLoadPanel(panel);

        _saveLoadMode = mode;
        panel.Rebuild(mode, metas);
    }

    private void BindSaveLoadPanel(SaveLoadMenuUIPanel panel)
    {
        if (_saveLoadPanel != null)
            _ctx.Unbind(_saveLoadPanel);

        _saveLoadPanel = panel;

        _ctx.Bind(panel,
            p => p.OnSlotSelected += HandleSaveLoadSlotSelected,
            p => p.OnSlotSelected -= HandleSaveLoadSlotSelected);

        _ctx.Bind(panel,
            p => p.OnCloseRequested += CloseSaveLoadPanel,
            p => p.OnCloseRequested -= CloseSaveLoadPanel);
    }

    private void HandleSaveLoadSlotSelected(int slotIndex)
    {
        VNServiceContainer svc = VNServiceContainer.Instance;

        if (svc == null)
        {
            Debug.LogWarning("[VN] SaveLoad slot selected, but VNServiceContainer is missing.");
            return;
        }

        if (_saveLoadMode == SaveLoadMenuMode.Save)
        {
            SaveSlot(svc, slotIndex);
            Debug.Log("Save");
            return;
        }

        LoadSlot(svc, slotIndex);
    }

    private void SaveSlot(VNServiceContainer svc, int slotIndex)
    {
        if (!svc.IsRuntimeBound || svc.SaveService == null)
        {
            Debug.LogWarning("[VN] Cannot save. Runtime services are not bound.");
            return;
        }

        bool saved = svc.SaveService.SaveManual(slotIndex);

        if (!saved)
            return;

        RefreshSaveLoadPanel();
    }

    private void LoadSlot(VNServiceContainer svc, int slotIndex)
    {
        if (!svc.IsRuntimeBound || svc.LoadService == null)
        {
            Debug.LogWarning("[VN] Cannot load. Runtime services are not bound.");
            return;
        }

        bool loadStarted = svc.LoadService.Load(slotIndex);

        if (!loadStarted)
            return;

        CloseSaveLoadPanel();
    }

    private void RefreshSaveLoadPanel()
    {
        if (_saveLoadPanel == null)
            return;

        VNServiceContainer svc = VNServiceContainer.Instance;

        if (svc == null || !svc.IsPersistentInitialized)
            return;

        VNSaveSlotMeta[] metas = svc.SaveRepository.GetAllMetas();
        _saveLoadPanel.Rebuild(_saveLoadMode, metas);
    }

    private void CloseSaveLoadPanel()
    {
        if (_saveLoadPanel != null)
        {
            _ctx.Unbind(_saveLoadPanel);
            _saveLoadPanel = null;
        }

        UIManager.Instance.PopPanel();
    }


    private void HandleSkipPressed()
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;

        string summary = "현재까지의 스토리(최근):";

        UIManager.Instance.PushPanel<SkipConfirmPanel>(panel =>
        {
            panel.Present(
                title: "에피소드를 스킵할까요?",
                body: summary,
                confirmLabel: "스킵하고 완료",
                cancelLabel: "취소"
            );

            panel.OnConfirmed -= ConfirmSkipEpisode;
            panel.OnCancelled -= CloseSkipConfirm;

            panel.OnConfirmed += ConfirmSkipEpisode;
            panel.OnCancelled += CloseSkipConfirm;
        });
    }

    private void CloseSkipConfirm()
    {
        var panel = UIManager.Instance.GetUI<SkipConfirmPanel>();

        panel.OnConfirmed -= ConfirmSkipEpisode;
        panel.OnCancelled -= CloseSkipConfirm;

        UIManager.Instance.PopPanel();
    }

    private void ConfirmSkipEpisode()
    {
        CloseSkipConfirm();

        string episodeId = _episodePlayState.SelectedEpisodeId;
        if (string.IsNullOrEmpty(episodeId))
        {
            Debug.LogWarning("[VN] Skip confirmed but current episode id is empty.");
        }

        _root.SetSkipModeActive(false);

        _vnRuntimeBridge.ForceCompleteEpisodeNow(episodeId);
        _episodePlayState.ApplyEpisodeState(episodeId);

        UIManager.Instance.PopAllPanels();
        //UIManager.Instance.SwitchRoot<LobbyUIRoot>();
        UIManager.Instance.SwitchRoot<TitleUIRoot>();
    }

    private void HandleAutoPressed()
    {
        _vnFeatures.ToggleAuto();
        _root.SetAutoModeActive(_vnFeatures.IsAuto);
    }

    private void HandleQuickMenuPressed()
    {
    }

    private void HandleExpandPressed()
    {
        if (_uxState.BacklogVisible)
            CloseBacklogPanel();

        if (_uxState.ChoicesVisible)
            CloseChoicePanel();
    }

    private void HandleShowPreviousLogPressed()
    {
        if (_uxState.BacklogVisible)
            return;

        _uxState.SetBacklogVisible(true);

        UIManager.Instance.PushPanel<BacklogPanel>(panel =>
        {
            _ctx.Bind(panel,
                p => p.OnCloseRequested += CloseBacklogPanel,
                p => p.OnCloseRequested -= CloseBacklogPanel);

            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void CloseBacklogPanel()
    {
        _uxState.SetBacklogVisible(false);
        UIManager.Instance.PopPanel();
    }

    private void HandleSetSpeedPressed()
    {
        _vnFeatures.ToggleSetSpeed();
    }

    private void HandleChoicesPresented(IReadOnlyList<string> choices)
    {
        _uxState.SetChoicesVisible(true);

        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();
            _root.SetAutoModeActive(false);
        }

        var existing = UIManager.Instance.GetUI<ChoicePanel>();
        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UIManager.Instance.PushPanel<ChoicePanel>(panel =>
        {
            panel.Present(choices);

            panel.OnChoiceSelected += HandleChoiceSelected;
            panel.OnCloseRequested += CloseChoicePanel;
        });
    }

    private void HandleChoiceSelected(int index)
    {
    }

    private void CloseChoicePanel()
    {
        _uxState.SetChoicesVisible(false);

        var panel = UIManager.Instance.GetUI<ChoicePanel>();

        panel.OnChoiceSelected -= HandleChoiceSelected;
        panel.OnCloseRequested -= CloseChoicePanel;

        UIManager.Instance.PopPanel();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _root = null;
    }
}