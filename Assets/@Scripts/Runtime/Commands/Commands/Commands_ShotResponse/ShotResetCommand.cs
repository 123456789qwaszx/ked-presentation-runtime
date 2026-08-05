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
        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-5 shot 묶음).
        return PresentationIntentStateCoreBridge.FromCore(
            Ked.Presentation.Core.ShotResetReduction.Reduce());
    }
}