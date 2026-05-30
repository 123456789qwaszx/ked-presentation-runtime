using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public interface IVNLineAborter
{
    void AbortCurrentVnLine();
}

public sealed class CustomLinePresenter : DialoguePresenterBase, IVNLineAborter
{
    private VNLinePresentationStateMachine _vnLinePresentationStateMachine;

    private EllipsisBreathTypewriter _typewriter;
    private DialogueBoxPresentationController _boxPresentation;
    private VNLinePresentationState _lineAdvanceState;
    private PresentationSessionContext _presentationSessionContext;
    private VNTraceStream _trace;

    private string _currentNodeName;
    
    private int _presenterGeneration;
    private CancellationTokenSource _presenterLifetimeCts = new();
    private CancellationTokenSource _lineVisualCts;
    

    [SerializeField] private List<ActionMarkupHandler> eventHandlers = new();
    private List<IActionMarkupHandler> ActionMarkupHandlers
    {
        get
        {
            var list = new List<IActionMarkupHandler> { new PauseEventProcessor() };
            list.AddRange(eventHandlers);
            return list;
        }
    }
    
    public void Initialize(
        DialogueRunner dialogueRunner,
        VNLinePresentationStateMachine vnLinePresentationStateMachine,
        EllipsisBreathTypewriter typewriter,
        VNLinePresentationState linePresentationAdvanceState,
        PresentationSessionContext presentationSessionContext,
        VNTraceStream trace = null)
    {
        dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        dialogueRunner.onNodeStart?.AddListener(OnNodeStart);
        RegisterPresenter(dialogueRunner);
        
        _vnLinePresentationStateMachine = vnLinePresentationStateMachine;
        
        _typewriter = typewriter;
        _typewriter.ActionMarkupHandlers = ActionMarkupHandlers;

        _lineAdvanceState = linePresentationAdvanceState;
        _presentationSessionContext = presentationSessionContext;
        
        _trace = trace;
        
    }
    
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        var ctx = new VNLinePresentationContext
        {
            Line = line,
            Token = token,
            NodeName = _currentNodeName,
        };

        await _vnLinePresentationStateMachine.RunAsync(
            ctx,
            beginRun: BeginLinePresentationRun,
            waitForAdvance: WaitForLineAdvanceAsync,
            shouldFastForward: ShouldFastForwardLine);
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


    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource cts = null;
        try
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask.WaitUntilCanceled(cts.Token).SuppressCancellationThrow();
        }
        finally
        {
            cts?.Dispose();
        }
    }
    
    private bool ShouldFastForwardLine() => _lineAdvanceState.IsSeekingActive || _presentationSessionContext.IsSpeedUpMode;
    
    public void AbortCurrentVnLine()
    {
        _presenterGeneration++;
        CancelLineVisualToken();
        CloseAll();
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
    
    private void CloseAll()
    {
        _boxPresentation?.CloseAll();
        _typewriter?.SetTextView(null);
    }
    
    private void CancelPresenterLifetimeWaiters()
    {
        _presenterLifetimeCts?.Cancel();
        _presenterLifetimeCts?.Dispose();
        _presenterLifetimeCts = new CancellationTokenSource();
    }
    
    private void CancelLineVisualToken()
    {
        if (_lineVisualCts == null) return;
        _lineVisualCts.Cancel();
        _lineVisualCts.Dispose();
        _lineVisualCts = null;
    }

    private void OnNodeStart(string nodeName) => _currentNodeName = nodeName ?? string.Empty;

    private void RegisterPresenter(DialogueRunner dialogueRunner)
    {
        var presenters = new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);
        presenters.Remove(this);

        int idx = presenters.FindIndex(x => x is LinePresenter);
        if (idx < 0) idx = presenters.Count;

        presenters.Insert(idx, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    private void OnDestroy()
    {
        CancelLineVisualToken();
        _presenterLifetimeCts?.Cancel();
        _presenterLifetimeCts?.Dispose();
        _presenterLifetimeCts = null;
    }
}