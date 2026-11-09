using UnityEngine;
using UnityEngine.UI;

public enum CharRigImageSizingMode
{
    // No sizing. Leave RectTransform as-is (sprite swap only).
    KeepLayout = 0,

    // Fit by parent height and preserve sprite aspect ratio.
    // - Sets sizeDelta.y = 0
    // - Does not touch anchors (may adjust pivot.x for left/center/right alignment)
    HeightFitPreserveAspect = 1,

    // Set RectTransform to the sprite's native size (pixelsPerUnit applied).
    // - Writes sizeDelta only
    // - Does not modify anchors / pivot / anchoredPosition
    NativeSizeNoReanchor = 2,

    // 배경 — 원본 픽셀 크기 그대로(캔버스 기준 1px = 원본 1px), 피벗 자리(가운데)에 (2026-11-09).
    // 화면보다 큰 배경은 화면 밖까지 걸치고 카메라가 그 안을 본다.
    // - sizeDelta = 그림 크기 − 부모 크기 × (anchorMax − anchorMin) — 앵커에 상관없이 rect = 그림 크기
    // - 앵커 · 피벗 · anchoredPosition은 건드리지 않는다
    // 코어 NodeTargets.BackgroundSizing이 같은 식으로 접는다.
    SourcePixels = 3,
}

public static class CharRigImageSizingPolicy
{
    public enum HorizontalAlign { Left, Center, Right }
    
    public static void Apply(Image image, Sprite sprite, CharRigImageSizingMode mode, HorizontalAlign align = HorizontalAlign.Center)
    {
        if (sprite == null)
            return;

        image.preserveAspect = true;

        switch (mode)
        {
            case CharRigImageSizingMode.KeepLayout:
                break;

            case CharRigImageSizingMode.HeightFitPreserveAspect:
                RectTransform parent = image.rectTransform.parent as RectTransform;
                ApplyHeightFitPreserveAspect(image.rectTransform, parent, sprite, align);
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
        // 배경 임포터가 상한을 올리고(BackgroundTextureImporter), 익스포터가 걸린 그림을 경고한다.
        imageRect.sizeDelta = new Vector2(
            sprite.rect.width - parentSize.x * anchorSpan.x,
            sprite.rect.height - parentSize.y * anchorSpan.y);
    }
    
    private static void ApplyHeightFitPreserveAspect(RectTransform imageRect, RectTransform parent, Sprite sprite, HorizontalAlign align)
    {
        float parentHeight = parent.rect.height;
        float aspect = sprite.rect.width / sprite.rect.height;
        
        float targetWidth = parentHeight * aspect;

        Vector2 sizeDelta = imageRect.sizeDelta;
        sizeDelta.x = targetWidth;
        sizeDelta.y = 0f;
        imageRect.sizeDelta = sizeDelta;

        Vector2 pivot = imageRect.pivot;
        Vector2 pos = imageRect.anchoredPosition;

        switch (align)
        {
            case HorizontalAlign.Left:
                pivot.x = 0f;
                pos.x = 0f;
                break;
            case HorizontalAlign.Right:
                pivot.x = 1f;
                pos.x = 0f;
                break;
            case HorizontalAlign.Center:
                pivot.x = 0.5f;
                pos.x = 0f;
                break;
        }

        imageRect.pivot = pivot;
        imageRect.anchoredPosition = pos;
    }

    private static void ApplyNativeSizeNoReanchor(RectTransform imgRt, Sprite sprite)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 native = new Vector2(sprite.rect.width / pixelsPerUnit, sprite.rect.height / pixelsPerUnit);

        imgRt.sizeDelta = native;
    }
}