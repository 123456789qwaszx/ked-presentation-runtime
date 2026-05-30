/// <summary>
/// 라인 처리 중 발생하는 도메인 커밋을 전담한다.
/// 시각적 작업(Box, Typewriter)은 포함하지 않는다.
/// </summary>
public sealed class VNLinePresentationCommitter
{
    private readonly YarnLineLifecycleBridge _bridge;
    private readonly LinePresentationAdvanceState _advanceState;
    private readonly YarnBridgePlaybackDriver _playbackDriver;

    private readonly BacklogRecorder _backlogRecorder;
    private readonly RollbackController _rollbackController;
    private readonly VNRuntimeStateProvider _runtimeStateProvider;
    private readonly VNTraceStream _trace;

    public VNLinePresentationCommitter(
        YarnLineLifecycleBridge bridge,
        LinePresentationAdvanceState advanceState,
        YarnBridgePlaybackDriver playbackDriver,
        BacklogRecorder backlogRecorder,
        RollbackController rollbackController,
        VNRuntimeStateProvider runtimeStateProvider,
        VNTraceStream trace = null)
    {
        _bridge = bridge;
        _advanceState = advanceState;
        _playbackDriver = playbackDriver;
        _backlogRecorder = backlogRecorder;
        _rollbackController = rollbackController;
        _runtimeStateProvider = runtimeStateProvider;
        _trace = trace;
    }

    /// <summary>
    /// Phase: LineReceived → LineEnteredCommitted
    ///
    /// - YarnLineMeta 생성 및 브리지 갱신 (→ LineEntered 이벤트 발행)
    ///   - BacklogRecorder, RollbackController, VNRuntimeStateProvider는
    ///     이 이벤트를 구독해 각자 처리한다.
    /// - PlaybackDriver PlayCollected
    /// - AdvanceState MarkLineEntered
    /// </summary>
    public YarnLineMeta CommitLineEntered(Yarn.Unity.LocalizedLine line, string nodeName)
    {
        // 브리지 갱신: LineEntered 이벤트를 통해
        // BacklogRecorder, RollbackController, VNRuntimeStateProvider가 반응한다.
        _bridge.RefreshCurrentLineMeta(line, nodeName);
        YarnLineMeta meta = _bridge.CurrentMeta;
        
        _runtimeStateProvider?.HandleLineEntered(meta);

        if (!_advanceState.IsSeekActive)
        {
            _backlogRecorder?.Record(meta);

            if (_advanceState.CanRecordRollbackPoint)
                _rollbackController?.AddRollbackPoint(meta);
        }

        // 이 순서가 중요하다:
        // RollbackPoint는 LineEntered 구독자에서 기록되고,
        // MarkLineEntered는 그 이후 IsFullyShown = false로 전환한다.
        _playbackDriver.PlayCollected();

        _advanceState.MarkLineEntered();

        Trace("CommitLineEntered", $"meta={FormatMeta(meta)}");

        return meta;

    }

    /// <summary>
    /// Phase: DisplayCommitted
    ///
    /// Yarn 진행상 이 라인의 표시 처리가 완료됐음을 커밋한다.
    /// (정상 표시 완료 / seek pass-through 모두 이 메서드로 처리)
    /// </summary>
    public void CommitLineProcessingCompleted()
    {
        _advanceState.MarkLineDisplayCompleted();
        Trace("CommitLineProcessingCompleted");
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(
            nameof(VNLinePresentationCommitter),
            evt,
            _advanceState?.Snapshot(),
            note);
    }

    private static string FormatMeta(YarnLineMeta meta)
    {
        return $"{meta.nodeName}/{meta.lineId}";
    }
}