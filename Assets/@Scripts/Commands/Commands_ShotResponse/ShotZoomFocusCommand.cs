using System;
using UnityEngine;

[Serializable]
[CommandMenuHint("Presentation Shot", "Shot Zoom Focus", Order = -849)]
public sealed class ShotZoomFocusCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Character Focus")]
    [Tooltip("캐릭터 key 또는 slot key입니다.")]
    public string focusRoleKey = "";

    [Tooltip("캐릭터 focus 예상 프리셋입니다. Transform anchor가 아니라 Character_CastTransform 기준 offset입니다.")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("focusPreset이 Custom일 때 사용할 custom point key입니다. 예: hand_left, weapon, phone")]
    public string customFocusKey = "";

    //[Tooltip("캐릭터/포즈별 focus 보정 DB입니다.")]
    //public CharacterFocusTuningDBSO focusTuningDb;

    [Tooltip("선택한 focus preset에 추가로 더할 최종 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Optional Pose Tuning")]
    [Tooltip("비워두면 focusRoleKey만 tuning key로 사용합니다. 입력하면 roleKey:poseKey로 DB를 찾습니다.")]
    public string poseKey = "";

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
    CharacterFocusTuningDBSO _focusTuningDB;
    
    public ShotZoomFocusCommand(
        PresentationResponseRig rig,
        ShotZoomFocusCommandSpec spec,
        CharacterFocusTuningDBSO focusTuningDB)
        : base(rig, spec)
    {
        _focusTuningDB = focusTuningDB;
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        if (!CharacterFocusPointResolver.TryResolve(
                scope,
                spec.focusRoleKey,
                spec.focusPreset,
                spec.poseKey,
                spec.customFocusKey,
                spec.focusOffset,
                _focusTuningDB,
                out CharacterFocusPointResult focus))
        {
            return from;
        }

        float targetZoom = PresentationShotIntentMath.ClampZoom(spec.zoom);

        float fromScale = PresentationShotIntentMath.EvaluateCameraScale(from.zoom);
        float targetScale = PresentationShotIntentMath.EvaluateCameraScale(targetZoom);

        Vector2 logicalFocusPoint =
            PresentationShotIntentMath.ToLogicalFocusPoint(
                focus.FocusPointInStageSpace,
                from.pan,
                fromScale);

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.StageRoot, spec.screenPoint) +
            spec.screenOffset;

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