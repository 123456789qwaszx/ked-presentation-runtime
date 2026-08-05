using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Track", Order = -848)]
public sealed class ShotTrackCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    [Tooltip("현재 pan에 더할 값")]
    public Vector2 pan = Vector2.zero;
}

public sealed class ShotTrackCommand : ShotIntentCommandBase<ShotTrackCommandSpec>
{
    public ShotTrackCommand(PresentationShotResponseSystem rig, ShotTrackCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-5 shot 묶음).
        return PresentationIntentStateCoreBridge.FromCore(
            Ked.Presentation.Core.ShotTrackReduction.Reduce(
                PresentationIntentStateCoreBridge.ToCore(from),
                new Ked.Presentation.Core.Vec2(spec.pan.x, spec.pan.y)));
    }
}