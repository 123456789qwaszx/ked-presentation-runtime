using Yarn.Unity;

public sealed class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Portrait;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private readonly DialogueBoxHost _host;
    private readonly DialogueBoxMetadataResolver _metadataResolver;
    private readonly DialogueBoxCurrentState _boxState;

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(DialogueBoxCurrentState dialogueBoxState, DialogueBoxHost host, DialogueBoxMetadataResolver metadataResolver)
    {
        _boxState = dialogueBoxState;
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

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(DialogueBoxPresentationContext ctx)
    {
        IPresentationDialogueBoxView currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;

        // Resolve box kind
        DialogueBoxKind nextBoxKind;

        if (_metadataResolver.TryResolveBoxKind(ctx.Metadata, out DialogueBoxKind metadataBoxKind))
            nextBoxKind = metadataBoxKind;
        else if (ctx.HasCharacterName)
            nextBoxKind = _namedLineBoxKind;
        else 
            nextBoxKind = _protagonistLineBoxKind;

        IPresentationDialogueBoxView nextBox = _host.ResolveTarget(nextBoxKind);

        // Resolve transition kind
        DialogueBoxTransitionKind transitionKind;

        bool shouldCutSilently = !ctx.IsSeekTargetLine && ctx.UseImmediateTransition;

        if (shouldCutSilently)
            transitionKind = DialogueBoxTransitionKind.Cut;
        else if (_metadataResolver.TryResolveTransitionKind(ctx.Metadata, out DialogueBoxTransitionKind metadataTransition))
            transitionKind = metadataTransition;
        else if (!_boxState.IsVisible || currentBoxKind.HasValue == false)
            transitionKind = DialogueBoxTransitionKind.FadeIn;
        else if (currentBoxKind.Value == nextBoxKind)
            transitionKind = DialogueBoxTransitionKind.Keep;
        else
            transitionKind = DialogueBoxTransitionKind.FadeOutIn;

        // Prime target box
        nextBox.ResetPresentationTransform();
        nextBox.PrimeText(
            ctx.Text,
            ctx.CharacterName,
            ctx.HasCharacterName);

        // Apply transition
        await ApplyTransitionAsync(
            transitionKind,
            currentBox,
            nextBox,
            ctx.UseImmediateTransition,
            ctx.Run);
        
        // Commit.
        // Only the still-valid run is allowed to commit the current box state.
        if (ctx.Run.IsValid)
            _boxState.Commit(nextBoxKind, nextBox, transitionKind);
        
        return new DialogueBoxPresentationResult(nextBox);
    }

    private async YarnTask ApplyTransitionAsync(
        DialogueBoxTransitionKind transitionKind,
        IPresentationDialogueBoxView previousBox,
        IPresentationDialogueBoxView nextBox,
        bool immediate,
        LinePresentationRun run)
    {
        switch (transitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (immediate || run.IsValid)
                    nextBox.SetVisibleImmediate(true);

                break;

            case DialogueBoxTransitionKind.Cut:
                if (immediate || run.IsValid)
                {
                    _host.HideAllExcept(nextBox);
                    nextBox.SetVisibleImmediate(true);
                }

                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (immediate)
                {
                    _host.HideAllExcept(nextBox);
                    nextBox.SetVisibleImmediate(true);
                }
                else
                {
                    _host.HideAllExcept(nextBox);
                    nextBox.PrepareHidden();
                    await nextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate)
                {
                    if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                        previousBox.SetVisibleImmediate(false);

                    _host.HideAllExcept(nextBox);
                    nextBox.SetVisibleImmediate(true);
                }
                else
                {
                    nextBox.PrepareHidden();

                    if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                        await previousBox.FadeOutAsync(_fadeDownDuration, run);

                    if (!run.IsValid)
                        break;

                    if (previousBox != null)
                        previousBox.SetVisibleImmediate(false);

                    nextBox.PrepareHidden();
                    await nextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.Hide:
                if (immediate)
                {
                    if (nextBox != null)
                        nextBox.SetVisibleImmediate(false);
                }
                else
                {
                    if (nextBox != null)
                        await nextBox.FadeOutAsync(_fadeDownDuration, run);

                    if (run.IsValid && nextBox != null)
                        nextBox.SetVisibleImmediate(false);
                }

                break;
        }
    }

    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        IPresentationDialogueBoxView abortedTarget = result.NextBox;
        
        bool IsCurrentBox(IPresentationDialogueBoxView box) 
            => ReferenceEquals(box, _boxState.Box);

        // Hide the stale box left behind by the aborted transition.
        if (abortedTarget != null && !IsCurrentBox(abortedTarget))
            abortedTarget.SetVisibleImmediate(false);

        // Restore the committed visibility of the current box.
        if (_boxState.IsVisible && _boxState.Box != null)
            _boxState.Box.SetVisibleImmediate(true);
    }
}