using UnityEngine;

public interface ICameraFocusStageRootProvider
{
    RectTransform StageRoot { get; }
}

public sealed partial class PresentationUIRoot : ICameraFocusStageRootProvider
{
    public RectTransform StageRoot => View.Rect(Refs.StageShot_Root);
}

public interface IDialogueBoxViewResolver
{
    IDialogueTextTarget ResolveTarget(DialogueBoxKind kind);
    void ShowOnly(IDialogueTextTarget target);
    void HideAll();
}

public sealed class PresentationViewCommandFactory : INodeCommandFactory
{
    private readonly PresentationResponseRig _presentationResponseRig;
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;

    private ICameraFocusStageRootProvider _stageRootProvider;
    private bool _stageRootProviderInit;

    public PresentationViewCommandFactory(
        PresentationResponseRig presentationResponseRig,
        IDialogueBoxViewResolver dialogueBoxViewResolver)
    {
        _presentationResponseRig = presentationResponseRig;
        _dialogueBoxResolver = dialogueBoxViewResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Presentation Response Binding
            RegisterBackgroundResponseBindingCommandSpec s => new RegisterBackgroundResponseBindingCommand(s, _presentationResponseRig, StageRootProvider),
            RegisterCharacterResponseBindingCommandSpec s => new RegisterCharacterResponseBindingCommand(s, _presentationResponseRig, StageRootProvider),

            // Transition
            SlantedMaskSlideInCommandSpec s => new SlantedMaskSlideInCommand(s),
            SlantedMaskSlideOutCommandSpec s => new SlantedMaskSlideOutCommand(s),

            VerticalStripWipeCommandSpec s => new VerticalStripWipeCommand(s),
            SlantedShutterCommandSpec s => new SlantedShutterCommand(s),
            FocusBlurFadeCommandSpec s => new FocusBlurFadeCommand(s),
            FocusBlurCurtainCommandSpec s => new FocusBlurCurtainCommand(s),

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

    private ICameraFocusStageRootProvider StageRootProvider
    {
        get
        {
            if (!_stageRootProviderInit)
                EnsureStageRootProvider();

            return _stageRootProvider;
        }
    }

    private void EnsureStageRootProvider()
    {
        _stageRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (_stageRootProvider != null)
            _stageRootProviderInit = true;
    }
}