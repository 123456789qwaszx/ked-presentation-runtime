using System;
using UnityEngine;

[Serializable]
public sealed class ShotResetCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Scope")]
    [Tooltip("끄면 렌즈(줌 · 팬 · 초점)만 중립으로 — shot_reset. 켜면 카메라를 통째로(몸 · 롤까지) — shot_reset_all.")]
    public bool all;
}

// shot_reset — 렌즈만 중립으로 돌린다. 장면의 구도(카메라의 몸)와 기울기는 그대로 두고 그 자리의 중립 렌즈로 돌아온다.
// shot_reset_all — 카메라를 통째로 중립으로.
public sealed class ShotResetCommand : ShotIntentCommandBase<ShotResetCommandSpec>
{
    public ShotResetCommand(PresentationShotResponseSystem rig, ShotResetCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => spec.all ? ShotAxes.All : ShotAxes.Lens;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return spec.all
            ? Ked.Presentation.Core.ShotIntents.ResetAll().ToUnity()
            : Ked.Presentation.Core.ShotIntents.Reset(from.ToCore()).ToUnity();
    }
}
