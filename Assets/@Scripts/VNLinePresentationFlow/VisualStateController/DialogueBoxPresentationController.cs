using TMPro;
using UnityEngine;
using Yarn.Unity;

// Coordinates dialogue box selection, text priming, transition playback.
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
        IDialogueTextTarget currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;
        bool currentBoxIsVisible = _boxState.IsVisible;

        // ResolveBoxKind
        DialogueBoxKind nextBoxKind;
        
        if (_metadataResolver.TryResolveBoxKind(line.Metadata, out DialogueBoxKind metadataBoxKind))
            nextBoxKind = metadataBoxKind;
        else if (line.HasCharacterName)
            nextBoxKind = _namedLineBoxKind;
        else nextBoxKind = _protagonistLineBoxKind;
        
        IDialogueTextTarget nextBox = _host.ResolveTarget(nextBoxKind);
        
        // ResolveTransitionKind
        DialogueBoxTransitionKind transitionKind;
        
        bool shouldFastForward = !options.IsSeekTargetLine && options.UseImmediateTransition;

        if (shouldFastForward)
            transitionKind = DialogueBoxTransitionKind.Cut;
        else if (_metadataResolver.TryResolveTransitionKind(line.Metadata, out DialogueBoxTransitionKind metadataTransition))
            transitionKind = metadataTransition;
        else if (!currentBoxIsVisible || currentBoxKind.HasValue == false)
            transitionKind = DialogueBoxTransitionKind.FadeIn;
        else if (currentBoxKind.Value == nextBoxKind)
            transitionKind = DialogueBoxTransitionKind.Keep;
        else
            transitionKind = DialogueBoxTransitionKind.FadeOutIn;
        
        // PlanBuilt
        DialogueBoxTransitionPlan plan = new DialogueBoxTransitionPlan(
            nextBoxKind,
            currentBox,
            nextBox,
            transitionKind,
            options.UseImmediateTransition);

        ResetBoxTransform(plan.NextBox);
        PrimeText(plan.NextBox, line);

        await ApplyTransitionAsync(plan, plan.UseImmediate, options.Run);

        if (!options.Run.IsValid)
            return DialogueBoxPresentationResult.Stale(plan);

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);

        return DialogueBoxPresentationResult.Completed(plan);
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
    
    public void CloseAll()
    {
        _host.HideAll();
        _boxState.Reset();
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

    // DialogueBoxTransitionKind를 실제 동작으로 바꾸는 유일한 지점.
    // 각 case 안에서 immediate 처리와 animated 처리를 나란히 두어, 전환별 전체 흐름을 한 곳에서 정리.
    private async YarnTask ApplyTransitionAsync(DialogueBoxTransitionPlan plan, bool immediate, LinePresentationRun run)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (immediate || run.IsValid)
                    SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
                if (immediate || run.IsValid) {
                    HideAllExcept(plan.NextBox);
                    SetVisibleImmediate(plan.NextBox, true);
                }
                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (immediate) {
                    HideAllExcept(plan.NextBox);
                    SetVisibleImmediate(plan.NextBox, true);
                }
                else {
                    HideAllExcept(plan.NextBox);
                    PrepareHidden(plan.NextBox);
                    await FadeInBoxAsync(plan.NextBox, _fadeUpDuration, run);
                }
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (immediate) {
                    if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                        SetVisibleImmediate(plan.PreviousBox, false);

                    HideAllExcept(plan.NextBox);
                    SetVisibleImmediate(plan.NextBox, true);
                }
                else {
                    PrepareHidden(plan.NextBox);

                    if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                        await FadeOutBoxAsync(plan.PreviousBox, _fadeDownDuration, run);

                    if (!run.IsValid)
                        break;

                    SetVisibleImmediate(plan.PreviousBox, false);
                    PrepareHidden(plan.NextBox);
                    await FadeInBoxAsync(plan.NextBox, _fadeUpDuration, run);
                }
                break;

            case DialogueBoxTransitionKind.Hide:
                if (immediate) {
                    SetVisibleImmediate(plan.NextBox, false);
                }
                else {
                    if (plan.NextBox != null)
                        await FadeOutBoxAsync(plan.NextBox, _fadeDownDuration, run);

                    if (run.IsValid)
                        SetVisibleImmediate(plan.NextBox, false);
                }
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
        if (view != null) {
            view.SetVisible(visible);
            SetCanvas(view.CanvasGroup, visible);
            return;
        }

        SetCanvas(box.CanvasGroup, visible);
    }

    private void ResetBoxTransform(IDialogueTextTarget box)
    {
        MonoBehaviour behaviour = box as MonoBehaviour;
        if (!behaviour)
            return;

        RectTransform rect = behaviour?.transform as RectTransform;
        if (rect == null)
            return;

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