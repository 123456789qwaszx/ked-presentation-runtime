using System;
using UnityEngine;

[Serializable]
public sealed class ShotFocusCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Focus")]
    [Tooltip("렌즈의 초점(무대 좌표, 절대값). 줌할 때 앞 · 뒤 층이 벌어지는 쪽의 기준이다.")]
    public Vector2 focus = Vector2.zero;
}

// shot_focus — 초점만 바꾼다. 줌 · 팬은 건드리지 않는다.
// 초점은 줌에 실린 값이라(ShotAxes.Zoom — 층 산개의 기준이라 줌과 한 몸이다) 줌 축을 다시 조준한다:
// 같은 줄에 shot_zoom과 함께 적으면 둘은 한 전환이고, 뒤에 적은 것의 길이 · 이징으로 함께 간다(값은 둘 다 남는다).
public sealed class ShotFocusCommand : ShotIntentCommandBase<ShotFocusCommandSpec>
{
    public ShotFocusCommand(PresentationShotResponseSystem rig, ShotFocusCommandSpec spec)
        : base(rig, spec)
    { }

    protected override ShotAxes Axes => ShotAxes.Zoom;

    protected override PresentationIntentState BuildTargetState(in PresentationIntentState from)
    {
        return Ked.Presentation.Core.ShotIntents
            .Focus(from.ToCore(), spec.focus.ToCore())
            .ToUnity();
    }
}
