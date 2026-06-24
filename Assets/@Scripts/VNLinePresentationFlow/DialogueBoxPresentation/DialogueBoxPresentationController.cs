using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Surface;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private readonly DialogueBoxHost _host;
    private readonly DialogueBoxMetadataResolver _metadataResolver;
    private readonly DialogueBoxCurrentState _boxState;
    private readonly DialogueSurfaceState _surfaceState;
    private readonly DialogueSurfaceLayoutPresetDBSO _surfaceLayoutDb;
    private readonly DialogueSpeakerPresentationPolicyDBSO _speakerPolicyDb;

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(
        DialogueBoxCurrentState dialogueBoxState,
        DialogueBoxHost host,
        DialogueBoxMetadataResolver metadataResolver,
        DialogueSurfaceState surfaceState,
        DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb,
        DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb)
    {
        _boxState = dialogueBoxState;
        _host = host;
        _metadataResolver = metadataResolver;
        _surfaceState = surfaceState;
        _surfaceLayoutDb = surfaceLayoutDb;
        _speakerPolicyDb = speakerPolicyDb;
    }

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(
        DialogueBoxPresentationContext ctx)
    {
        InvalidateVisibilityTransition();

        IPresentationDialogueBoxView currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;

        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy = default;
        bool hasSpeakerPolicy = false;

        if (ctx.HasCharacterName)
        {
            hasSpeakerPolicy = _speakerPolicyDb.TryFind(
                ctx.CharacterName,
                out speakerPolicy);
        }

        DialogueBoxKind nextBoxKind = ResolveNextBoxKind(
            ctx,
            hasSpeakerPolicy,
            speakerPolicy);

        IPresentationDialogueBoxView nextBox = _host.ResolveTarget(nextBoxKind);

        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(
            ctx,
            currentBoxKind,
            nextBoxKind);

        string displayCharacterName = ctx.CharacterName;

        if (ctx.HasCharacterName &&
            hasSpeakerPolicy &&
            !string.IsNullOrWhiteSpace(speakerPolicy.fallbackDisplayName))
        {
            displayCharacterName = speakerPolicy.fallbackDisplayName;
        }

        nextBox.ResetPresentationTransform();

        if (nextBoxKind == DialogueBoxKind.Surface)
            ApplyCurrentSurfaceLayout(nextBox);

        nextBox.PrimeText(
            ctx.Text,
            displayCharacterName,
            ctx.HasCharacterName);

        await ApplyTransitionAsync(
            transitionKind,
            currentBox,
            nextBox,
            ctx.UseImmediateTransition,
            ctx.Run);

        // Only the still-valid run is allowed to commit the current box state.
        if (ctx.Run.IsValid)
            _boxState.Commit(nextBoxKind, nextBox, transitionKind);

        return new DialogueBoxPresentationResult(nextBox);
    }

    private DialogueBoxKind ResolveNextBoxKind(
        DialogueBoxPresentationContext ctx,
        bool hasSpeakerPolicy,
        DialogueSpeakerPresentationPolicyDBSO.Entry speakerPolicy)
    {
        if (_metadataResolver.TryResolveBoxKind(
                ctx.Metadata,
                out DialogueBoxKind metadataBoxKind))
            return metadataBoxKind;

        if (hasSpeakerPolicy && speakerPolicy.useBoxKindOverride)
            return speakerPolicy.boxKind;

        return ctx.HasCharacterName
            ? _namedLineBoxKind
            : _protagonistLineBoxKind;
    }

    private DialogueBoxTransitionKind ResolveTransitionKind(
        DialogueBoxPresentationContext ctx,
        DialogueBoxKind? currentBoxKind,
        DialogueBoxKind nextBoxKind)
    {
        if (ctx.UseImmediateTransition)
            return DialogueBoxTransitionKind.Cut;

        if (_metadataResolver.TryResolveTransitionKind(ctx.Metadata, out DialogueBoxTransitionKind metadataTransition))
            return metadataTransition;

        if (!_boxState.IsVisible || currentBoxKind.HasValue == false)
            return DialogueBoxTransitionKind.FadeIn;

        if (currentBoxKind.Value == nextBoxKind)
            return DialogueBoxTransitionKind.Keep;

        return DialogueBoxTransitionKind.FadeOutIn;
    }

    private void ApplyCurrentSurfaceLayout(IPresentationDialogueBoxView box)
    {
        DialogueSurfaceLayoutPresetDBSO.Entry entry =
            _surfaceLayoutDb.FindOrDefault(_surfaceState.CurrentLayoutKey);

        box.ApplySurfaceLayout(entry);
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
                    _host.HideAllDialogueBoxesExcept(nextBox);
                    nextBox.SetVisibleImmediate(true);
                }

                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (immediate)
                {
                    _host.HideAllDialogueBoxesExcept(nextBox);
                    nextBox.SetVisibleImmediate(true);
                }
                else
                {
                    _host.HideAllDialogueBoxesExcept(nextBox);
                    nextBox.PrepareHidden();
                    await nextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate)
                {
                    if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                        previousBox.SetVisibleImmediate(false);

                    _host.HideAllDialogueBoxesExcept(nextBox);
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

    public void CloseAll()
    {
        InvalidateVisibilityTransition();

        _host.HideAllDialogueBoxes();
        _boxState.Reset();
    }

    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        InvalidateVisibilityTransition();

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
