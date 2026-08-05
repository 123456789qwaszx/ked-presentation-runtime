
using UnityEngine;

public static class PresentationShotIntentMath
{
    // zoom→배율·pan 역산 규약의 원천은 코어다 (U13-b-5 shot 묶음).
    // 커맨드의 목표 계산과 여기 적용측이 같은 규약을 봐야 하므로 위임만 한다.
    public static float EvaluateCameraScale(
        float zoom)
        => Ked.Presentation.Core.ShotIntentMath.EvaluateCameraScale(zoom);

    // 현재 보이는 위치에서 authored screen-pan 값을 제거하여, 원래 논리 좌표 복원.
    public static Vector2 RemoveCurrentCameraTransformFromFocusPoint(
        Vector2 focusPointInStageSpace,
        Vector2 currentPan,
        float currentScale)
        => (focusPointInStageSpace - currentPan) / currentScale;
    
    // 캐릭터 얼굴이 원하는 화면 위치에 보이도록 authored screen-pan 값을 역산.
    // Positive pan means the visible presentation moves in that direction,
    // while the camera root applies the inverse transform.
    public static Vector2 CalculatePanToPlaceFocusAtScreenPoint(
        Vector2 logicalFocusPointInStageSpace, 
        Vector2 desiredPointInStageSpace, 
        float targetScale)
        => desiredPointInStageSpace - logicalFocusPointInStageSpace * targetScale;

    // This interpolates authored intent values, not final Transform values.
    // Each frame re-solves the final response from the current interpolated intent.
    public static PresentationIntentState Interpolate(
        in PresentationIntentState from,
        in PresentationIntentState to,
        float t)
        => new() {
            zoom = Mathf.Lerp(from.zoom, to.zoom, t),
            panInRigSpace = Vector2.Lerp(from.panInRigSpace, to.panInRigSpace, t),
            focusPointInRigSpace = Vector2.Lerp(from.focusPointInRigSpace, to.focusPointInRigSpace, t) };

    // Used to skip unnecessary tweens.
    public static bool ApproximatelyEqual(
        in PresentationIntentState a, 
        in PresentationIntentState b) 
        => Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
           Vector2.SqrMagnitude(a.panInRigSpace - b.panInRigSpace) <= 0.0001f &&
           Vector2.SqrMagnitude(a.focusPointInRigSpace - b.focusPointInRigSpace) <= 0.0001f;
}