using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class OverlayRigOperations
{
    public static void SetSprite(
        this OverlayRigRefs refs,
        Sprite sprite,
        bool setNativeSize = true)
    {
        if (refs?.Overlay_Image == null)
            return;

        refs.Overlay_Image.sprite = sprite;
        refs.Overlay_Image.preserveAspect = true;

        if (setNativeSize && sprite != null && refs.Overlay_Size != null)
        {
            Rect rect = sprite.rect;
            refs.Overlay_Size.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }

    public static void SetText(
        this OverlayRigRefs refs,
        string text)
    {
        if (refs?.Overlay_Text == null)
            return;

        refs.Overlay_Text.text = text ?? string.Empty;
    }

    public static void SetVisibleImmediate(
        this OverlayRigRefs refs,
        bool visible)
    {
        if (refs?.Overlay_RootCanvasGroup == null)
            return;

        refs.Overlay_RootCanvasGroup.alpha = visible ? 1f : 0f;
    }

    public static void SetImageAlphaImmediate(
        this OverlayRigRefs refs,
        float alpha)
    {
        if (refs?.Overlay_Image == null)
            return;

        Color c = refs.Overlay_Image.color;
        c.a = alpha;
        refs.Overlay_Image.color = c;
    }

    public static void SetImageColorImmediate(
        this OverlayRigRefs refs,
        Color color)
    {
        if (refs?.Overlay_Image == null)
            return;

        refs.Overlay_Image.color = color;
    }

    public static void SetTextAlphaImmediate(
        this OverlayRigRefs refs,
        float alpha)
    {
        if (refs?.Overlay_Text == null)
            return;

        Color c = refs.Overlay_Text.color;
        c.a = alpha;
        refs.Overlay_Text.color = c;
    }

    public static void SetTextColorImmediate(
        this OverlayRigRefs refs,
        Color color)
    {
        if (refs?.Overlay_Text == null)
            return;

        refs.Overlay_Text.color = color;
    }

    public static void SetGraphicAlphaImmediate(
        this OverlayRigRefs refs,
        OverlayRigTarget target,
        float alpha)
    {
        Graphic graphic = refs.GetGraphic(target);

        if (graphic == null)
            return;

        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    public static void SetGraphicColorImmediate(
        this OverlayRigRefs refs,
        OverlayRigTarget target,
        Color color)
    {
        Graphic graphic = refs.GetGraphic(target);

        if (graphic == null)
            return;

        graphic.color = color;
    }

    public static void ResetToBaselineImmediate(this OverlayRigRefs refs)
    {
        if (refs == null)
            return;

        if (refs.Overlay_RootCanvasGroup != null)
        {
            refs.Overlay_RootCanvasGroup.alpha = 0f;
            refs.Overlay_RootCanvasGroup.interactable = false;
            refs.Overlay_RootCanvasGroup.blocksRaycasts = false;
        }

        SetPos(refs.Overlay_Anchor, Vector2.zero);

        // Screen-space / overlay-space movement track. Not affected by BaseRotation.
        SetPos(refs.Overlay_Track, Vector2.zero);
        SetRotZ(refs.Overlay_Track, 0f);

        // Rotated movement basis.
        SetRotZ(refs.Overlay_BaseRotation, 0f);

        SetPos(refs.Overlay_Track_Move, Vector2.zero);
        SetPos(refs.Overlay_Track_X, Vector2.zero);
        SetPos(refs.Overlay_Track_X_Offset, Vector2.zero);
        SetPos(refs.Overlay_Track_Y, Vector2.zero);
        SetPos(refs.Overlay_Track_Y_Offset, Vector2.zero);

        SetRotZ(refs.Overlay_Rotation, 0f);

        if (refs.Overlay_Size != null)
            refs.Overlay_Size.sizeDelta = Vector2.zero;

        SetScale(refs.Overlay_Scale, Vector3.one);
        SetScale(refs.Overlay_ActingScale, Vector3.one);
        SetScale(refs.Overlay_ActingScale_X, Vector3.one);
        SetScale(refs.Overlay_ActingScale_Y, Vector3.one);

        StretchFull(refs.Overlay_Content);

        StretchFull(refs.Overlay_ImageBox);
        StretchFull(refs.Overlay_ImagePad);

        if (refs.Overlay_Image != null)
        {
            refs.Overlay_Image.color = Color.white;
            refs.Overlay_Image.raycastTarget = false;
            refs.Overlay_Image.preserveAspect = true;
            refs.Overlay_Image.sprite = null;
        }

        StretchFull(refs.Overlay_TextBox);
        StretchFull(refs.Overlay_TextPad);

        if (refs.Overlay_Text != null)
        {
            refs.Overlay_Text.text = string.Empty;
            refs.Overlay_Text.color = Color.white;
            refs.Overlay_Text.raycastTarget = false;
            refs.Overlay_Text.alignment = TextAlignmentOptions.Center;
            refs.Overlay_Text.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    public static void KillAllTweens(this OverlayRigRefs refs, bool complete)
    {
        if (refs?.RigRoot == null)
            return;

        RectTransform[] rects =
            refs.RigRoot.GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                rects[i].DOKill(complete);
        }

        CanvasGroup[] canvasGroups =
            refs.RigRoot.GetComponentsInChildren<CanvasGroup>(true);

        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].DOKill(complete);
        }

        Graphic[] graphics =
            refs.RigRoot.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].DOKill(complete);
        }

        DOTween.Kill(refs.RigRoot, complete);
        DOTween.Kill(refs.RigRoot.gameObject, complete);
    }

    private static void SetPos(RectTransform rt, Vector2 pos)
    {
        if (rt != null)
            rt.anchoredPosition = pos;
    }

    private static void SetRotZ(RectTransform rt, float z)
    {
        if (rt != null)
            rt.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    private static void SetScale(RectTransform rt, Vector3 scale)
    {
        if (rt != null)
            rt.localScale = scale;
    }

    private static void StretchFull(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}
