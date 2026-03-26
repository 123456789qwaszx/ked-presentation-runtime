using System;
using System.Collections.Generic;

public sealed class DialogueUIBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();

    private readonly UIManager _ui;
    
    private readonly EpisodePlayState _episodePlayState;

    public DialogueUIBindings(
        EpisodePlayState episodePlayState
    )
    {
        _episodePlayState = episodePlayState;
    }

    public void Bind(DialogueUIRoot root)
    {
        // StepNext
        _ctx.Bind(root,
            r => r.OnStepNextPressed += HandleStepNextPressed,
            r => r.OnStepNextPressed -= HandleStepNextPressed);

        // Skip
        _ctx.Bind(root,
            r => r.OnSkipPressed += HandleSkipPressed,
            r => r.OnSkipPressed -= HandleSkipPressed);

        // Auto
        _ctx.Bind(root,
            r => r.OnAutoPressed += HandleAutoPressed,
            r => r.OnAutoPressed -= HandleAutoPressed);

        // QuickMenu
        _ctx.Bind(root,
            r => r.OnQuickMenuPressed += HandleQuickMenuPressed,
            r => r.OnQuickMenuPressed -= HandleQuickMenuPressed);

        // Expand (HUD hide/show)
        _ctx.Bind(root,
            r => r.OnExpandPressed += HandleExpandPressed,
            r => r.OnExpandPressed -= HandleExpandPressed);

        // Previous log
        _ctx.Bind(root,
            r => r.OnShowPreviousLogPressed += HandleShowPreviousLogPressed,
            r => r.OnShowPreviousLogPressed -= HandleShowPreviousLogPressed);

        // Speedup
        _ctx.Bind(root,
            r => r.OnSetSpeedupPressed += HandleSpeedupPressed,
            r => r.OnSetSpeedupPressed -= HandleSpeedupPressed);
    }

    private void HandleStepNextPressed()
    { }

    private void HandleSkipPressed()
    { }

    private void CloseSkipConfirm()
    { }

    
    private void ConfirmSkipEpisode()
    {
        CloseSkipConfirm();

        _ui.GetUI<DialogueUIRoot>()?.SetSkipModeActive(false);
        _episodePlayState?.ForceCompleteCurrentEpisodeNow();
    }
    
    private void HandleAutoPressed()
    { }

    private void HandleQuickMenuPressed()
    { }

    private void HandleExpandPressed()
    { }

    private void HandleShowPreviousLogPressed()
    { }

    private void CloseBacklogPanel()
    { }

    private void HandleSpeedupPressed()
    { }

    private void HandleChoicesPresented(IReadOnlyList<string> choices)
    { }

    private void HandleChoiceSelected(int index) 
    { }

    private void CloseChoicePanel()
    { }
    
    public void Dispose()
    {
        _ctx.Dispose();
    }
}