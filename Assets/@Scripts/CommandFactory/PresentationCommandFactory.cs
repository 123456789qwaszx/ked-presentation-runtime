public interface IBGViewPrefabProvider
{
    bool TryGetBackgroundViewPrefab(string key, out RectTransformResponseTarget prefab);
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

    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;

    public PresentationViewCommandFactory(
        PresentationViewAccess presentationViewAccess,
        PresentationResponseRig presentationResponseRig,
        IBGViewPrefabProvider bgViewPrefabProvider,
        IBGRuntimeRegistry bgRuntimeRegistry,
        IDialogueBoxViewResolver dialogueBoxViewResolver)
    {
        _presentationViewAccess = presentationViewAccess;
        _presentationResponseRig = presentationResponseRig;
        _bgViewPrefabProvider = bgViewPrefabProvider;
        _bgRuntimeRegistry = bgRuntimeRegistry;
        _dialogueBoxResolver = dialogueBoxViewResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Presentation View setup
            SetupPresentationViewCommandSpec s => new SetupPresentationViewCommand(_presentationViewAccess, _presentationResponseRig, s),

            SlantedMaskSlideInCommandSpec s => new SlantedMaskSlideInCommand(s),
            SlantedMaskSlideOutCommandSpec s => new SlantedMaskSlideOutCommand(s),
            
            VerticalStripWipeCommandSpec s => new VerticalStripWipeCommand(s),
            SlantedShutterCommandSpec s => new SlantedShutterCommand(s),
            FocusBlurFadeCommandSpec s => new FocusBlurFadeCommand(s),
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),
            LightSweepCommandSpec s => new LightSweepCommand(s),
            
            
            ResetPresentationTargetTransformCommandSpec s => new ResetPresentationTargetTransformCommand(s),

            // Background
            SpawnBackgroundCommandSpec s => new SpawnBackgroundCommand(
                _bgViewPrefabProvider,
                s,
                _bgRuntimeRegistry,
                _presentationResponseRig),

            SetBackgroundSpriteCommandSpec s => new SetBackgroundSpriteCommand(s),
            FadeBackgroundCommandSpec s => new FadeBackgroundCommand(s),
            DestroyBackgroundCommandSpec s => new DestroyBackgroundCommand(
                s,
                _bgRuntimeRegistry,
                _presentationResponseRig),
            
            // Presentation Shot / Response Rig
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),
            
            ShotZoomFocusCommandSpec s => new ShotZoomFocusCommand(_presentationResponseRig, s),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),
            
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),
            
            SetCharRigCamFocusCommandSpec s => new SetCharRigCamFocusCommand(s),

            // Dialogue Box
            HideDialogueBoxCommandSpec s => new HideDialogueBoxCommand(s, _dialogueBoxResolver),

            _ => null
        };

        return command != null;
    }
}