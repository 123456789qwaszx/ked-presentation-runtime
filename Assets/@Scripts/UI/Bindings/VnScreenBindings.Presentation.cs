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
            BindMain(root, ApplyBindings);
        });
    }

    private void ApplyBindings(PresentationUIRoot root)
    {
        AddBinding(root, 
            r => r.RollbackClicked += HandleRollbackClicked, 
            r => r.RollbackClicked -= HandleRollbackClicked);
        
        AddBinding(root, 
            r => r.FastForwardDown += HandleFastForwardDown,
            r => r.FastForwardDown -= HandleFastForwardDown);
        
        AddBinding(root,
            r => r.FastForwardUp += HandleFastForwardUp,
            r => r.FastForwardUp -= HandleFastForwardUp);
        
        AddBinding(root,
            r => r.StepNextClicked += HandleStepNextClicked,
            r => r.StepNextClicked -= HandleStepNextClicked);
        
        AddBinding(root,
            r => r.SkipMenuClicked += HandleSkipMenuClicked,
            r => r.SkipMenuClicked -= HandleSkipMenuClicked);
        
        AddBinding(root,
            r => r.AutoClicked += HandleAutoClicked,
            r => r.AutoClicked -= HandleAutoClicked);
        
        AddBinding(root,
            r => r.QuickMenuClicked += HandleQuickMenuClicked,
            r => r.QuickMenuClicked -= HandleQuickMenuClicked);
        
        AddBinding(root,
            r => r.ExpandClicked += HandleExpandClicked,
            r => r.ExpandClicked -= HandleExpandClicked);
        
        AddBinding(root,
            r => r.BackLogClicked += HandleBackLogClicked,
            r => r.BackLogClicked -= HandleBackLogClicked);
        
        AddBinding(root,
            r => r.PlaybackSpeedClicked += HandlePlaybackSpeedClicked,
            r => r.PlaybackSpeedClicked -= HandlePlaybackSpeedClicked);
        
        AddBinding(root,
            r => r.SaveMenuClicked += HandleSaveMenuClicked,
            r => r.SaveMenuClicked -= HandleSaveMenuClicked);
        
        AddBinding(root,
            r => r.LoadMenuClicked += HandleLoadMenuClicked,
            r => r.LoadMenuClicked -= HandleLoadMenuClicked);
    }

    private void HandleRollbackClicked()
    {
        if (!_vnFeatures.RequestRollbackOneStep())
            return;
        
        _episodePlayer.RestartForRollback(_linePresentationAdvanceState.TargetNodeName);
    }

    private void HandleFastForwardDown()
    {
        _vnFeatures.BeginHoldSpeedUp();
    }

    private void HandleFastForwardUp()
    {
        _vnFeatures.EndHoldSpeedUp();
    }

    private void HandleStepNextClicked()
    {
        if (_vnFeatures != null && _vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            if (UIManager.Instance.GetUI<PresentationUIRoot>() != null)
                UIManager.Instance.GetUI<PresentationUIRoot>().SetAutoModeActive(false);

            return;
        }
        
        _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    private void HandleSaveMenuClicked()
    {
        if (HasPanel)
            return;
        
        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            UIManager.Instance.GetUI<PresentationUIRoot>().SetAutoModeActive(false);
        }

        UIManager.Instance.GetUI<PresentationUIRoot>().SetExpanded(false);
        UIManager.Instance.GetUI<PresentationUIRoot>().SetQuickMenuOpen(false);

        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void HandleLoadMenuClicked()
    {
        if (HasPanel)
            return;
        
        if (_vnFeatures.IsAuto)
        {
            _vnFeatures.ToggleAuto();

            UIManager.Instance.GetUI<PresentationUIRoot>().SetAutoModeActive(false);
        }

        UIManager.Instance.GetUI<PresentationUIRoot>().SetExpanded(false);
        UIManager.Instance.GetUI<PresentationUIRoot>().SetQuickMenuOpen(false);

        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandleSkipMenuClicked()
    {
        if (_uxState.ChoicesVisible || _uxState.BacklogVisible)
            return;

        string summary = "현재까지의 스토리(최근):";

        UI.PushPanel<SkipConfirmPanel>(panel =>
        {
            BindPanel(panel, BindSkipConfirmPanel);

            panel.Present(
                title: "에피소드를 스킵할까요?",
                body: summary,
                confirmLabel: "스킵하고 완료",
                cancelLabel: "취소");
        });
    }

    private void BindSkipConfirmPanel(SkipConfirmPanel panel)
    {
        AddBinding(
            panel,
            p => p.OnConfirmed += ConfirmSkipEpisode,
            p => p.OnConfirmed -= ConfirmSkipEpisode);

        AddBinding(
            panel,
            p => p.OnCancelled += CloseSkipConfirm,
            p => p.OnCancelled -= CloseSkipConfirm);
    }

    private void HandleAutoClicked()
    {
        _vnFeatures.ToggleAuto();
        UIManager.Instance.GetUI<PresentationUIRoot>().SetAutoModeActive(_vnFeatures.IsAuto);
    }

    private void HandleQuickMenuClicked()
    {
    }

    private void HandleExpandClicked()
    {
        if (_uxState.BacklogVisible)
            CloseBacklogPanel();

        if (_uxState.ChoicesVisible)
            CloseChoicePanel();
    }

    private void HandleBackLogClicked()
    {
        if (_uxState.BacklogVisible)
            return;

        _uxState.SetBacklogVisible(true);

        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindPanel(panel, BindBacklogPanel);
            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void BindBacklogPanel(BacklogPanel panel)
    {
        AddBinding(panel,
            p => p.OnCloseRequested += CloseBacklogPanel,
            p => p.OnCloseRequested -= CloseBacklogPanel);
    }

    private void HandlePlaybackSpeedClicked()
    {
        _vnFeatures.ToggleSetSpeed();
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

        if (UIManager.Instance.GetUI<PresentationUIRoot>() != null)
            UIManager.Instance.GetUI<PresentationUIRoot>().SetSkipModeActive(false);

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

            if (UIManager.Instance.GetUI<PresentationUIRoot>() != null)
                UIManager.Instance.GetUI<PresentationUIRoot>().SetAutoModeActive(false);
        }

        ChoicePanel existing = UI.GetUI<ChoicePanel>();

        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UI.PushPanel<ChoicePanel>(panel =>
        {
            BindPanel(panel, BindChoicePanel);
            panel.Present(choices);
        });
    }

    private void BindChoicePanel(ChoicePanel panel)
    {
        AddBinding(panel, p => p.OnChoiceSelected += HandleChoiceSelected, p => p.OnChoiceSelected -= HandleChoiceSelected);
        AddBinding(panel, p => p.OnCloseRequested += CloseChoicePanel, p => p.OnCloseRequested -= CloseChoicePanel);
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