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
    CharacterFocusTuningDBSO _focusTuningDB;
    private readonly IShotResponseStageProvider _stageProvider;
    
    public ShotZoomFocusCommand(
        PresentationShotResponseSystem rig, 
        ShotZoomFocusCommandSpec spec, 
        CharacterFocusTuningDBSO focusTuningDB,
        IShotResponseStageProvider stageProvider)
        : base(rig, spec)
    {
        _focusTuningDB = focusTuningDB;
        _stageProvider = stageProvider;
    }

    protected override PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope)
    {
        // 호스트가 모으는 입력: 정착 focus 측정(b-3 이후 순수)과 화면 지점 해석.
        CharacterFocusPointResolver.TryResolve(
            scope,
            _stageProvider,
            spec.focusRoleKey,
            spec.focusPreset,
            spec.focusOffset,
            _focusTuningDB,
            useSettledPlacementTargets: true,
            out CharacterFocusPointResult focus);

        Vector2 desiredPoint =
            ScreenFocusPointResolver.Resolve(focus.RigSpaceRoot, spec.screenPoint) + spec.screenOffset;

        // "스펙 → 목표 상태" 변환(현 카메라 제거 → 논리 좌표 복원 → pan 역산)은
        // 코어 리덕션이 한다 (U13-b-5 shot 묶음).
        return PresentationIntentStateCoreBridge.FromCore(
            Ked.Presentation.Core.ShotZoomFocusReduction.Reduce(
                PresentationIntentStateCoreBridge.ToCore(from),
                spec.zoom,
                new Ked.Presentation.Core.Vec2(focus.FocusPointInRigSpace.x, focus.FocusPointInRigSpace.y),
                new Ked.Presentation.Core.Vec2(desiredPoint.x, desiredPoint.y)));
    }
}