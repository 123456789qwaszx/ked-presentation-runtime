using UnityEngine;

public interface IBGViewPrefabProvider
{
    bool TryGetBackgroundViewPrefab(string key, out GameObject prefab);
}

public interface IDialogueBoxViewResolver
{
    IDialogueTextTarget ResolveTarget(DialogueBoxKind kind);
    void ShowOnly(IDialogueTextTarget target);
    void HideAll();
}

public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    private readonly PresentationViewAccess _presentationViewAccess;
    
    private readonly IBGViewPrefabProvider _bgViewPrefabProvider;
    private readonly IBGRuntimeRegistry _bgRuntimeRegistry;

    public PresentationViewCommandFactory(
        PresentationViewAccess presentationViewAccess,
        IBGViewPrefabProvider bgViewPrefabProvider,
        IBGRuntimeRegistry bgRuntimeRegistry)
    {
        _presentationViewAccess = presentationViewAccess;
        _bgViewPrefabProvider = bgViewPrefabProvider;
        _bgRuntimeRegistry = bgRuntimeRegistry;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Presentation View setup
            SetupPresentationViewCommandSpec s => new SetupPresentationViewCommand(_presentationViewAccess, s),

            // Presentation View transform
            FadeToPresentationCommandSpec s => new FadeToPresentationCommand(s),
            MoveByPresentationCommandSpec s => new MoveByPresentationCommand(s),
            ScaleToPresentationCommandSpec s => new ScaleToPresentationCommand(s),

            // Background
            SpawnBackgroundCommandSpec s => new SpawnBackgroundCommand(_bgViewPrefabProvider, s, _bgRuntimeRegistry),
            SetBackgroundSpriteCommandSpec s => new SetBackgroundSpriteCommand(s),
            FadeBackgroundCommandSpec s => new FadeBackgroundCommand(s),
            DestroyBackgroundCommandSpec s => new DestroyBackgroundCommand(s),

            // Presentation Shot / Response Rig
            ShotResetCommandSpec s => new ShotResetCommand(s),
            ShotZoomCommandSpec s => new ShotZoomCommand(s),
            ShotPanToCommandSpec s => new ShotPanToCommand(s),
            ShotTrackCommandSpec s => new ShotTrackCommand(s),

            _ => null
        };

        return command != null;
    }
}