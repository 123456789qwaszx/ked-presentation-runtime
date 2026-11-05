using System;
using UnityEngine;

[Serializable]
public sealed class ShotZoomFocusCommandSpec : ShotIntentCommandSpecBase
{
    [Header("Character Focus")]
    [Tooltip("캐릭터 key 또는 slot key입니다.")]
    public string focusRoleKey = "";

    [Tooltip("캐릭터 focus 예상 프리셋입니다. Transform anchor가 아니라 Character_CastTransform 기준 offset입니다.")]
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;

    [Tooltip("선택한 focus preset에 추가로 더할 최종 command-time offset입니다.")]
    public Vector2 focusOffset = Vector2.zero;

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
    private readonly PresentationStage _stage;
    CharacterFocusTuningDBSO _focusTuningDB;
    
    public ShotZoomFocusCommand(
        PresentationStage stage,
        PresentationShotResponseSystem rig, 
        ShotZoomFocusCommandSpec spec, 
        CharacterFocusTuningDBSO focusTuningDB)
        : base(rig, spec)
    {
        _stage = stage;
        _focusTuningDB = focusTuningDB;
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // 입력 수집
        // 정착 focus 측정과 화면 지점 해석. 측정은 무대 좌표(카메라 중립)로 한다 — 캐릭터의 무대 위 자리.
        CharacterFocusPointResult focus = CharacterFocusPointResolver.Resolve(
            _stage,
            spec.focusRoleKey,
            spec.focusPreset,
            spec.focusOffset,
            _focusTuningDB,
            useSettledPlacementTargets: true);

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.RigSpaceRoot, spec.screenPoint) + spec.screenOffset;

        // 논리 focus 복원 -> 목표 배율에서 pan 역산.
        // 코어 폴드(StageReducer.Shot)와 같은 경로: 무대 좌표 focus에 지금 카메라를 씌워 "보이는 위치"를 만들고,
        // 리덕션이 같은 카메라를 벗긴다. 결과는 무대 위 자리만으로 정해진다 — 샷의 진행도 · 깊이 층 시차와 무관.
        // 무대 중립을 모르면(조립 밖) 화면에서 잰 값을 그대로 넘긴다 — 지금 적용된 카메라(Presented)로 벗긴다.
        PresentationIntentState presented = rig.Presented;
        Vector2 measuredFocus = focus.FocusPointInRigSpace;

        if (focus.InStageSpace)
        {
            float presentedScale = PresentationShotIntentMath.EvaluateCameraScale(presented.zoom);
            measuredFocus = measuredFocus * presentedScale + presented.panInRigSpace;
        }

        return Ked.Presentation.Core.ShotZoomFocusReduction
            .Reduce(
                presented.ToCore(),
                spec.zoom,
                measuredFocus.ToCore(),
                desiredPoint.ToCore())
            .ToUnity();
    }
}