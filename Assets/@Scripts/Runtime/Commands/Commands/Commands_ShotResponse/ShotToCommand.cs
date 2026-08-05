using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot To", Order = -847)]
public sealed class ShotToCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Pan")]
    public Vector2 pan = Vector2.zero;

    [Header("Zoom")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotToCommand : ShotIntentCommandBase<ShotToCommandSpec>
{
    public ShotToCommand(PresentationShotResponseSystem rig, ShotToCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-5 shot 묶음).
        return PresentationIntentStateCoreBridge.FromCore(
            Ked.Presentation.Core.ShotToReduction.Reduce(
                PresentationIntentStateCoreBridge.ToCore(from),
                spec.zoom,
                new Ked.Presentation.Core.Vec2(spec.pan.x, spec.pan.y)));
    }
}