using TMPro;
using UnityEngine;
using Yarn.Unity;

// Coordinates dialogue box selection, text priming, transition playback,
// stale-run cleanup, and current-box state commit for one VN line.
// This class owns dialogue box visual state, but not the full VN line lifecycle.
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

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        DialogueBoxTransitionPlan plan = BuildPlan(line, options);

        ResetBoxTransform(plan.NextBox);
        PrimeText(plan.NextBox, line);
        PrepareTransition(plan);

        if (plan.UseImmediate)
            ApplyImmediate(plan);
        else
            await ApplyAsync(plan, _fadeUpDuration, _fadeDownDuration, options.Run);

        if (!options.Run.IsValid)
            return DialogueBoxPresentationResult.Stale(plan);

        Commit(plan);

        return DialogueBoxPresentationResult.Completed(plan);
    }

    public void HideAllForSeek()
    {
        HideAll();
        _boxState.Reset();
    }

    public void CloseAll()
    {
        HideAll();
        _boxState.Reset();
    }

    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        IDialogueTextTarget previousBox = result.Plan.PreviousBox;
        IDialogueTextTarget nextBox = result.Plan.NextBox;

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            SetVisibleImmediate(nextBox, false);

        if (previousBox != null && !ReferenceEquals(previousBox, _boxState.Box) && !ReferenceEquals(previousBox, nextBox))
            SetVisibleImmediate(previousBox, false);
        
        if (_boxState.IsVisible && _boxState.Box != null)
            SetVisibleImmediate(_boxState.Box, true);
    }

    private DialogueBoxTransitionPlan BuildPlan(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        IDialogueTextTarget currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;
        bool currentBoxIsVisible = _boxState.IsVisible;

        DialogueBoxKind nextBoxKind = ResolveBoxKind(line.Metadata, line.HasCharacterName);
        IDialogueTextTarget nextBox = _host.ResolveTarget(nextBoxKind);

        bool shouldFastForward = !options.IsSeekTargetLine && options.UseImmediateTransition;

        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(
            currentBoxKind,
            currentBoxIsVisible,
            nextBoxKind,
            line.Metadata,
            shouldFastForward);

        DialogueBoxTransitionPlan plan = new DialogueBoxTransitionPlan(
            nextBoxKind,
            currentBox,
            nextBox,
            transitionKind,
            options.UseImmediateTransition);

        return plan;
    }

    private DialogueBoxKind ResolveBoxKind(string[] metadata, bool hasCharacterName)
    {
        if (_metadataResolver.TryResolveBoxKind(metadata, out DialogueBoxKind metadataBoxKind))
            return metadataBoxKind;
        
        else if (hasCharacterName)
            return _namedLineBoxKind;
        else 
            return _protagonistLineBoxKind;
    }

    private DialogueBoxTransitionKind ResolveTransitionKind(
        DialogueBoxKind? currentBoxKind,
        bool isBoxVisible,
        DialogueBoxKind nextBoxKind,
        string[] metadata,
        bool consumeSilently)
    {
        if (consumeSilently)
            return DialogueBoxTransitionKind.Cut;

        if (_metadataResolver.TryResolveTransitionKind(metadata, out DialogueBoxTransitionKind metadataTransition))
            return metadataTransition;

        if (!isBoxVisible || currentBoxKind.HasValue == false)
            return DialogueBoxTransitionKind.FadeIn;

        if (currentBoxKind.Value == nextBoxKind)
            return DialogueBoxTransitionKind.Keep;

        return DialogueBoxTransitionKind.FadeOutIn;
    }

    private void PrimeText(IDialogueTextTarget target, VNDialogueLine line)
    {
        TMP_Text lineText = target.LineText;
        if (lineText != null)
        {
            lineText.text = line.Text;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
        }

        TMP_Text nameText = target.NameText;
        if (nameText != null)
        {
            bool showName = line.HasCharacterName;

            nameText.text = showName
                ? line.CharacterName
                : string.Empty;

            nameText.gameObject.SetActive(showName);
        }
    }

    private void PrepareTransition(DialogueBoxTransitionPlan plan)
    {
        IDialogueTextTarget nextBox = plan.NextBox;

        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                break;

            case DialogueBoxTransitionKind.Cut:
                HideAllExcept(nextBox);
                SetVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.FadeIn:
                HideAllExcept(nextBox);
                PrepareHidden(nextBox);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                PrepareHidden(nextBox);
                break;

            case DialogueBoxTransitionKind.Hide:
                break;
        }
    }

    private async YarnTask ApplyAsync(DialogueBoxTransitionPlan plan, float fadeUpDuration, float fadeDownDuration, LinePresentationRun run)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (run.IsValid)
                    SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
                if (run.IsValid)
                    ApplyImmediate(plan);
                break;

            case DialogueBoxTransitionKind.FadeIn:
                await FadeInBoxAsync(plan.NextBox, fadeUpDuration, run);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                    await FadeOutBoxAsync(plan.PreviousBox, fadeDownDuration, run);

                if (!run.IsValid)
                    break;

                SetVisibleImmediate(plan.PreviousBox, false);
                PrepareHidden(plan.NextBox);

                await FadeInBoxAsync(plan.NextBox, fadeUpDuration, run);
                break;

            case DialogueBoxTransitionKind.Hide:
                if (plan.NextBox != null)
                    await FadeOutBoxAsync(plan.NextBox, fadeDownDuration, run);

                if (run.IsValid)
                    SetVisibleImmediate(plan.NextBox, false);
                break;
        }
    }

    private void ApplyImmediate(DialogueBoxTransitionPlan plan)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
            case DialogueBoxTransitionKind.FadeIn:
                HideAllExcept(plan.NextBox);
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                    SetVisibleImmediate(plan.PreviousBox, false);

                HideAllExcept(plan.NextBox);
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Hide:
                SetVisibleImmediate(plan.NextBox, false);
                break;
        }
    }

    private async YarnTask FadeInBoxAsync(IDialogueTextTarget box, float duration, LinePresentationRun run)
    {
        if (!run.IsValid)
            return;

        CanvasGroup canvasGroup = box.CanvasGroup;

        SetVisibleImmediate(box, true);
        canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(canvasGroup, 0f, 1f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        SetCanvas(canvasGroup, true);
    }

    private async YarnTask FadeOutBoxAsync(IDialogueTextTarget box, float duration, LinePresentationRun run)
    {
        if (!run.IsValid)
            return;

        CanvasGroup canvasGroup = box.CanvasGroup;
        float fromAlpha = canvasGroup.alpha;

        await Effects
            .FadeAlphaAsync(canvasGroup, fromAlpha, 0f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;
        
        SetCanvas(canvasGroup, false);
    }

    private void Commit(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);
    }
    
    private void HideAll()
    {
        _host.HideAll();
    }

    private void HideAllExcept(IDialogueTextTarget keep)
    {
        _host.HideAllExcept(keep);
    }

    private void PrepareHidden(IDialogueTextTarget box)
    {
        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
            view.SetVisible(true);

        SetCanvas(box.CanvasGroup, false);
    }

    private void SetVisibleImmediate(IDialogueTextTarget box, bool visible)
    {
        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
        {
            view.SetVisible(visible);
            SetCanvas(view.CanvasGroup, visible);
            return;
        }

        SetCanvas(box.CanvasGroup, visible);
    }

    private void ResetBoxTransform(IDialogueTextTarget box)
    {
        MonoBehaviour behaviour = box as MonoBehaviour;
        RectTransform rect = behaviour?.transform as RectTransform;
        
        rect.localPosition = Vector3.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    
    public void SetProtagonistLineBoxKind(DialogueBoxKind kind)
    {
        _protagonistLineBoxKind = kind;
    }

    public void SetNamedLineBoxKind(DialogueBoxKind kind)
    {
        _namedLineBoxKind = kind;
    }

    public void ResetDefaultLineBoxKinds()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;
    }
    
    private static void SetCanvas(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}