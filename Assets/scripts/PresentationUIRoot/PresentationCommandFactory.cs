public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            FadeToPresentationCommandSpec s => new FadeToPresentationCommand(s),
            MoveByPresentationCommandSpec s => new MoveByPresentationCommand(s),
            ScaleToPresentationCommandSpec s => new ScaleToPresentationCommand(s),

            SpawnBackgroundCommandSpec s => new SpawnBackgroundCommand(s),
            SetBackgroundSpriteCommandSpec s => new SetBackgroundSpriteCommand(s),
            DestroyBackgroundCommandSpec s => new DestroyBackgroundCommand(s),

            _ => null
        };

        return command != null;
    }
}