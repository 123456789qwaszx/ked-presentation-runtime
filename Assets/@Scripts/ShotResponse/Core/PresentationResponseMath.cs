using UnityEngine;

public static class PresentationResponseMath
{
    public static PresentationTargetResponse CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile,
        in PresentationResponseMeasure measure)
    {
        float cameraScale = PresentationShotIntentMath.EvaluateCameraScale(state.zoom);
        float safeCameraScale = Mathf.Max(0.0001f, cameraScale);

        // 실제 카메라 스케일 변화량을 response 기준으로 사용.
        // 예: zoom=8, cameraScale=1.4라면 zoomAmount=0.4.
        float zoomAmount = safeCameraScale - 1f;

        Vector2 focusSpreadOffset =
            CalculateAxisSeparatedFocusSpreadOffset(
                measure.basePositionInRigSpace,
                state.focusPointInRigSpace,
                zoomAmount,
                profile.focusSpreadPixelsPerZoom);

        Vector2 panOffset =
            CalculateAxisSeparatedPanOffset(
                state.panInRigSpace,
                profile.panResponse);

        Vector2 destPos =
            measure.basePositionInRigSpace +
            focusSpreadOffset +
            panOffset;

        // StageDepth layer는 이미 StageZoom_Root 아래에 있다.
        // 따라서 layer scale response는 "카메라 스케일을 얼마나 상쇄/강조할지"로 해석.
        // 음수 = 상쇄, 양수 = 조금 더 반응
        float responseScaleMultiplier =
            Mathf.Pow(safeCameraScale, profile.zoomScaleResponse);

        Vector2 destScale =
            measure.baseLocalScale * responseScaleMultiplier;

        return new PresentationTargetResponse
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }

    private static Vector2 CalculateAxisSeparatedFocusSpreadOffset(
        Vector2 basePositionInRigSpace,
        Vector2 focusPointInRigSpace,
        float zoomAmount,
        Vector2 focusSpreadPixelsPerZoom)
    {
        Vector2 fromFocus = basePositionInRigSpace - focusPointInRigSpace;

        float signX = Mathf.Approximately(fromFocus.x, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.x);

        float signY = Mathf.Approximately(fromFocus.y, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.y);

        return new Vector2(
            signX * focusSpreadPixelsPerZoom.x * zoomAmount,
            signY * focusSpreadPixelsPerZoom.y * zoomAmount);
    }

    private static Vector2 CalculateAxisSeparatedPanOffset(
        Vector2 panInRigSpace,
        Vector2 panResponse)
    {
        return new Vector2(
            panInRigSpace.x * panResponse.x,
            panInRigSpace.y * panResponse.y);
    }
}