public sealed class OverlayRigCommandFactory : INodeCommandFactory
{
    private readonly StageOverlayRigSlotResolver _slotResolver;
    private readonly OverlayRigBuilder _builder;

    public OverlayRigCommandFactory(
        StageOverlayRigSlotResolver slotResolver,
        OverlayRigBuilder builder)
    {
        _slotResolver = slotResolver;
        _builder = builder;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            SetupOverlayRigCommandSpec s => new SetupOverlayRigCommand(
                _slotResolver,
                _builder,
                s),

            OverlayMoveCommandSpec s => new OverlayMoveCommand(s),
            OverlaySizeCommandSpec s => new OverlaySizeCommand(s),
            OverlayScaleCommandSpec s => new OverlayScaleCommand(s),
            OverlayShowCommandSpec s => new OverlayShowCommand(s),
            OverlayHideCommandSpec s => new OverlayHideCommand(s),
            OverlaySpriteCommandSpec s => new OverlaySpriteCommand(s),
            OverlayTextCommandSpec s => new OverlayTextCommand(s),

            _ => null
        };

        return command != null;
    }
}