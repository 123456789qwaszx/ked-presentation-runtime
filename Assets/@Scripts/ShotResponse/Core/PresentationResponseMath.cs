using UnityEngine;

public static class PresentationResponseMath
{
    public static PresentationTargetResponse CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        Vector2 directionFromFocus = (profile.basePositionInRigSpace - state.focusPointInRigSpace).normalized;
        float spreadDistance = state.zoom * profile.focusSpreadPixelsPerZoom;
        
        Vector2 focusSpreadOffset = directionFromFocus * spreadDistance;

        Vector2 destPos = profile.basePositionInRigSpace + state.panInRigSpace * profile.panResponse + focusSpreadOffset;
        Vector2 destScale = profile.baseLocalScale * (1f + state.zoom);
        
        return new PresentationTargetResponse 
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }
}