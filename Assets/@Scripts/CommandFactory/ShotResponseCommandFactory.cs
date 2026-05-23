using UnityEngine;

public interface ICameraFocusStageRootProvider
{
    RectTransform StageRoot { get; }
}

public sealed partial class PresentationUIRoot : ICameraFocusStageRootProvider
{
    public RectTransform StageRoot => View.Rect(Refs.StageShot_Root);
}

public sealed class ShotResponseCommandFactory : INodeCommandFactory
{
    private readonly PresentationResponseRig _presentationResponseRig;
    private readonly CharacterFocusTuningDBSO _focusTuningDB;

    private ICameraFocusStageRootProvider _stageRootProvider;
    private bool _stageRootProviderInit;

    public ShotResponseCommandFactory(
        PresentationResponseRig presentationResponseRig,
        CharacterFocusTuningDBSO focusTuningDB)
    {
        _presentationResponseRig = presentationResponseRig;
        _focusTuningDB = focusTuningDB;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            RegisterBackgroundResponseBindingCommandSpec s => new RegisterBackgroundResponseBindingCommand(s, _presentationResponseRig, StageRootProvider),
            RegisterCharacterResponseBindingCommandSpec s => new RegisterCharacterResponseBindingCommand(s, _presentationResponseRig, StageRootProvider),

            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),
            
            ShotZoomFocusCommandSpec s => new ShotZoomFocusCommand(_presentationResponseRig, s, _focusTuningDB),
            ShotToCommandSpec s => new ShotToCommand(_presentationResponseRig, s),
            
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

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