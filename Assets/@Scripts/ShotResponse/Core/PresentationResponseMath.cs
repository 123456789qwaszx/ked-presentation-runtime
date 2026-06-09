using UnityEngine;

public static class PresentationResponseMath
{
    public static PresentationTargetResponse CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile,
        in PresentationResponseMeasure measure)
    {
        Vector2 focusToTarget = measure.basePositionInRigSpace - state.focusPointInRigSpace;
        Vector2 directionFromFocus = focusToTarget.normalized;

        float spreadDistance = state.zoom * profile.focusSpreadPixelsPerZoom;
        Vector2 focusSpreadOffset = directionFromFocus * spreadDistance;

        Vector2 destPos =
            measure.basePositionInRigSpace +
            state.panInRigSpace * profile.panResponse +
            focusSpreadOffset;

        Vector2 destScale =
            measure.baseLocalScale * (1f + state.zoom);

        return new PresentationTargetResponse
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }
}