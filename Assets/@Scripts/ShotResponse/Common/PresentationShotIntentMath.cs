using UnityEngine;

public static class PresentationShotIntentMath
{
    // Camera scale conversion used by the camera root.
    private const float DefaultZoomToScaleFactor = 0.05f;

    public static float EvaluateCameraScale(
        float zoom) 
        => Mathf.Min(1f + zoom * DefaultZoomToScaleFactor);

    // 현재 보이는 위치에서의 카메라 값을 제거하여, 원래 좌표 복원.
    public static Vector2 RemoveCurrentCameraTransformFromFocusPoint(
        Vector2 focusPointInStageSpace, 
        Vector2 currentPan, 
        float currentScale)
        => (focusPointInStageSpace - currentPan) / currentScale;
    
    // 캐릭터 얼굴이 원하는 위치에 보이도록 카메라 pan 값을 역산.
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
            pan = Vector2.Lerp(from.pan, to.pan, t),
            focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t) };

    // Used to skip unnecessary tweens.
    public static bool ApproximatelyEqual(
        in PresentationIntentState a, 
        in PresentationIntentState b) 
        => Mathf.Abs(a.zoom - b.zoom) <= 0.0001f &&
           Vector2.SqrMagnitude(a.pan - b.pan) <= 0.0001f &&
           Vector2.SqrMagnitude(a.focusPoint - b.focusPoint) <= 0.0001f;
}


// ShotIntentState토대로, 개별 대상의 반응을 계산
public static class PresentationResponseMath
{
    public static PresentationResponseBinding.Response CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        Vector2 focusToTarget = profile.basePositionInRigSpace - state.focusPoint;
        Vector2 directionFromFocus = focusToTarget.normalized;
        
        float spreadDistance = state.zoom * profile.focusSpreadPixelsPerZoom;
        Vector2 focusSpreadOffset = directionFromFocus * spreadDistance;

        Vector2 destPos = profile.basePositionInRigSpace + state.pan * profile.panResponse + focusSpreadOffset;
        Vector2 destScale = profile.baseLocalScale * (1f + state.zoom);
        
        return new PresentationResponseBinding.Response
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }
}