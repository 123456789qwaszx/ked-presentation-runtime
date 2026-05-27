using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings
{
    private EpisodePlayState _episodePlayState;
    private VnFeatureController _vnFeatures;
    private VnUxState _uxState;
    private VnRuntimeBridge _vnRuntimeBridge;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private LinePresentationAdvanceState _linePresentationAdvanceState;

    private PresentationUIRoot _presentationRoot;

    public void ConfigurePresentationView(
        EpisodePlayState episodePlayState,
        VnFeatureController vnFeatures,
        VnUxState uxState,
        VnRuntimeBridge vnRuntimeBridge,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        LinePresentationAdvanceState linePresentationAdvanceState)
    {
        _episodePlayState = episodePlayState;
        _vnFeatures = vnFeatures;
        _uxState = uxState;
        _vnRuntimeBridge = vnRuntimeBridge;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _linePresentationAdvanceState = linePresentationAdvanceState;
    }

    public void GoToPresentationView()
    {
        UI.SwitchRoot<PresentationUIRoot>(root =>
        {
            BindMain(root, BindPresentationRootEvents);
        });
    }

    private void BindPresentationRootEvents(PresentationUIRoot root)
    {
        _presentationRoot = root;
        AddCleanup(root, () => { _presentationRoot = null; });

        BindEvent(root, r =>
                r.OnRollbackOneStepPressed += HandleRollbackPressed, 
            r => r.OnRollbackOneStepPressed -= HandleRollbackPressed);
        BindEvent(root,
            r => r.OnSpeedUpHoldStarted += HandleSpeedUpHoldStarted,
            r => r.OnSpeedUpHoldStarted -= HandleSpeedUpHoldStarted);
        BindEvent(root,
            r => r.OnSpeedUpHoldEnded += HandleSpeedUpHoldEnded,
            r => r.OnSpeedUpHoldEnded -= HandleSpeedUpHoldEnded);
        BindEvent(root,
            r => r.OnStepNextPressed += HandleStepNextPressed,
            r => r.OnStepNextPressed -= HandleStepNextPressed);
        BindEvent(root,
            r => r.OnSkipPressed += HandleSkipPressed,
            r => r.OnSkipPressed -= HandleSkipPressed);
        BindEvent(root,
            r => r.AutoClicked += HandleAutoPressed,
            r => r.AutoClicked -= HandleAutoPressed);
        BindEvent(root,
            r => r.QuickMenuClicked += HandleQuickMenuPressed,
            r => r.QuickMenuClicked -= HandleQuickMenuPressed);
        BindEvent(root,
            r => r.OnExpandPressed += HandleExpandPressed,
            r => r.OnExpandPressed -= HandleExpandPressed);
        BindEvent(root,
            r => r.OnShowPreviousLogPressed += HandleShowPreviousLogPressed,
            r => r.OnShowPreviousLogPressed -= HandleShowPreviousLogPressed);
        BindEvent(root,
            r => r.OnSetSpeedupPressed += HandleSetSpeedPressed,
            r => r.OnSetSpeedupPressed -= HandleSetSpeedPressed);
        BindEvent(root,
            r => r.OnSaveMenuPressed += HandlePresentationSaveMenuPressed,
            r => r.OnSaveMenuPressed -= HandlePresentationSaveMenuPressed);
        BindEvent(root,
            r => r.OnLoadMenuPressed += HandlePresentationLoadMenuPressed,
            r => r.OnLoadMenuPressed -= HandlePresentationLoadMenuPressed);
    }

    private void HandleRollbackPressed()
    {
        if (!_vnFeatures.RequestRollbackOneStep())
            return;
        
        _episodePlayer.RestartForRollback(_linePresentationAdvanceState.TargetNodeName);
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
        if (_vnFeatures != null && _vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            if (_presentationRoot != null)
                _presentationRoot.SetAutoModeActive(false);

            return;
        }
        
        _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void HandlePresentationSaveMenuPressed()
    {
        OpenPresentationSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void HandlePresentationLoadMenuPressed()
    {
        OpenPresentationSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandleSkipPressed()
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;

        string summary = "현재까지의 스토리(최근):";

        UI.PushPanel<SkipConfirmPanel>(panel =>
        {
            Bind(panel, BindSkipConfirmPanel);

            panel.Present(
                title: "에피소드를 스킵할까요?",
                body: summary,
                confirmLabel: "스킵하고 완료",
                cancelLabel: "취소");
        });
    }

    private void BindSkipConfirmPanel(SkipConfirmPanel panel)
    {
        BindEvent(
            panel,
            p => p.OnConfirmed += ConfirmSkipEpisode,
            p => p.OnConfirmed -= ConfirmSkipEpisode);

        BindEvent(
            panel,
            p => p.OnCancelled += CloseSkipConfirm,
            p => p.OnCancelled -= CloseSkipConfirm);
    }

    private void HandleAutoPressed()
    {
        _vnFeatures.ToggleAuto();
        _presentationRoot.SetAutoModeActive(_vnFeatures.IsAuto);
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
            Bind(panel, BindBacklogPanel);
            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void BindBacklogPanel(BacklogPanel panel)
    {
        BindEvent(panel,
            p => p.OnCloseRequested += CloseBacklogPanel,
            p => p.OnCloseRequested -= CloseBacklogPanel);
    }

    private void HandleSetSpeedPressed()
    {
        _vnFeatures.ToggleSetSpeed();
    }

    private void OpenPresentationSaveLoadMenu(SaveLoadMenuMode mode)
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;
        
        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            _presentationRoot.SetAutoModeActive(false);
        }

        _presentationRoot.SetExpanded(false);
        _presentationRoot.SetQuickMenuOpen(false);

        _currentSaveLoadMode = mode;

        UI.SwitchRoot<SaveLoadMenuUIPanel>(root =>
        {
            BindMain(root, BindPresentationSaveLoadRoot);
        });
    }

    private void BindPresentationSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        BindEvent(saveLoadRoot,
            r => r.OnSlotSelected += OnPresentationSaveLoadSlotSelected,
            r => r.OnSlotSelected -= OnPresentationSaveLoadSlotSelected);
        BindEvent(saveLoadRoot,
            r => r.OnCloseRequested += OnPresentationSaveLoadCloseRequested,
            r => r.OnCloseRequested -= OnPresentationSaveLoadCloseRequested);

        RefreshPresentationSaveLoadRoot(saveLoadRoot);
    }

    private void RefreshPresentationSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        VNSaveSlotMeta[] metas = _vnSaveLoadSystem.GetAllSaveSlotMetas();

        saveLoadRoot.Rebuild(_currentSaveLoadMode, metas);
    }

    private void OnPresentationSaveLoadSlotSelected(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            HandlePresentationSaveSlotSelected(slotIndex);
            return;
        }

        HandlePresentationLoadSlotSelected(slotIndex);
    }

    private void HandlePresentationSaveSlotSelected(int slotIndex)
    {
        if (!_vnSaveLoadSystem.SaveService.SaveManual(slotIndex))
        {
            if (_boundMain is SaveLoadMenuUIPanel saveLoadRoot)
                RefreshPresentationSaveLoadRoot(saveLoadRoot);

            return;
        }

        if (_boundMain is SaveLoadMenuUIPanel refreshedRoot)
            RefreshPresentationSaveLoadRoot(refreshedRoot);
    }

    private void HandlePresentationLoadSlotSelected(int slotIndex)
    {
        if (!_vnSaveLoadSystem.LoadService.Load(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Load failed. slotIndex={slotIndex}");
            return;
        }

        ClosePresentationSaveLoadRoot();
    }

    private void OnPresentationSaveLoadCloseRequested()
    {
        ClosePresentationSaveLoadRoot();
    }

    private void ClosePresentationSaveLoadRoot()
    {
        UI.PopPanel();
    }

    private void CloseSkipConfirm()
    {
        SkipConfirmPanel panel = UI.GetUI<SkipConfirmPanel>();

        if (panel != null)
            Unbind(panel);

        UI.PopPanel();
    }

    private void ConfirmSkipEpisode()
    {
        CloseSkipConfirm();

        string episodeId = _episodePlayState.SelectedEpisodeId;

        if (string.IsNullOrEmpty(episodeId))
            Debug.LogWarning("[VN] Skip confirmed but current episode id is empty.");

        if (_presentationRoot != null)
            _presentationRoot.SetSkipModeActive(false);

        _vnRuntimeBridge.ForceCompleteEpisodeNow(episodeId);
        _episodePlayState.ApplyEpisodeState(episodeId);

        UI.PopAllPanels();
        GoToTitle();
    }

    private void CloseBacklogPanel()
    {
        _uxState.SetBacklogVisible(false);

        BacklogPanel panel = UI.GetUI<BacklogPanel>();

        if (panel != null)
            Unbind(panel);

        UI.PopPanel();
    }

    private void HandleChoicesPresented(IReadOnlyList<string> choices)
    {
        _uxState.SetChoicesVisible(true);

        if (_vnFeatures != null && _vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            if (_presentationRoot != null)
                _presentationRoot.SetAutoModeActive(false);
        }

        ChoicePanel existing = UI.GetUI<ChoicePanel>();

        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UI.PushPanel<ChoicePanel>(panel =>
        {
            Bind(panel, BindChoicePanel);
            panel.Present(choices);
        });
    }

    private void BindChoicePanel(ChoicePanel panel)
    {
        BindEvent(panel, p => p.OnChoiceSelected += HandleChoiceSelected, p => p.OnChoiceSelected -= HandleChoiceSelected);
        BindEvent(panel, p => p.OnCloseRequested += CloseChoicePanel, p => p.OnCloseRequested -= CloseChoicePanel);
    }

    private void HandleChoiceSelected(int index)
    {
    }

    private void CloseChoicePanel()
    {
        _uxState.SetChoicesVisible(false);

        ChoicePanel panel = UI.GetUI<ChoicePanel>();

        if (panel != null)
            Unbind(panel);

        UI.PopPanel();
    }
}