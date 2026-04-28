using UnityEngine;

public interface IBGViewPrefabProvider
{
    bool TryGetBackgroundViewPrefab(string key, out GameObject prefab);
}

public interface IDialogueBoxViewPrefabProvider
{
    bool TryGetDialogueBoxViewPrefab(string key, out GameObject prefab);
}

public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    PresentationViewAccess _presentationViewAccess = new ();
    
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

            SetupPresentationViewCommandSpec s => new SetupPresentationViewCommand(_presentationViewAccess, s),
            
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