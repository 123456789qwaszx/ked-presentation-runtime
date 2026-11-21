using System;

public sealed class ShotResponseCommandFactory : INodeCommandFactory
{
    private readonly PresentationShotResponseSystem _presentationResponseRig;

    public ShotResponseCommandFactory(PresentationShotResponseSystem presentationResponseRig)
    {
        _presentationResponseRig = presentationResponseRig ?? throw new ArgumentNullException(nameof(presentationResponseRig));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,
            
            ShotResetCommandSpec s => new ShotResetCommand(_presentationResponseRig, s),

            // 축마다 하나 — 렌즈 · 헤드 · 몸
            ShotZoomCommandSpec s => new ShotZoomCommand(_presentationResponseRig, s),
            ShotPanCommandSpec s => new ShotPanCommand(_presentationResponseRig, s),
            ShotFocusCommandSpec s => new ShotFocusCommand(_presentationResponseRig, s),
            ShotRollCommandSpec s => new ShotRollCommand(_presentationResponseRig, s),
            ShotDollyCommandSpec s => new ShotDollyCommand(_presentationResponseRig, s),
            ShotTrackCommandSpec s => new ShotTrackCommand(_presentationResponseRig, s),

            // 연기 — 흔들림
            ShotShakeCommandSpec s => new ShotShakeCommand(_presentationResponseRig, s),

            _ => null
        };

        return command != null;
    }
}