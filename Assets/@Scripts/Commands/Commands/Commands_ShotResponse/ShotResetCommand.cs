using System;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Reset", Order = -849)]
public sealed class ShotResetCommandSpec : ShotIntentCommandSpecBase 
{ }

public sealed class ShotResetCommand : ShotIntentCommandBase<ShotResetCommandSpec>
{
    public ShotResetCommand(PresentationShotResponseSystem rig, ShotResetCommandSpec spec) 
        : base(rig, spec) 
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        return PresentationIntentState.Default;
    }
}