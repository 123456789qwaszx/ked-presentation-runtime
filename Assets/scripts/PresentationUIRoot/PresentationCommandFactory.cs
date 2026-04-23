public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    private readonly IBGViewPrefabProvider _bgViewPrefabProvider;

    public PresentationViewCommandFactory(IBGViewPrefabProvider bgViewPrefabProvider)
    {
        _bgViewPrefabProvider = bgViewPrefabProvider;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            FadeToPresentationCommandSpec s => new FadeToPresentationCommand(s),
            MoveByPresentationCommandSpec s => new MoveByPresentationCommand(s),
            ScaleToPresentationCommandSpec s => new ScaleToPresentationCommand(s),

            SpawnBackgroundCommandSpec s => new SpawnBackgroundCommand(_bgViewPrefabProvider, s),
            SetBackgroundSpriteCommandSpec s => new SetBackgroundSpriteCommand(s),
            FadeBackgroundCommandSpec s => new FadeBackgroundCommand(s),
            DestroyBackgroundCommandSpec s => new DestroyBackgroundCommand(s),

            _ => null
        };

        return command != null;
    }
}