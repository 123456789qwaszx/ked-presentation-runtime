using System.Threading;
using Yarn.Unity;

public sealed class SubPresentationPresenter : DialoguePresenterBase
{
    private PresentationSessionContext _context;
    private LinePresentationAdvanceState _linePresentationAdvanceState;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;
    private YarnLineLifecycleBridge _lineLifecycleBridge;
    private VNTraceStream _trace;

    private CancellationTokenSource _presenterLifetimeCts = new();

    public void Initialize(
        PresentationSessionContext context,
        LinePresentationAdvanceState linePresentationAdvanceState,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver,
        YarnLineLifecycleBridge lineLifecycleBridge,
        VNTraceStream trace = null)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _lineLifecycleBridge = lineLifecycleBridge;
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
        YarnLineMeta mainMetaAtStart = GetCurrentMainMeta();

        Trace("RunLineStart", line, mainMetaAtStart);
        
        Trace("PlayCollectedStart", line, mainMetaAtStart);
        _yarnBridgePlaybackDriver.PlayCollected();
        Trace("PlayCollectedComplete", line, mainMetaAtStart);
        

        if (ShouldPassThroughLine(mainMetaAtStart, out string reason))
        {
            Trace("PassThrough", line, mainMetaAtStart, $"reason={reason}");
            return;
        }

        Trace("WaitForAdvanceStart", line, mainMetaAtStart);
        await WaitForLineAdvanceAsync(token);
        Trace("WaitForAdvanceComplete", line, mainMetaAtStart);
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

    private bool ShouldPassThroughLine(YarnLineMeta mainMeta, out string reason)
    {
        if (_context != null && _context.IsSpeedUpMode)
        {
            reason = "SpeedUpMode";
            return true;
        }

        if (_linePresentationAdvanceState != null && _linePresentationAdvanceState.IsSeeking)
        {
            if (IsMainMetaSeekTarget(mainMeta))
            {
                reason = "SeekTarget";
                return false;
            }

            reason = "SeekingNonTarget";
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
        string evt,
        LocalizedLine line = null,
        YarnLineMeta? mainMetaAtStart = null,
        string note = null)
    {
        if (_trace == null)
            return;

        string state = _linePresentationAdvanceState == null
            ? "lineState=null"
            : _linePresentationAdvanceState.Snapshot();

        string mainAtStart = mainMetaAtStart.HasValue
            ? GetMainLineMetaNote("mainAtStart", mainMetaAtStart.Value)
            : string.Empty;

        string currentMain = GetCurrentMainLineMetaNote();
        string subLine = GetSubLineNote(line);

        string finalNote = CombineNotes(mainAtStart, currentMain, subLine, note);

        _trace.Trace(
            "SubPresentationPresenter",
            evt,
            state,
            finalNote,
            this);
    }

    private string GetCurrentMainLineMetaNote()
    {
        if (_lineLifecycleBridge == null)
            return "currentMain=null";

        return GetMainLineMetaNote("currentMain", _lineLifecycleBridge.CurrentMeta);
    }

    private static string GetMainLineMetaNote(string label, YarnLineMeta meta)
    {
        string formattedMeta = YarnLineLifecycleBridge.FormatMeta(meta);
        string charName = meta.charName ?? string.Empty;
        string textPreview = YarnLineLifecycleBridge.Preview(meta.rawText);

        return $"{label}={formattedMeta}, {label}Char={charName}, {label}Text='{textPreview}'";
    }

    private static string GetSubLineNote(LocalizedLine line)
    {
        if (line == null)
            return string.Empty;

        string rawText = line.RawText ?? string.Empty;
        string preview = YarnLineLifecycleBridge.Preview(rawText);

        return $"subRaw='{preview}'";
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