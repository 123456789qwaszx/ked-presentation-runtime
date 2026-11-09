using UnityEngine;
using UnityEngine.UI;

public enum CharRigImageSizingMode
{
    // No sizing. Leave RectTransform as-is (sprite swap only).
    KeepLayout = 0,

    // 1은 비워 둔다 — HeightFitPreserveAspect(부모 높이에 맞추고 종횡비 유지)였다. 2026-11-09에 걷었다.
    // 직렬화된 1이 엉뚱한 방식으로 읽히지 않게 다시 쓰지 않는다.

    // Set RectTransform to the sprite's native size (pixelsPerUnit applied).
    // - Writes sizeDelta only
    // - Does not modify anchors / pivot / anchoredPosition
    NativeSizeNoReanchor = 2,

    // 초상 · 배경 — 원본 픽셀 크기 그대로(캔버스 기준 1px = 원본 1px), 피벗 자리(가운데)에 (2026-11-09).
    // 화면보다 큰 배경은 화면 밖까지 걸치고 카메라가 그 안을 본다. 초상의 화면 크기와 자리는
    // 역할 앵커(visualScale · offset → CharacterPortrait_VisualOffset)가 정한다.
    // - sizeDelta = 그림 크기 − 부모 크기 × (anchorMax − anchorMin) — 앵커에 상관없이 rect = 그림 크기
    // - 앵커 · 피벗 · anchoredPosition은 건드리지 않는다
    // 코어 NodeTargets.SourcePixelsSizing이 같은 식으로 접는다.
    SourcePixels = 3,
}

public static class CharRigImageSizingPolicy
{
    public static void Apply(Image image, Sprite sprite, CharRigImageSizingMode mode)
    {
        if (sprite == null)
            return;

        image.preserveAspect = true;

        switch (mode)
        {
            case CharRigImageSizingMode.KeepLayout:
                break;

            case CharRigImageSizingMode.NativeSizeNoReanchor:
                ApplyNativeSizeNoReanchor(image.rectTransform, sprite);
                break;

            case CharRigImageSizingMode.SourcePixels:
                ApplySourcePixels(image.rectTransform, sprite);
                break;
        }
    }

    private static void ApplySourcePixels(RectTransform imageRect, Sprite sprite)
    {
        RectTransform parent = imageRect.parent as RectTransform;
        Vector2 parentSize = parent != null ? parent.rect.size : Vector2.zero;
        Vector2 anchorSpan = imageRect.anchorMax - imageRect.anchorMin;

        // sprite.rect = 임포트된 픽셀 크기. 임포터 상한에 걸리면 원본보다 작다 —
        // 임포터가 상한을 올리고(BackgroundTextureImporter · PortraitTextureImporter), 익스포터가 걸린 그림을 경고한다.
        imageRect.sizeDelta = new Vector2(
            sprite.rect.width - parentSize.x * anchorSpan.x,
            sprite.rect.height - parentSize.y * anchorSpan.y);
    }
    
    private static void ApplyNativeSizeNoReanchor(RectTransform imgRt, Sprite sprite)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 native = new Vector2(sprite.rect.width / pixelsPerUnit, sprite.rect.height / pixelsPerUnit);

        imgRt.sizeDelta = native;
    }
}