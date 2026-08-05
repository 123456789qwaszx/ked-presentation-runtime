using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom", Order = -850)]
public sealed class ShotZoomCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Zoom")]
    [Tooltip("목표 zoom intent 값")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotZoomCommand : ShotIntentCommandBase<ShotZoomCommandSpec>
{
    public ShotZoomCommand(PresentationShotResponseSystem rig, ShotZoomCommandSpec spec)
        : base(rig, spec)
    { }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-5 shot 묶음).
        return PresentationIntentStateCoreBridge.FromCore(
            Ked.Presentation.Core.ShotZoomReduction.Reduce(
                PresentationIntentStateCoreBridge.ToCore(from),
                spec.zoom));
    }
}