public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Flash / Hit feedback
            ScreenFlashCommandSpec s => new ScreenFlashCommand(s),

            // Mood / Vignette
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s),
            
            _ => null
        };

        return command != null;
    }
}