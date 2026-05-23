using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom Focus", Order = -849)]
public sealed class ShotZoomFocusCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Character Focus")]
    public string focusRoleKey = "";

    [Tooltip("Legacy field. 현재 ShotZoomFocusCommand에서는 사용하지 않습니다. focusLocalOffset으로 보정합니다.")]
    public CharacterFocusAnchor focusAnchor = CharacterFocusAnchor.Face;

    [Tooltip("선택한 focus 기준점에 추가로 더할 오프셋입니다.")]
    public Vector2 focusLocalOffset = Vector2.zero;

    [Header("Screen Focus")]
    public ScreenFocusPoint screenPoint = ScreenFocusPoint.Center;

    [Tooltip("ScreenFocusPoint에 추가로 더할 오프셋. Stage local space 기준.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Intent")]
    [Range(-10f, 10f)]
    public float zoom = 0f;
}

public sealed class ShotZoomFocusCommand : ShotIntentCommandBase<ShotZoomFocusCommandSpec>
{
    public ShotZoomFocusCommand(
        PresentationResponseRig rig,
        ShotZoomFocusCommandSpec spec)
        : base(rig, spec)
    {
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        if (!CharacterFocusPointResolver.TryResolve(
                scope,
                Spec.focusRoleKey,
                Spec.focusLocalOffset,
                out CharacterFocusPointResult focus))
        {
            return from;
        }

        float targetZoom = PresentationShotIntentMath.ClampZoom(Spec.zoom);

        float fromScale = PresentationShotIntentMath.EvaluateScale(from.zoom);
        float targetScale = PresentationShotIntentMath.EvaluateScale(targetZoom);

        Vector2 logicalFocusPoint =
            PresentationShotIntentMath.ToLogicalFocusPoint(
                focus.FocusPointInStageSpace,
                from.pan,
                fromScale);

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.StageRoot, Spec.screenPoint) +
            Spec.screenOffset;

        Vector2 targetPan =
            PresentationShotIntentMath.CalculatePanForFocus(
                logicalFocusPoint,
                desiredPoint,
                targetScale);

        return new PresentationIntentState
        {
            zoom = targetZoom,
            pan = targetPan,
            focusPoint = logicalFocusPoint,
        };
    }
}