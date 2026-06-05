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
    private VNLinePresentationFlow _vnLinePresentationFlow;

    private EllipsisBreathTypewriter _typewriter;
    private DialogueBoxPresentationController _boxPresentation;
    private VNLinePresentationState _vnLinePresentationState;
    private PresentationSessionContext _presentationSessionContext;
    private VNTraceStream _trace;
    private IYarnLaneDebugSink _debugSink;

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
        VNLinePresentationFlow vnLinePresentationFlow,
        EllipsisBreathTypewriter typewriter,
        VNLinePresentationState vnLinePresentationState,
        PresentationSessionContext presentationSessionContext,
        VNTraceStream trace = null,
        IYarnLaneDebugSink debugSink = null)
    {
        dialogueRunner.onNodeStart?.AddListener(nodeName =>_currentNodeName = nodeName );
        RegisterPresenter(dialogueRunner);
        
        _vnLinePresentationFlow = vnLinePresentationFlow;
        
        _typewriter = typewriter;
        _typewriter.ActionMarkupHandlers = ActionMarkupHandlers;

        _vnLinePresentationState = vnLinePresentationState;
        _presentationSessionContext = presentationSessionContext;
        
        _trace = trace;
        _debugSink = debugSink;
    }
    
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        _debugSink?.SetMain(_currentNodeName, line.TextWithoutCharacterName.Text);
        
        var ctx = new VNLinePresentationContext
        {
            Line = line,
            Token = token,
            NodeName = _currentNodeName,
        };

        await _vnLinePresentationFlow.RunAsync(
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
    
    private bool ShouldFastForwardLine() => _vnLinePresentationState.IsSeekingActive || _presentationSessionContext.IsSpeedUpMode;
    
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
        _debugSink?.ClearMain();
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