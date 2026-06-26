// Yarn 메인 흐름이 PresentationSession으로부터 실제로 빌려 쓰는 것은
// "세 레인의 CommandRunScope를 만들고(Start), 멈추고 정리하는(End)" 생애주기뿐이다.
// Node/Step Gate 진행(Tick/PlayStep/StepGatePlanBuilder/StepGateAdvancer)은
// SequenceSpecSO를 직접 재생하는 PresentationSession(OverlaySequenceRunner 등)의 책임이며,
// Yarn 메인 흐름은 여기에 의존하지 않는다.
public sealed class PresentationLaneScopeSession
{
    private readonly PresentationSessionContext _context;
    private readonly VNLinePresentationState _linePresentationAdvanceState;
    private readonly PresentationStage _stage;

    private readonly CommandExecutor _executor;
    private readonly CommandExecutor _subExecutor;
    private readonly CommandExecutor _subOneShotExecutor;

    private CommandRunScope _sessionScope;
    public CommandRunScope CurrentScope => _sessionScope;

    private CommandRunScope _subScope;
    public CommandRunScope SubScope => _subScope;

    private CommandRunScope _subOneShotScope;
    public CommandRunScope SubOneShotScope => _subOneShotScope;

    public bool IsRunning => _sessionScope != null;

    public PresentationLaneScopeSession(
        CommandExecutor executor,
        CommandExecutor subExecutor,
        CommandExecutor subOneShotExecutor,
        PresentationSessionContext presentationSessionContext,
        VNLinePresentationState linePresentationAdvanceState,
        PresentationStage presentationStage)
    {
        _executor = executor;
        _subExecutor = subExecutor;
        _subOneShotExecutor = subOneShotExecutor;
        _context = presentationSessionContext;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _stage = presentationStage;
    }

    public void Start()
    {
        if (IsRunning)
            EndImmediately();

        _sessionScope = new CommandRunScope(_context, _linePresentationAdvanceState, _stage);
        _subScope = new CommandRunScope(_context, _linePresentationAdvanceState, _stage, reportsNodeBusy: false);
        _subOneShotScope = new CommandRunScope(_context, _linePresentationAdvanceState, _stage, reportsNodeBusy: false);

        _context.ResetSessionFlagsForStart();
    }

    public void EndImmediately() => End();

    private void End()
    {
        _executor.Stop(CleanupPolicy.Cancel);
        _subExecutor?.Stop(CleanupPolicy.Cancel);
        _subOneShotExecutor?.Stop(CleanupPolicy.Cancel);

        _sessionScope = null;
        _subScope = null;
        _subOneShotScope = null;
    }

    public void ClearStage() => _stage?.Clear();
}