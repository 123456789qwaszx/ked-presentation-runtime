using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private PresentationSessionContext _context;
    private LinePresentationAdvanceState _linePresentationAdvanceState;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;
    private YarnLineLifecycleBridge _lineLifecycleBridge;
    private VNTraceStream _trace;

    private CancellationTokenSource _presenterLifetimeCts = new();

    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;

    public void Initialize(
        PresentationSessionContext context,
        LinePresentationAdvanceState linePresentationAdvanceState,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver,
        YarnLineLifecycleBridge lineLifecycleBridge,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        VNTraceStream trace = null)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _lineLifecycleBridge = lineLifecycleBridge;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _trace = trace;

        Trace("Initialized");
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        Trace("OnDialogueStarted");
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        Trace("OnDialogueComplete");
        CancelPresenterLifetimeWaiters();

        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        Trace("RunLineStart");

        _yarnBridgePlaybackDriver.PlayCollected();

        Trace("WaitForAdvanceStart");

        NotifyReadyForAdvance();

        await WaitForLineAdvanceAsync(token);

        NotifyNotReady("WaitForAdvanceComplete");

        Trace("WaitForAdvanceComplete");
    }
    
    private void NotifyReadyForAdvance()
    {
        if (_dialogueAdvanceDispatcher == null)
            return;

        _dialogueAdvanceDispatcher.NotifySubPresentationReadyForAdvance();
    }

    private void NotifyNotReady(string reason)
    {
        if (_dialogueAdvanceDispatcher == null)
            return;

        _dialogueAdvanceDispatcher.NotifySubPresentationNotReadyForAdvance(reason);
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

    private bool ShouldSuppressPlayback(out string reason)
    {
        if (_context != null && _context.IsSpeedUpMode)
        {
            reason = "SpeedUpMode";
            return true;
        }

        reason = "None";
        return false;
    }

    private bool IsMainMetaSeekTarget(YarnLineMeta mainMeta)
    {
        if (_linePresentationAdvanceState == null)
            return false;

        if (!HasValidMainMeta(mainMeta))
            return false;

        return _linePresentationAdvanceState.IsSeekTarget(mainMeta);
    }

    private static bool HasValidMainMeta(YarnLineMeta meta)
    {
        return !string.IsNullOrWhiteSpace(meta.nodeName)
            && !string.IsNullOrWhiteSpace(meta.lineId);
    }

    private YarnLineMeta GetCurrentMainMeta()
    {
        if (_lineLifecycleBridge == null)
            return default;

        return _lineLifecycleBridge.CurrentMeta;
    }

    private void CancelPresenterLifetimeWaiters()
    {
        Trace("CancelPresenterLifetimeWaiters");

        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
        }

        _presenterLifetimeCts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        Trace("OnDestroy");

        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
            _presenterLifetimeCts = null;
        }
    }

    private void Trace(
        string evt)
    {
        if (_trace == null)
            return;

        string state = _linePresentationAdvanceState == null
            ? "lineState=null"
            : _linePresentationAdvanceState.Snapshot();

        _trace.Trace(
            "SubPresentationPresenter",
            evt,
            state);
    }

    private string GetCurrentMainLineMetaNote()
    {
        if (_lineLifecycleBridge == null)
            return "currentMain=null";

        return GetMainLineMetaNote("currentMain", _lineLifecycleBridge.CurrentMeta);
    }

    private static string GetMainLineMetaNote(string label, YarnLineMeta meta)
    {
        string charName = meta.charName ?? string.Empty;

        return $"{label}Char={charName}'";
    }

    private static string CombineNotes(string a, string b, string c, string d)
    {
        string result = string.Empty;

        AppendNote(ref result, a);
        AppendNote(ref result, b);
        AppendNote(ref result, c);
        AppendNote(ref result, d);

        return result;
    }

    private static void AppendNote(ref string result, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return;

        if (string.IsNullOrWhiteSpace(result))
            result = note;
        else
            result += ", " + note;
    }
}