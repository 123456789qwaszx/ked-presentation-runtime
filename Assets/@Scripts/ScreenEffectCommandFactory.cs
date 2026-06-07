public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Flash / Hit feedback
            ScreenFlashCommandSpec s => new ScreenFlashCommand(s),

            _ => null
        };

        return command != null;
    }
}