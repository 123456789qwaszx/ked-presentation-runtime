using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultLineBoxKind = DialogueBoxKind.Surface;

    private readonly IPresentationDialogueBoxView _box;
    private readonly DialogueBoxMetadataResolver _metadataResolver;
    private readonly DialogueBoxCurrentState _boxState;
    private readonly DialogueSurfaceState _surfaceState;
    private readonly DialogueSurfaceLayoutPresetDBSO _surfaceLayoutDb;
    private readonly DialogueSpeakerPresentationPolicyDBSO _speakerPolicyDb;

    private DialogueBoxKind _lineBoxKind = DefaultLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(
        DialogueBoxCurrentState dialogueBoxState,
        IPresentationDialogueBoxView box,
        DialogueBoxMetadataResolver metadataResolver,
        DialogueSurfaceState surfaceState,
        DialogueSurfaceLayoutPresetDBSO surfaceLayoutDb,
        DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb)
    {
        _boxState = dialogueBoxState;
        _box = box;
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

        IPresentationDialogueBoxView nextBox = _box;

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

        ApplySurfaceLayoutFor(nextBox, nextBoxKind);

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

        return _lineBoxKind;
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

    // 레이아웃 결정: surface_layout 오버라이드 or 없으면 kind가 고른 프리셋.
    private void ApplySurfaceLayoutFor(IPresentationDialogueBoxView box, DialogueBoxKind kind)
    {
        DialogueSurfaceLayoutPresetDBSO.Entry entry = _surfaceState.HasOverride
            ? _surfaceLayoutDb.FindOrDefault(_surfaceState.OverrideLayoutKey)
            : _surfaceLayoutDb.FindByKind(kind);

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
                    nextBox.SetVisibleImmediate(true);
                }

                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (immediate)
                {
                    nextBox.SetVisibleImmediate(true);
                }
                else
                {
                    nextBox.PrepareHidden();
                    await nextBox.FadeInAsync(_fadeUpDuration, run);
                }

                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate)
                {
                    if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                        previousBox.SetVisibleImmediate(false);

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

        _box.SetVisibleImmediate(false);
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