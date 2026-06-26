using UnityEngine;

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

            // Setup
            SetupOverlayRigCommandSpec s => new SetupOverlayRigCommand(
                _slotResolver,
                _builder,
                s),

            _ => null
        };

        return command != null;
    }
}
