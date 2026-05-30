using UnityEngine;
using Yarn.Unity;

public sealed class DialogueBoxPresentationController
{
    private readonly DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private readonly DialogueBoxHost _dialogueBoxResolver;
    private readonly DialogueBoxTransitionPolicy _boxTransitionPolicy;
    private readonly DialogueTextRouter _dialogueTextRouter;
    private readonly DialogueBoxTextPrimer _textPrimer;
    private readonly DialogueBoxTransitionRunner _transitionRunner;

    private readonly DialogueBoxCurrentState _boxState = new ();
    
    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        DialogueBoxHost dialogueBoxResolver,
        DialogueBoxTransitionPolicy boxTransitionPolicy,
        DialogueTextRouter dialogueTextRouter,
        DialogueBoxTextPrimer textPrimer,
        DialogueBoxTransitionRunner transitionRunner)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _boxTransitionPolicy = boxTransitionPolicy;
        _dialogueTextRouter = dialogueTextRouter;
        _textPrimer = textPrimer;
        _transitionRunner = transitionRunner;
    }

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        if (line == null || !options.Run.IsValid)
            return new DialogueBoxPresentationResult(null, true);
        
        DialogueBoxTransitionPlan plan = BuildPlan(line, options);
        
        _transitionRunner.ResetBoxTransform(plan.NextBox);

        _textPrimer.Prime(plan.NextBox, line);

        _transitionRunner.Prepare(plan);

        if (plan.UseImmediate)
            _transitionRunner.ApplyImmediate(plan);
        else 
            await _transitionRunner.ApplyAsync(plan, _fadeUpDuration, _fadeDownDuration, options.Run);
        
        if (!options.Run.IsValid)
            return new DialogueBoxPresentationResult(plan, true);

        Commit(plan);

        _dialogueTextRouter.Bind(_boxState);

        return new DialogueBoxPresentationResult(plan, false);
    }

    public void HideAllForSeek()
    {
        _transitionRunner.HideAll();
        _dialogueTextRouter.Clear();
        _boxState.Reset();
    }

    public void CloseAll()
    {
        _transitionRunner.HideAll();
        _dialogueTextRouter.Clear();
        _boxState.Reset();
    }

    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        if (result == null || result.Plan == null)
            return;

        IDialogueTextTarget previousBox = result.Plan.PreviousBox;
        IDialogueTextTarget nextBox = result.Plan.NextBox;

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            _transitionRunner.SetVisibleImmediate(nextBox, false);

        if (previousBox != null && !ReferenceEquals(previousBox, _boxState.Box) && !ReferenceEquals(previousBox, nextBox))
            _transitionRunner.SetVisibleImmediate(previousBox, false);
        
        if (_boxState.IsVisible && _boxState.Box != null)
            _transitionRunner.SetVisibleImmediate(_boxState.Box, true);
    }

    private DialogueBoxTransitionPlan BuildPlan(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        IDialogueTextTarget currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;
        bool currentBoxIsVisible = _boxState.IsVisible;

        DialogueBoxKind nextBoxKind = _lineRoutingPolicy.Resolve(line.Metadata, line.HasCharacterName);
        IDialogueTextTarget nextBox = _dialogueBoxResolver.ResolveTarget(nextBoxKind);

        bool shouldTreatAsFastForwardForPolicy = !options.IsSeekTargetLine && options.UseImmediateTransition;

        DialogueBoxTransitionKind transitionKind =
            _boxTransitionPolicy.Resolve(
                currentBoxKind,
                currentBoxIsVisible,
                nextBoxKind,
                line.Metadata,
                shouldTreatAsFastForwardForPolicy);
        
        DialogueBoxTransitionPlan plan = new DialogueBoxTransitionPlan(
            nextBoxKind,
            currentBox,
            nextBox,
            transitionKind,
            options.UseImmediateTransition);

        return plan;
    }

    private void Commit(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);
    }
}