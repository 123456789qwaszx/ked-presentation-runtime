using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationViewUIBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();
    private static UIManager UI => UIManager.Instance;

    private readonly EpisodePlayState _episodePlayState;
    private readonly VnFeatureController _vnFeatures;
    private readonly VnUxState _uxState;
    private readonly VnRuntimeBridge _vnRuntimeBridge;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;

    private PresentationUIRoot _root;

    private VNServiceContainer _vnServiceContainer;

    private SaveLoadMenuMode _currentSaveLoadMode;
    private SaveLoadMenuUIPanel _saveLoadRoot;

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

    public void AttachVNServiceContainer(VNServiceContainer serviceContainer)
    {
        _vnServiceContainer = serviceContainer;
    }

    public void Bind(PresentationUIRoot root)
    {
        if (root == null)
            return;

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

    #region Presentation Root Events

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

    private void HandleSkipPressed()
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;

        string summary = "현재까지의 스토리(최근):";

        UI.PushPanel<SkipConfirmPanel>(panel =>
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

        UI.PushPanel<BacklogPanel>(panel =>
        {
            _ctx.Bind(panel,
                p => p.OnCloseRequested += CloseBacklogPanel,
                p => p.OnCloseRequested -= CloseBacklogPanel);

            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void HandleSetSpeedPressed()
    {
        _vnFeatures.ToggleSetSpeed();
    }

    #endregion

    #region SaveLoad

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;

        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();
            _root.SetAutoModeActive(false);
        }

        _root.SetExpanded(false);
        _root.SetQuickMenuOpen(false);

        _currentSaveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(root =>
        {
            BindSaveLoadRoot(root);
        });
    }

    private void BindSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        if (saveLoadRoot == null)
            return;

        if (_saveLoadRoot != null && _saveLoadRoot != saveLoadRoot)
            _ctx.Unbind(_saveLoadRoot);

        _ctx.Unbind(saveLoadRoot);

        _saveLoadRoot = saveLoadRoot;

        _ctx.Bind(
            saveLoadRoot,
            r => r.OnSlotSelected += OnSaveLoadSlotSelected,
            r => r.OnSlotSelected -= OnSaveLoadSlotSelected);

        _ctx.Bind(
            saveLoadRoot,
            r => r.OnCloseRequested += OnSaveLoadCloseRequested,
            r => r.OnCloseRequested -= OnSaveLoadCloseRequested);

        RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void RefreshSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        VNSaveSlotMeta[] metas = _vnServiceContainer.GetAllSaveSlotMetas();
        saveLoadRoot.Rebuild(_currentSaveLoadMode, metas);
    }

    private void OnSaveLoadSlotSelected(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            HandleSaveSlotSelected(slotIndex);
            return;
        }

        HandleLoadSlotSelected(slotIndex);
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!_vnServiceContainer.SaveService.SaveManual(slotIndex))

        RefreshSaveLoadRoot(_saveLoadRoot);
    }

    private void HandleLoadSlotSelected(int slotIndex)
    {
        if (!_vnServiceContainer.IsInitialized || _vnServiceContainer.LoadService == null)
        {
            Debug.LogWarning("[PresentationViewUIBindings] Runtime is not bound. Cannot load.");
            return;
        }

        if (!_vnServiceContainer.LoadService.Load(slotIndex))
        {
            Debug.LogWarning($"[PresentationViewUIBindings] Load failed. slotIndex={slotIndex}");
            return;
        }

        CloseSaveLoadRoot();
    }

    private void OnSaveLoadCloseRequested()
    {
        CloseSaveLoadRoot();
    }

    private void CloseSaveLoadRoot()
    {
        if (_saveLoadRoot != null)
        {
            _ctx.Unbind(_saveLoadRoot);
            _saveLoadRoot = null;
        }

        UI.PopPanel();
    }

    #endregion

    #region Skip

    private void CloseSkipConfirm()
    {
        var panel = UI.GetUI<SkipConfirmPanel>();

        if (panel != null)
        {
            panel.OnConfirmed -= ConfirmSkipEpisode;
            panel.OnCancelled -= CloseSkipConfirm;
        }

        UI.PopPanel();
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

        UI.PopAllPanels();
        UI.SwitchRoot<TitleUIRoot>();
    }

    #endregion

    #region Backlog

    private void CloseBacklogPanel()
    {
        _uxState.SetBacklogVisible(false);
        UI.PopPanel();
    }

    #endregion

    #region Choices

    private void HandleChoicesPresented(IReadOnlyList<string> choices)
    {
        _uxState.SetChoicesVisible(true);

        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();
            _root.SetAutoModeActive(false);
        }

        var existing = UI.GetUI<ChoicePanel>();
        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UI.PushPanel<ChoicePanel>(panel =>
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

        var panel = UI.GetUI<ChoicePanel>();

        if (panel != null)
        {
            panel.OnChoiceSelected -= HandleChoiceSelected;
            panel.OnCloseRequested -= CloseChoicePanel;
        }

        UI.PopPanel();
    }

    #endregion

    public void Dispose()
    {
        if (_saveLoadRoot != null)
        {
            _ctx.Unbind(_saveLoadRoot);
            _saveLoadRoot = null;
        }

        _ctx.Dispose();
        _root = null;
        _vnServiceContainer = null;
    }
}