using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

public interface ILinePresentationAborter
{
    void AbortCurrentLinePresentationForRollback();
}

public sealed class CustomLinePresenter : DialoguePresenterBase, ILinePresentationAborter
{
    private DialogueBoxPresentationController _boxPresentation;
    private EllipsisBreathTypewriter _typewriter;
    private PresentationSessionContext _context;
    private LinePresentationAdvanceState _lineAdvanceState;
    private VNTraceStream _trace;

    private int _presenterGeneration;

    private CancellationTokenSource _presenterLifetimeCts = new ();

    private CancellationTokenSource _lineVisualCts;

    [SerializeField]
    private List<ActionMarkupHandler> eventHandlers = new ();

    private List<IActionMarkupHandler> ActionMarkupHandlers
    {
        get
        {
            PauseEventProcessor pauser = new PauseEventProcessor();

            List<IActionMarkupHandler> actionMarkupHandlers = new List<IActionMarkupHandler>
            {
                pauser,
            };

            actionMarkupHandlers.AddRange(eventHandlers);
            return actionMarkupHandlers;
        }
    }

    public event Action<LocalizedLine> LineEntered;

    public void Initialize(
        DialogueRunner dialogueRunner,
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        IDialogueBoxViewResolver dialogueBoxResolver,
        DialogueTextRouter dialogueTextRouter,
        EllipsisBreathTypewriter typewriter,
        PresentationSessionContext context,
        LinePresentationAdvanceState lineAdvanceState,
        VNTraceStream trace = null)
    {
        _typewriter = typewriter;
        _typewriter.ActionMarkupHandlers = ActionMarkupHandlers;

        _context = context;
        _lineAdvanceState = lineAdvanceState;
        _trace = trace;

        DialogueBoxTransitionPolicy transitionPolicy = new ();
        DialogueBoxTextPrimer textPrimer = new ();
        DialogueBoxTransitionRunner transitionRunner = new (dialogueBoxResolver, trace);

        _boxPresentation = new DialogueBoxPresentationController(
            lineRoutingPolicy,
            dialogueBoxResolver,
            transitionPolicy,
            dialogueTextRouter,
            textPrimer,
            transitionRunner,
            trace);
        
        RegisterBeforeDefaultLinePresenter(dialogueRunner);
    }

    public void AbortCurrentLinePresentationForRollback()
    {
        _presenterGeneration++;
        CancelLineVisualToken();
        CloseAll();

        Trace("AbortCurrentLinePresentationForRollback");
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        CloseAll();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CancelLineVisualToken();
        CancelPresenterLifetimeWaiters();
        CloseAll();

        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        Trace("RunLineStart", line);
        LineEntered?.Invoke(line);

        _lineAdvanceState.MarkLineEntered();

        LinePresentationRun run = BeginLinePresentationRun();

        bool isPendingSeekTargetLine = _lineAdvanceState.IsPendingSeekTargetLine(line.TextID);
        bool shouldPassThroughSeekLine = _lineAdvanceState.IsSeeking && !isPendingSeekTargetLine;
        Trace("SeekCheck", line, $"isPendingTarget={isPendingSeekTargetLine}");

        if (shouldPassThroughSeekLine)
        {
            Trace("SilentSeekPassThrough", line);
            _boxPresentation.HideAllForSeek();
            _lineAdvanceState.MarkLineDisplayCompleted();

            await WaitForLineAdvanceAsync(token);
            return;
        }

        if (isPendingSeekTargetLine)
        {
            Trace("SeekTargetLineAccepted", line);
            _lineAdvanceState.ConsumeSeekTargetLine(line.TextID);
        }

        DialogueBoxPresentationResult boxResult = await _boxPresentation.ShowLineAsync(
            VNDialogueLineFactory.FromLocalizedLine(line),
            new DialogueBoxPresentationOptions
            {
                IsSeekTargetLine = isPendingSeekTargetLine,
                UseImmediateTransition = isPendingSeekTargetLine || ShouldFastForwardLine(),
                Run = run,
            });
        
        if (!run.IsValid)
        {
            Trace("RunBecameStaleAfterBoxPresentation", line);
            _boxPresentation.CleanupStale(boxResult);

            await WaitForLineAdvanceAsync(token);
            return;
        }

        TMP_Text lineText = boxResult?.LineText;
        _typewriter.SetTextView(lineText);

        MarkupParseResult text = line.TextWithoutCharacterName;
        _typewriter.PrepareForContent(text);

        await _typewriter.RunTypewriter(text, token.HurryUpToken).SuppressCancellationThrow();

        if (!run.IsValid)
            Trace("SkipDisplayCompletedBecauseRunInvalid", line);
        else
        {
            _lineAdvanceState.MarkLineDisplayCompleted();
            _typewriter.ContentWillDismiss();
        }

        await WaitForLineAdvanceAsync(token);
    }

    private LinePresentationRun BeginLinePresentationRun()
    {
        CancelLineVisualToken();

        _lineVisualCts = CancellationTokenSource.CreateLinkedTokenSource(_presenterLifetimeCts.Token);

        return new LinePresentationRun(
            _presenterGeneration,
            () => _presenterGeneration,
            _lineVisualCts.Token);
    }

    private void CancelLineVisualToken()
    {
        if (_lineVisualCts == null)
            return;

        _lineVisualCts.Cancel();
        _lineVisualCts.Dispose();
        _lineVisualCts = null;
    }

    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource lineWaitCts = null;

        try
        {
            lineWaitCts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask
                .WaitUntilCanceled(lineWaitCts.Token)
                .SuppressCancellationThrow();
        }
        finally
        {
            if (lineWaitCts != null)
                lineWaitCts.Dispose();
        }
    }

    private void CancelPresenterLifetimeWaiters()
    {
        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
        }

        _presenterLifetimeCts = new CancellationTokenSource();
    }

    private bool ShouldFastForwardLine()
    {
        return _lineAdvanceState.IsSeeking || _context.IsSpeedUpMode;
    }

    private void CloseAll()
    {
        _boxPresentation.CloseAll();
        _typewriter.SetTextView(null);
    }

    private void RegisterBeforeDefaultLinePresenter(DialogueRunner dialogueRunner)
    {
        List<DialoguePresenterBase> presenters = new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);

        presenters.Remove(this);

        int insertIndex = presenters.FindIndex(x => x is LinePresenter);
        if (insertIndex < 0)
            insertIndex = presenters.Count;

        presenters.Insert(insertIndex, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    private void OnDestroy()
    {
        CancelLineVisualToken();

        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
            _presenterLifetimeCts = null;
        }
    }

    private void Trace(string evt, LocalizedLine line = null, string note = null)
    {
        if (_trace == null)
            return;

        string lineInfo = line == null
            ? string.Empty
            : $"line={line.TextID}, char={line.CharacterName ?? string.Empty}";

        string state = _lineAdvanceState == null
            ? "lineState=null"
            : _lineAdvanceState.Snapshot();

        string finalNote;

        if (string.IsNullOrWhiteSpace(note))
            finalNote = lineInfo;
        else if (string.IsNullOrWhiteSpace(lineInfo))
            finalNote = note;
        else
            finalNote = $"{lineInfo}, {note}";

        _trace.Trace(
            "CustomLinePresenter",
            evt,
            state,
            finalNote,
            this);
    }
}