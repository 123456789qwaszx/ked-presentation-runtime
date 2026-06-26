using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class SpriteImageRigOperations
{
    public static void SetSprite(
        this SpriteImageRigRefs refs,
        Sprite sprite,
        bool setNativeSize = true)
    {
        if (refs?.Sprite_Image == null)
            return;

        refs.Sprite_Image.sprite = sprite;
        refs.Sprite_Image.preserveAspect = true;

        if (setNativeSize && sprite != null && refs.Sprite_Size != null)
        {
            Rect rect = sprite.rect;
            refs.Sprite_Size.sizeDelta = new Vector2(rect.width, rect.height);
        }
    }

    public static void SetVisibleImmediate(
        this SpriteImageRigRefs refs,
        bool visible)
    {
        if (refs?.Sprite_RootCanvasGroup == null)
            return;

        refs.Sprite_RootCanvasGroup.alpha = visible ? 1f : 0f;
    }

    public static void SetImageAlphaImmediate(
        this SpriteImageRigRefs refs,
        float alpha)
    {
        if (refs?.Sprite_Image == null)
            return;

        Color c = refs.Sprite_Image.color;
        c.a = alpha;
        refs.Sprite_Image.color = c;
    }

    public static void SetImageColorImmediate(
        this SpriteImageRigRefs refs,
        Color color)
    {
        if (refs?.Sprite_Image == null)
            return;

        refs.Sprite_Image.color = color;
    }

    public static void ResetToBaselineImmediate(this SpriteImageRigRefs refs)
    {
        if (refs == null)
            return;

        if (refs.Sprite_RootCanvasGroup != null)
        {
            refs.Sprite_RootCanvasGroup.alpha = 0f;
            refs.Sprite_RootCanvasGroup.interactable = false;
            refs.Sprite_RootCanvasGroup.blocksRaycasts = false;
        }

        SetPos(refs.Sprite_Anchor, Vector2.zero);

        SetRotZ(refs.Sprite_BaseRotation, 0f);

        SetPos(refs.Sprite_Track_Move, Vector2.zero);
        SetPos(refs.Sprite_Track_X, Vector2.zero);
        SetPos(refs.Sprite_Track_X_Offset, Vector2.zero);
        SetPos(refs.Sprite_Track_Y, Vector2.zero);
        SetPos(refs.Sprite_Track_Y_Offset, Vector2.zero);

        SetRotZ(refs.Sprite_Rotation, 0f);

        if (refs.Sprite_Size != null)
            refs.Sprite_Size.sizeDelta = Vector2.zero;

        SetScale(refs.Sprite_Scale, Vector3.one);
        SetScale(refs.Sprite_ActingScale, Vector3.one);
        SetScale(refs.Sprite_ActingScale_X, Vector3.one);
        SetScale(refs.Sprite_ActingScale_Y, Vector3.one);

        if (refs.Sprite_Image != null)
        {
            refs.Sprite_Image.color = Color.white;
            refs.Sprite_Image.raycastTarget = false;
            refs.Sprite_Image.preserveAspect = true;
        }
    }

    public static void KillAllTweens(this SpriteImageRigRefs refs, bool complete)
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
}