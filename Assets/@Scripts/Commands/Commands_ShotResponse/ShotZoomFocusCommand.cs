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

        // 1. 이번에 원하는 zoom 값을 정한다.
        float targetZoom = spec.zoom;

        // 2. 현재 zoom이 실제 scale로는 얼마인지 구한다.
        float fromScale = PresentationShotIntentMath.EvaluateCameraScale(from.zoom);
        
        // 3. 목표 zoom이 실제 scale로는 얼마인지 구한다.
        float targetScale = PresentationShotIntentMath.EvaluateCameraScale(targetZoom);

        // 4. 현재 측정된 캐릭터 focus 위치에서 현재 pan/scale을 제거해서 논리 focusPoint로 되돌린다.
        Vector2 logicalFocusPoint =
            PresentationShotIntentMath.RemoveCurrentCameraTransformFromFocusPoint(focus.FocusPointInStageSpace, from.pan, fromScale);

        // 5. 화면의 어느 지점에 focus를 두고 싶은지 구한다. 예: Center, Left, Right, Upper 등
        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.StageRoot, spec.screenPoint) + spec.screenOffset;

        // 6. targetScale에서 logicalFocusPoint가 desiredPoint에 오도록 필요한 targetPan을 계산한다.
        Vector2 targetPan =
            PresentationShotIntentMath.CalculatePanToPlaceFocusAtScreenPoint(logicalFocusPoint, desiredPoint, targetScale);

        // 7. zoom, pan, focusPoint를 새 IntentState로 만든다.
        return new PresentationIntentState
        {
            zoom = targetZoom,
            pan = targetPan,
            focusPoint = logicalFocusPoint,
        };
    }
}