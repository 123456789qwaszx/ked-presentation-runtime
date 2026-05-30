using TMPro;
using UnityEngine;
using Yarn.Unity;

public sealed class DialogueBoxPresentationController
{
    private const DialogueBoxKind DefaultProtagonistLineBoxKind = DialogueBoxKind.Portrait;
    private const DialogueBoxKind DefaultNamedLineBoxKind = DialogueBoxKind.Speaker;

    private readonly DialogueBoxHost _host;
    private readonly VNTraceStream _trace;

    private readonly DialogueBoxCurrentState _boxState = new ();

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationPhase CurrentPhase { get; private set; } = DialogueBoxPresentationPhase.None;

    public DialogueBoxPresentationController(DialogueBoxHost host, VNTraceStream trace = null)
    {
        _host = host;
        _trace = trace;
    }

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        SetPhase(DialogueBoxPresentationPhase.LineReceived);

        DialogueBoxTransitionPlan plan = BuildPlan(line, options);
        SetPhase(DialogueBoxPresentationPhase.PlanBuilt);

        ResetBoxTransform(plan.NextBox);
        PrimeText(plan.NextBox, line);
        SetPhase(DialogueBoxPresentationPhase.TextPrimed);

        PrepareTransition(plan);
        SetPhase(DialogueBoxPresentationPhase.TransitionPrepared);

        SetPhase(DialogueBoxPresentationPhase.TransitionApplying);
        if (plan.UseImmediate)
            ApplyImmediate(plan);
        else
            await ApplyAsync(plan, _fadeUpDuration, _fadeDownDuration, options.Run);

        if (!options.Run.IsValid)
        {
            SetPhase(DialogueBoxPresentationPhase.Stale);
            Trace("ShowLineStale", FormatPlan(plan, line, options));
            return DialogueBoxPresentationResult.Stale(plan);
        }

        Commit(plan);
        SetPhase(DialogueBoxPresentationPhase.Committed);
        
        SetPhase(DialogueBoxPresentationPhase.Completed);
        Trace("ShowLineCompleted", FormatPlan(plan, line, options));

        return DialogueBoxPresentationResult.Completed(plan);
    }

    public void HideAllForSeek()
    {
        HideAll();
        _boxState.Reset();

        SetPhase(DialogueBoxPresentationPhase.None);
        Trace("HideAllForSeek");
    }

    public void CloseAll()
    {
        HideAll();
        _boxState.Reset();

        SetPhase(DialogueBoxPresentationPhase.None);
        Trace("CloseAll");
    }

    public void CleanupStale(DialogueBoxPresentationResult result)
    {
        if (result == null || result.Plan == null)
            return;

        IDialogueTextTarget previousBox = result.Plan.PreviousBox;
        IDialogueTextTarget nextBox = result.Plan.NextBox;

        Trace(
            "CleanupStale",
            $"previous={GetBoxName(previousBox)}, next={GetBoxName(nextBox)}, current={GetBoxName(_boxState.Box)}");

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            SetVisibleImmediate(nextBox, false);

        if (previousBox != null &&
            !ReferenceEquals(previousBox, _boxState.Box) &&
            !ReferenceEquals(previousBox, nextBox))
        {
            SetVisibleImmediate(previousBox, false);
        }

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

        bool shouldTreatAsFastForwardForPolicy = !options.IsSeekTargetLine && options.UseImmediateTransition;

        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(
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

        Trace("BuildPlan", FormatPlan(plan, line, options));

        return plan;
    }

    private DialogueBoxKind ResolveBoxKind(string[] metadata, bool hasCharacterName)
    {
        if (TryResolveBoxKindFromMetadata(metadata, out DialogueBoxKind metadataBoxKind))
            return metadataBoxKind;

        return hasCharacterName
            ? _namedLineBoxKind
            : _protagonistLineBoxKind;
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

        if (TryResolveTransitionFromMetadata(metadata, out DialogueBoxTransitionKind metadataTransition))
            return metadataTransition;

        if (!isBoxVisible || currentBoxKind.HasValue == false)
            return DialogueBoxTransitionKind.FadeIn;

        if (currentBoxKind.Value == nextBoxKind)
            return DialogueBoxTransitionKind.Keep;

        return DialogueBoxTransitionKind.FadeOutIn;
    }

    private void PrimeText(IDialogueTextTarget target, VNDialogueLine line)
    {
        if (target == null || line == null)
            return;

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

        Trace("PrimeText", $"line={line.TextId}, box={GetBoxName(target)}");
    }

    private void PrepareTransition(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

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

        Trace("PrepareTransition", $"transition={plan.TransitionKind}, next={GetBoxName(nextBox)}");
    }

    private async YarnTask ApplyAsync(
        DialogueBoxTransitionPlan plan,
        float fadeUpDuration,
        float fadeDownDuration,
        LinePresentationRun run)
    {
        if (plan == null)
            return;

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
        if (plan == null)
            return;

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

        Trace("ApplyImmediate", $"transition={plan.TransitionKind}, next={GetBoxName(plan.NextBox)}");
    }

    private async YarnTask FadeInBoxAsync(
        IDialogueTextTarget box,
        float duration,
        LinePresentationRun run)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        if (run == null || !run.IsValid)
            return;

        CanvasGroup canvasGroup = box.CanvasGroup;

        SetVisibleImmediate(box, true);
        canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(canvasGroup, 0f, 1f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private async YarnTask FadeOutBoxAsync(
        IDialogueTextTarget box,
        float duration,
        LinePresentationRun run)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        if (run == null || !run.IsValid)
            return;

        CanvasGroup canvasGroup = box.CanvasGroup;
        float fromAlpha = canvasGroup.alpha;

        await Effects
            .FadeAlphaAsync(canvasGroup, fromAlpha, 0f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Commit(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);
        Trace("Commit", $"kind={plan.NextKind}, box={GetBoxName(plan.NextBox)}, transition={plan.TransitionKind}");
    }
    
    private void HideAll()
    {
        if (_host != null)
        {
            _host.HideAll();
            return;
        }
    }

    private void HideAllExcept(IDialogueTextTarget keep)
    {
        if (_host != null)
        {
            _host.HideAllExcept(keep);
            return;
        }

        if (keep != null)
            SetVisibleImmediate(keep, true);
    }

    private void PrepareHidden(IDialogueTextTarget box)
    {
        if (box == null)
            return;

        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
            view.SetVisible(true);

        CanvasGroup canvasGroup = box.CanvasGroup;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void SetVisibleImmediate(IDialogueTextTarget box, bool visible)
    {
        if (box == null)
            return;

        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
        {
            view.SetVisible(visible);

            CanvasGroup viewCanvasGroup = view.CanvasGroup;
            if (viewCanvasGroup != null)
            {
                viewCanvasGroup.alpha = visible ? 1f : 0f;
                viewCanvasGroup.interactable = visible;
                viewCanvasGroup.blocksRaycasts = visible;
            }

            return;
        }

        CanvasGroup canvasGroup = box.CanvasGroup;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private void ResetBoxTransform(IDialogueTextTarget box)
    {
        if (box == null)
            return;

        MonoBehaviour behaviour = box as MonoBehaviour;
        if (behaviour == null)
            return;

        RectTransform rect = behaviour.transform as RectTransform;
        if (rect != null)
        {
            rect.localPosition = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
            return;
        }

        behaviour.transform.localPosition = Vector3.zero;
    }

    private void SetPhase(DialogueBoxPresentationPhase phase)
    {
        CurrentPhase = phase;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state =
            $"phase={CurrentPhase}, " +
            $"boxKind={(_boxState.BoxKind.HasValue ? _boxState.BoxKind.Value.ToString() : "null")}, " +
            $"box={GetBoxName(_boxState.Box)}, " +
            $"visible={_boxState.IsVisible}";

        _trace.Trace(
            nameof(DialogueBoxPresentationController),
            evt,
            state,
            note);
    }

    private static bool TryResolveBoxKindFromMetadata(
        string[] metadata,
        out DialogueBoxKind kind)
    {
        kind = default(DialogueBoxKind);

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = metadata[i];

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            tag = tag.Trim().ToLowerInvariant();

            switch (tag)
            {
                case "portrait":
                case "box:portrait":
                case "box=portrait":
                    kind = DialogueBoxKind.Portrait;
                    return true;

                case "speaker":
                case "box:speaker":
                case "box=speaker":
                    kind = DialogueBoxKind.Speaker;
                    return true;

                case "letterbox":
                case "letter_box":
                case "box:letterbox":
                case "box=letterbox":
                    kind = DialogueBoxKind.LetterBox;
                    return true;

                case "onlytext":
                case "only_text":
                case "box:onlytext":
                case "box=onlytext":
                    kind = DialogueBoxKind.OnlyText;
                    return true;

                case "blackbook":
                case "black_book":
                case "box:blackbook":
                case "box=blackbook":
                    kind = DialogueBoxKind.BlackBook;
                    return true;
            }
        }

        return false;
    }

    private static bool TryResolveTransitionFromMetadata(
        string[] metadata,
        out DialogueBoxTransitionKind transition)
    {
        transition = default(DialogueBoxTransitionKind);

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = metadata[i];

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            tag = tag.Trim().ToLowerInvariant();

            switch (tag)
            {
                case "boxtransition=keep":
                case "boxtransition:keep":
                case "box_transition=keep":
                case "box_transition:keep":
                case "boxkeep":
                case "box_keep":
                    transition = DialogueBoxTransitionKind.Keep;
                    return true;

                case "boxtransition=cut":
                case "boxtransition:cut":
                case "box_transition=cut":
                case "box_transition:cut":
                case "boxcut":
                case "box_cut":
                    transition = DialogueBoxTransitionKind.Cut;
                    return true;

                case "boxtransition=fade":
                case "boxtransition:fade":
                case "box_transition=fade":
                case "box_transition:fade":
                case "boxfade":
                case "box_fade":
                    transition = DialogueBoxTransitionKind.FadeOutIn;
                    return true;

                case "boxtransition=fadein":
                case "boxtransition:fadein":
                case "box_transition=fadein":
                case "box_transition:fadein":
                case "boxfadein":
                case "box_fadein":
                case "box_fade_in":
                    transition = DialogueBoxTransitionKind.FadeIn;
                    return true;

                case "boxtransition=hide":
                case "boxtransition:hide":
                case "box_transition=hide":
                case "box_transition:hide":
                case "boxhide":
                case "box_hide":
                    transition = DialogueBoxTransitionKind.Hide;
                    return true;
            }
        }

        return false;
    }

    private static string FormatPlan(
        DialogueBoxTransitionPlan plan,
        VNDialogueLine line,
        DialogueBoxPresentationOptions options)
    {
        if (plan == null)
            return "plan=null";

        string lineId = line != null ? line.TextId : "null";

        bool isSeekTarget = options != null && options.IsSeekTargetLine;
        bool immediateOption = options != null && options.UseImmediateTransition;

        return
            $"line={lineId}, " +
            $"nextKind={plan.NextKind}, " +
            $"transition={plan.TransitionKind}, " +
            $"previous={GetBoxName(plan.PreviousBox)}, " +
            $"next={GetBoxName(plan.NextBox)}, " +
            $"useImmediate={plan.UseImmediate}, " +
            $"isSeekTarget={isSeekTarget}, " +
            $"immediateOption={immediateOption}";
    }

    private static string GetBoxName(IDialogueTextTarget box)
    {
        if (box == null)
            return "null";

        MonoBehaviour behaviour = box as MonoBehaviour;
        if (behaviour != null)
            return behaviour.name;

        return box.GetType().Name;
    }
}