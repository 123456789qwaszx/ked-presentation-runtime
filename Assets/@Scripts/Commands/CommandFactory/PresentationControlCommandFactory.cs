public sealed class PresentationControlCommandFactory : INodeCommandFactory
{
    private readonly UIPatchService _uiPatchService;
    
    private readonly ITimeSource _time;
    private readonly ISignalBus _signal;
    private readonly ISignalLatch _latch;
    
    public PresentationControlCommandFactory(
        UIPatchService uiPatchService,
        ITimeSource time, 
        ISignalBus signal, 
        ISignalLatch latch)
    {
        _uiPatchService = uiPatchService;
        _time = time;
        _signal = signal;
        _latch = latch;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            UIPatchCommandSpec s => new UIPatchCommand(_uiPatchService, s),
            
            // ActorAlias
            SetPresentationActorAliasCommandSpec s => new SetPresentationActorAliasCommand(s),
            
            WaitCommandSpec s =>  new WaitCommand(s),
            HoldSignalCommandSpec s => Create(s),
            RaiseSignalCommandSpec s => Create(s),

            _ => null
        };

        return command != null;
    }
    
    private HoldSignalCommand Create(HoldSignalCommandSpec s)
        => new(
            _latch,
            _time,
            key: s.signalKey,
            consume: s.consume,
            timeoutSeconds: s.timeoutSeconds,
            respectTimeScale: s.respectTimeScale);

    private RaiseSignalCommand Create(RaiseSignalCommandSpec s)
        => new(
            _signal,
            key: s.signalKey,
            raiseOnSkip: s.raiseOnSkip);
}