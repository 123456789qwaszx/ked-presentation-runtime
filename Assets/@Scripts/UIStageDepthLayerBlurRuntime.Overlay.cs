using UnityEngine;
using UnityEngine.UI;

// - ApplyBlurTextureToOverlay
// - overlay uvRect sync
// - coverage padding apply/reset
// - screen pixel → parent local padding 변환
// - StopTrackingAndHideImmediate
public sealed partial class UIStageDepthLayerBlurRuntime
{
    // ── overlay ────────────────────────────────────────────────────────────────

    // RawImage의 texture/uvRect/enabled는 Baker가 소유한다. alpha는 Command가 소유한다.
    private void ApplyBlurTextureToOverlay(LayerState state)
    {
        if (!state.Target.IsValid)
            return;

        RawImage rawImage = state.Target.OverlayRawImage;

        if (rawImage.texture != state.BakedTexture)
            rawImage.texture = state.BakedTexture;

        // 텍스처가 준비된 시점에만 켠다(빈 RawImage 흰색 번쩍임 방지).
        if (!rawImage.enabled)
            rawImage.enabled = true;

        rawImage.raycastTarget = false;

        SyncOverlayUvRectToScreen(rawImage);
    }

    // BakedTexture는 화면 전체 기준 screen-space RT다. RawImage는 depth layer 안쪽 렌더 순서를 지키되,
    // 현재 화면에서 차지하는 영역만 RT에서 샘플링하도록 uvRect를 맞춘다.
    private void SyncOverlayUvRectToScreen(RawImage rawImage)
    {
        if (rawImage == null)
            return;

        RectTransform rt = rawImage.rectTransform;

        if (rt == null)
            return;

        rt.GetWorldCorners(_overlayWorldCorners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < _overlayWorldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, _overlayWorldCorners[i]);

            minX = Mathf.Min(minX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxX = Mathf.Max(maxX, screen.x);
            maxY = Mathf.Max(maxY, screen.y);
        }

        float invScreenWidth = 1f / Mathf.Max(1, Screen.width);
        float invScreenHeight = 1f / Mathf.Max(1, Screen.height);

        rawImage.uvRect = new Rect(
            minX * invScreenWidth,
            minY * invScreenHeight,
            (maxX - minX) * invScreenWidth,
            (maxY - minY) * invScreenHeight);
    }

    private void ApplyOverlayCoveragePadding(LayerState state)
    {
        if (state == null || !state.Target.IsValid)
            return;

        RectTransform overlayRect = state.Target.OverlayCanvasGroup.transform as RectTransform;
        RectTransform rawImageRect = state.Target.OverlayRawImage.rectTransform;

        if (overlayRect == null || rawImageRect == null)
            return;

        if (!state.OverlayPaddingCaptured)
        {
            state.BaseOverlayOffsetMin = overlayRect.offsetMin;
            state.BaseOverlayOffsetMax = overlayRect.offsetMax;
            state.BaseRawImageOffsetMin = rawImageRect.offsetMin;
            state.BaseRawImageOffsetMax = rawImageRect.offsetMax;
            state.OverlayPaddingCaptured = true;
        }

        float padding = Mathf.Max(0f, state.CoveragePaddingPixels);

        Vector2 overlayPadding = ConvertScreenPixelsToParentLocalPadding(overlayRect, padding);
        Vector2 rawPadding = ConvertScreenPixelsToParentLocalPadding(rawImageRect, padding);

        overlayRect.offsetMin = state.BaseOverlayOffsetMin - overlayPadding;
        overlayRect.offsetMax = state.BaseOverlayOffsetMax + overlayPadding;

        rawImageRect.offsetMin = state.BaseRawImageOffsetMin - rawPadding;
        rawImageRect.offsetMax = state.BaseRawImageOffsetMax + rawPadding;
    }

    private void ResetOverlayCoveragePadding(LayerState state)
    {
        if (state == null || !state.Target.IsValid)
            return;

        if (!state.OverlayPaddingCaptured)
            return;

        RectTransform overlayRect = state.Target.OverlayCanvasGroup.transform as RectTransform;
        RectTransform rawImageRect = state.Target.OverlayRawImage.rectTransform;

        if (overlayRect != null)
        {
            overlayRect.offsetMin = state.BaseOverlayOffsetMin;
            overlayRect.offsetMax = state.BaseOverlayOffsetMax;
        }

        if (rawImageRect != null)
        {
            rawImageRect.offsetMin = state.BaseRawImageOffsetMin;
            rawImageRect.offsetMax = state.BaseRawImageOffsetMax;
        }
    }

    private static Vector2 ConvertScreenPixelsToParentLocalPadding(RectTransform rect, float pixels)
    {
        if (rect == null || pixels <= 0f)
            return Vector2.zero;

        RectTransform parent = rect.parent as RectTransform;

        if (parent == null)
            return new Vector2(pixels, pixels);

        Camera camera = null;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            Vector2.zero,
            camera,
            out Vector2 localA);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            new Vector2(pixels, pixels),
            camera,
            out Vector2 localB);

        Vector2 delta = localB - localA;

        return new Vector2(
            Mathf.Abs(delta.x),
            Mathf.Abs(delta.y));
    }

    // source가 비어 추적할 게 없으면 추적 중단 + overlay 끔.
    // (alpha는 Command 소유이므로 여기서 건드리지 않는다. RawImage만 끈다.)
    private void StopTrackingAndHideImmediate(LayerState state)
    {
        state.IsTracking = false;

        ResetOverlayCoveragePadding(state);

        if (state.Target.IsValid)
            state.Target.OverlayRawImage.enabled = false;
    }
}
