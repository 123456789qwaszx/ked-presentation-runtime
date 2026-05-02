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
    private readonly PresentationResponseRig _presentationResponseRig;
    
    private readonly IBGViewPrefabProvider _bgViewPrefabProvider;
    private readonly IBGRuntimeRegistry _bgRuntimeRegistry;

    public PresentationViewCommandFactory(
        PresentationViewAccess presentationViewAccess,
        PresentationResponseRig presentationResponseRig,
        IBGViewPrefabProvider bgViewPrefabProvider,
        IBGRuntimeRegistry bgRuntimeRegistry)
    {
        _presentationViewAccess = presentationViewAccess;
        _presentationResponseRig = presentationResponseRig;
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
            SpawnBackgroundCommandSpec s => new SpawnBackgroundCommand(_bgViewPrefabProvider, s, _bgRuntimeRegistry, _presentationResponseRig),
            SetBackgroundSpriteCommandSpec s => new SetBackgroundSpriteCommand(s),
            FadeBackgroundCommandSpec s => new FadeBackgroundCommand(s),
            DestroyBackgroundCommandSpec s => new DestroyBackgroundCommand(s),

            // Presentation Shot / Response Rig
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotPanToCommandSpec s => new ShotPanToCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

            _ => null
        };

        return command != null;
    }
}