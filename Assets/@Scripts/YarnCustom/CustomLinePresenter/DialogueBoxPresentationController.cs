using Yarn.Unity;

public sealed class DialogueBoxPresentationController
{
    private readonly DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;
    private readonly DialogueBoxTransitionPolicy _boxTransitionPolicy;
    private readonly DialogueTextRouter _dialogueTextRouter;
    private readonly DialogueBoxTextPrimer _textPrimer;
    private readonly DialogueBoxTransitionRunner _transitionRunner;
    private readonly VNTraceStream _trace;

    private readonly DialogueBoxCurrentState _boxState = new ();
    
    private float _fadeUpDuration = 0.25f;
    private float _fadeDownDuration = 0.1f;

    public DialogueBoxPresentationController(
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        IDialogueBoxViewResolver dialogueBoxResolver,
        DialogueBoxTransitionPolicy boxTransitionPolicy,
        DialogueTextRouter dialogueTextRouter,
        DialogueBoxTextPrimer textPrimer,
        DialogueBoxTransitionRunner transitionRunner,
        VNTraceStream trace = null)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _boxTransitionPolicy = boxTransitionPolicy;
        _dialogueTextRouter = dialogueTextRouter;
        _textPrimer = textPrimer;
        _transitionRunner = transitionRunner;
        _trace = trace;
    }

    public async YarnTask<DialogueBoxPresentationResult> ShowLineAsync(VNDialogueLine line, DialogueBoxPresentationOptions options)
    {
        if (line == null)
        {
            Trace("ShowLineSkipped", "reason=LineNull");
            return new DialogueBoxPresentationResult(null, true);
        }

        if (options == null || options.Run == null || !options.Run.IsValid)
        {
            Trace("ShowLineSkipped", "reason=InvalidOptionsOrRun");
            return new DialogueBoxPresentationResult(null, true);
        }

        DialogueBoxTransitionPlan plan = BuildPlan(line, options);

        Trace("ShowLineStart", FormatPlan(plan, line, options));

        _transitionRunner.ResetBoxTransform(plan.NextBox);

        _textPrimer.Prime(plan.NextBox, line);
        Trace("PrimeText", $"line={line.TextId}, box={GetBoxName(plan.NextBox)}");

        _transitionRunner.Prepare(plan);

        if (plan.UseImmediate)
            _transitionRunner.ApplyImmediate(plan);
        else 
            await _transitionRunner.ApplyAsync(plan, _fadeUpDuration, _fadeDownDuration, options.Run);
        
        if (!options.Run.IsValid)
        {
            Trace("ShowLineStale", FormatPlan(plan, line, options));
            return new DialogueBoxPresentationResult(plan, true);
        }

        Commit(plan);

        _dialogueTextRouter.Bind(_boxState);
        Trace("Commit", FormatPlan(plan, line, options));

        return new DialogueBoxPresentationResult(plan, false);
    }

    public void HideAllForSeek()
    {
        Trace("HideAllForSeek");
        _transitionRunner.HideAll();
        _dialogueTextRouter.Clear();
        _boxState.Reset();
    }

    public void CloseAll()
    {
        Trace("CloseAll");
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

        if (_transitionRunner == null)
            return;

        Trace("CleanupStale", $"previous={GetBoxName(previousBox)}, next={GetBoxName(nextBox)}, current={GetBoxName(_boxState.Box)}");

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            _transitionRunner.SetVisibleImmediate(nextBox, false);

        if (previousBox != null &&
            !ReferenceEquals(previousBox, _boxState.Box) &&
            !ReferenceEquals(previousBox, nextBox))
        {
            _transitionRunner.SetVisibleImmediate(previousBox, false);
        }

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

        Trace("BuildPlan", FormatPlan(plan, line, options));

        return plan;
    }

    private void Commit(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

        _boxState.Commit(plan.NextKind, plan.NextBox, plan.TransitionKind);
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state =
            $"boxKind={(_boxState.BoxKind.HasValue ? _boxState.BoxKind.Value.ToString() : "null")}, " +
            $"box={GetBoxName(_boxState.Box)}, " +
            $"visible={_boxState.IsVisible}";

        _trace.Trace(
            "DialogueBoxPresentation",
            evt,
            state,
            note);
    }

    private static string FormatPlan(DialogueBoxTransitionPlan plan, VNDialogueLine line, DialogueBoxPresentationOptions options)
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
            $"immediateOption={immediateOption}, ";
    }

    private static string GetBoxName(IDialogueTextTarget box)
    {
        if (box == null)
            return "null";

        UnityEngine.MonoBehaviour behaviour = box as UnityEngine.MonoBehaviour;
        if (behaviour != null)
            return behaviour.name;

        return box.GetType().Name;
    }
}