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
    private readonly VNTraceStream _trace;

    private readonly DialogueBoxCurrentState _boxState = new ();

    private DialogueBoxKind _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
    private DialogueBoxKind _namedLineBoxKind = DefaultNamedLineBoxKind;

    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationPhase CurrentPhase { get; private set; } = DialogueBoxPresentationPhase.None;

    public DialogueBoxPresentationController(DialogueBoxHost host, DialogueBoxMetadataResolver metadataResolver, VNTraceStream trace = null)
    {
        _host = host;
        _metadataResolver = metadataResolver;
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

        if (!options.Run.IsValid) {
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
        IDialogueTextTarget previousBox = result.Plan.PreviousBox;
        IDialogueTextTarget nextBox = result.Plan.NextBox;
        
        Trace("CleanupStale", $"previous={GetBoxName(previousBox)}, next={GetBoxName(nextBox)}, current={GetBoxName(_boxState.Box)}");

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

        Trace("BuildPlan", FormatPlan(plan, line, options));
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

        Trace("PrimeText", $"line={line.TextId}, box={GetBoxName(target)}");
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

        Trace("PrepareTransition", $"transition={plan.TransitionKind}, next={GetBoxName(nextBox)}");
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

        Trace("ApplyImmediate", $"transition={plan.TransitionKind}, next={GetBoxName(plan.NextBox)}");
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
        Trace("Commit", $"kind={plan.NextKind}, box={GetBoxName(plan.NextBox)}, transition={plan.TransitionKind}");
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

    private void SetPhase(DialogueBoxPresentationPhase phase)
    {
        CurrentPhase = phase;
    }
    
    public void SetProtagonistLineBoxKind(DialogueBoxKind kind)
    {
        _protagonistLineBoxKind = kind;
        Trace("SetProtagonistLineBoxKind", $"kind={kind}");
    }

    public void SetNamedLineBoxKind(DialogueBoxKind kind)
    {
        _namedLineBoxKind = kind;
        Trace("SetNamedLineBoxKind", $"kind={kind}");
    }

    public void SetDefaultLineBoxKinds(DialogueBoxKind protagonistKind, DialogueBoxKind namedKind)
    {
        _protagonistLineBoxKind = protagonistKind;
        _namedLineBoxKind = namedKind;

        Trace(
            "SetDefaultLineBoxKinds",
            $"protagonist={protagonistKind}, named={namedKind}");
    }

    public void ResetDefaultLineBoxKinds()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;

        Trace(
            "ResetDefaultLineBoxKinds",
            $"protagonist={_protagonistLineBoxKind}, named={_namedLineBoxKind}");
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

        _trace.Trace(nameof(DialogueBoxPresentationController), evt, state, note);
    }

    private static string FormatPlan(DialogueBoxTransitionPlan plan, VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        if (plan == null) 
            return "plan=null";
        
        string lineId = line != null 
            ? line.TextId 
            : "null";
        
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
    
    private static void SetCanvas(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}