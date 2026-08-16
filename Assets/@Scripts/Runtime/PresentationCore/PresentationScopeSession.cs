public interface ICommandRunScopeProvider
{
    CommandRunScope CurrentScope { get; }
}

// Yarn 메인 흐름이 연출 세션으로부터 빌려 쓰는 것은
// "CommandRunScope를 만들고(Start), 멈추고 정리하는(End)" 생애주기뿐이다.
public sealed class PresentationScopeSession : ICommandRunScopeProvider
{
    private readonly PresentationSessionContext _context;
    private readonly ISeekStateQuery _linePresentationAdvanceState;
    private readonly PresentationStage _stage;

    private readonly CommandExecutor _executor;

    private CommandRunScope _sessionScope;
    public CommandRunScope CurrentScope => _sessionScope;

    public bool IsRunning => _sessionScope != null;

    public PresentationScopeSession(
        CommandExecutor executor,
        PresentationSessionContext presentationSessionContext,
        ISeekStateQuery linePresentationAdvanceState,
        PresentationStage presentationStage)
    {
        _executor = executor;
        _context = presentationSessionContext;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _stage = presentationStage;
    }

    public void Start()
    {
        _sessionScope = new CommandRunScope(_context, _linePresentationAdvanceState, _stage);

        _context.ResetSessionFlagsForStart();
    }

    public void End()
    {
        _executor.Stop(CleanupPolicy.Cancel);

        _sessionScope = null;
    }

    public void ClearStage() => _stage?.Clear();
}
