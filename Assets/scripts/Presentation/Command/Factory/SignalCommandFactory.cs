public sealed class SignalCommandFactory : INodeCommandFactory
{
    private readonly ITimeSource _time;
    private readonly ISignalBus _signal;
    private readonly ISignalLatch _latch;
    
    public SignalCommandFactory(
        ITimeSource time,
        ISignalBus signal,
        ISignalLatch latch
        )
    {
        _time = time;
        _signal = signal;
        _latch = latch;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            WaitCommandSpec s => Create(s),
            HoldSignalCommandSpec s => Create(s),
            RaiseSignalCommandSpec s => Create(s),
            
            _ => null
        };

        return command != null;
    }

    private RaiseSignalCommand Create(RaiseSignalCommandSpec s)
        => new(_signal,
            key: s.signalKey,
            raiseOnSkip: s.raiseOnSkip
        );

    private HoldSignalCommand Create(HoldSignalCommandSpec s)
        => new(_latch, _time,
            key: s.signalKey,
            consume: s.consume,
            timeoutSeconds: s.timeoutSeconds,
            respectTimeScale: s.respectTimeScale
        );

    private CpsWaitCommand Create(WaitCommandSpec s)
        => new(seconds: s.seconds
        );
}