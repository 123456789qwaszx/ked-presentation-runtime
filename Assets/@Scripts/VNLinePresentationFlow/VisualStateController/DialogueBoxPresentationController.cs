using Yarn.Unity;

public sealed class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Portrait;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private readonly DialogueBoxHost _host;
    private readonly DialogueBoxMetadataResolver _metadataResolver;

    private readonly DialogueBoxCurrentState _boxState = new ();

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(DialogueBoxHost host, DialogueBoxMetadataResolver metadataResolver)
    {
        _host = host;
        _metadataResolver = metadataResolver;
    }
    
    public void SetProtagonistLineBoxKind(DialogueBoxKind kind) => _protagonistLineBoxKind = kind;
    public void SetNamedLineBoxKind(DialogueBoxKind kind) => _namedLineBoxKind = kind;
    
    public void ResetDefaultLineBoxKinds()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;
    }
    
    public void CloseAll()
    {
        _host.HideAll();
        _boxState.Reset();
    }
    
    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(
        VNDialogueLine line,
        DialogueBoxPresentationOptions options)
    {
        IPresentationDialogueBoxView currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;

        // ResolveBoxKind
        DialogueBoxKind nextBoxKind;
        
        if (_metadataResolver.TryResolveBoxKind(line.Metadata, out DialogueBoxKind metadataBoxKind))
            nextBoxKind = metadataBoxKind;
        else if (line.HasCharacterName)
            nextBoxKind = _namedLineBoxKind;
        else nextBoxKind = _protagonistLineBoxKind;
        
        IPresentationDialogueBoxView nextBox = _host.ResolveTarget(nextBoxKind);
        
        // ResolveTransitionKind
        DialogueBoxTransitionKind transitionKind;
        
        bool shouldFastForward = !options.IsSeekTargetLine && options.UseImmediateTransition;

        if (shouldFastForward)
            transitionKind = DialogueBoxTransitionKind.Cut;
        else if (_metadataResolver.TryResolveTransitionKind(line.Metadata, out DialogueBoxTransitionKind metadataTransition))
            transitionKind = metadataTransition;
        else if (!_boxState.IsVisible || currentBoxKind.HasValue == false)
            transitionKind = DialogueBoxTransitionKind.FadeIn;
        else if (currentBoxKind.Value == nextBoxKind)
            transitionKind = DialogueBoxTransitionKind.Keep;
        else
            transitionKind = DialogueBoxTransitionKind.FadeOutIn;
        
        // PlanBuilt
        DialogueBoxTransitionPlan plan = new(
            nextBoxKind,
            currentBox,
            nextBox,
            transitionKind,
            options.UseImmediateTransition);
        
        plan.NextBox.ResetPresentationTransform();
        plan.NextBox.PrimeText(line);

        await ApplyTransitionAsync(plan, plan.UseImmediate, options.Run);

        if (!options.Run.IsValid)
            return DialogueBoxPresentationResult.Stale(plan);

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);

        return DialogueBoxPresentationResult.Completed(plan);
    }
    
    private async YarnTask ApplyTransitionAsync(
        DialogueBoxTransitionPlan plan,
        bool immediate,
        LinePresentationRun run)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (immediate || run.IsValid)
                    plan.NextBox.SetVisibleImmediate(true);
                
                break;

            case DialogueBoxTransitionKind.Cut:
                if (immediate || run.IsValid) 
                {
                    _host.HideAllExcept(plan.NextBox);
                    plan.NextBox.SetVisibleImmediate(true);
                }

                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (immediate) 
                {
                    _host.HideAllExcept(plan.NextBox);
                    plan.NextBox.SetVisibleImmediate(true);
                }
                else 
                {
                    _host.HideAllExcept(plan.NextBox);
                    plan.NextBox.PrepareHidden();
                    await plan.NextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate) 
                {
                    if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                        plan.PreviousBox.SetVisibleImmediate(false);

                    _host.HideAllExcept(plan.NextBox);
                    plan.NextBox.SetVisibleImmediate(true);
                }
                else 
                {
                    plan.NextBox.PrepareHidden();

                    if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                        await plan.PreviousBox.FadeOutAsync(_fadeDownDuration, run);

                    if (!run.IsValid)
                        break;

                    if (plan.PreviousBox != null)
                        plan.PreviousBox.SetVisibleImmediate(false);

                    plan.NextBox.PrepareHidden();
                    await plan.NextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.Hide:
                if (immediate) 
                {
                    plan.NextBox.SetVisibleImmediate(false);
                }
                else 
                {
                    if (plan.NextBox != null)
                        await plan.NextBox.FadeOutAsync(_fadeDownDuration, run);

                    if (run.IsValid && plan.NextBox != null)
                        plan.NextBox.SetVisibleImmediate(false);
                }

                break;
        }
    }
    
    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        IPresentationDialogueBoxView previousBox = result.Plan.PreviousBox;
        IPresentationDialogueBoxView nextBox = result.Plan.NextBox;

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            nextBox.SetVisibleImmediate(false);

        if (previousBox != null &&
            !ReferenceEquals(previousBox, _boxState.Box) 
            && !ReferenceEquals(previousBox, nextBox))
            previousBox.SetVisibleImmediate(false);

        if (_boxState.IsVisible && _boxState.Box != null)
            _boxState.Box.SetVisibleImmediate(true);
    }
}