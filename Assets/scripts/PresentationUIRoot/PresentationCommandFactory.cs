public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    private readonly IBGViewPrefabProvider _bgViewPrefabProvider;
    private readonly IDialogueBoxViewPrefabProvider _dialogueBoxViewPrefabProvider;

    public PresentationViewCommandFactory(
        IBGViewPrefabProvider bgViewPrefabProvider,
        IDialogueBoxViewPrefabProvider dialogueBoxViewPrefabProvider)
    {
        _bgViewPrefabProvider = bgViewPrefabProvider;
        _dialogueBoxViewPrefabProvider = dialogueBoxViewPrefabProvider;
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

            SpawnDialogueBoxCommandSpec s => new SpawnDialogueBoxCommand(_dialogueBoxViewPrefabProvider, s),
            FadeDialogueBoxCommandSpec s => new FadeDialogueBoxCommand(s),
            SetDialogueBoxTextCommandSpec s => new SetDialogueBoxTextCommand(s),
            DestroyDialogueBoxCommandSpec s => new DestroyDialogueBoxCommand(s),

            _ => null
        };

        return command != null;
    }
}