using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 레지스트리 기반으로 살아있는 rig의 표시 Image만 집합에 넣고,
// content 하위를 계층 순서로 훑어 누적 CanvasGroup alpha까지 반영한 entry를 만든다.
public sealed class UIStageDepthLayerSourceCollector
{
    private readonly List<CharacterRigRefs> _characterRigBuffer = new();
    private readonly List<BackgroundRigRefs> _backgroundRigBuffer = new();
    private readonly HashSet<Image> _allowedSourceImages = new();

    public void Collect(
        RectTransform contentRoot,
        CharacterRigRegistry characterRigs,
        BackgroundRigRegistry backgroundRigs,
        List<SourceImageEntry> results)
    {
        results.Clear();
        _allowedSourceImages.Clear();

        if (contentRoot == null)
            return;

        _characterRigBuffer.Clear();
        _backgroundRigBuffer.Clear();

        characterRigs?.CollectAliveRigs(_characterRigBuffer);
        backgroundRigs?.CollectAliveRigs(_backgroundRigBuffer);

        // 해당 depth content 아래에 현재 mount된 살아있는 rig의 "실제 표시 Image"만 집합에 넣는다.
        for (int i = 0; i < _backgroundRigBuffer.Count; i++)
        {
            BackgroundRigRefs refs = _backgroundRigBuffer[i];

            if (refs == null || refs.RigRoot == null || !IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            AddAllowedImage(refs.BackgroundSprite_Image);
        }

        for (int i = 0; i < _characterRigBuffer.Count; i++)
        {
            CharacterRigRefs refs = _characterRigBuffer[i];

            if (refs == null || refs.RigRoot == null || !IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            AddAllowedImage(refs.CharacterPortraitSprite_Image);
            AddAllowedImage(refs.CharacterPortraitSpriteOverlay_Image);

            AddAllowedImage(refs.EmojiSlot00_Image);
            AddAllowedImage(refs.EmojiSlot01_Image);
            AddAllowedImage(refs.EmojiSlot02_Image);
        }

        // content 하위를 계층 순서로 훑어, 집합에 든 Image만 그리기 순서대로 수집한다.
        CollectAllowedImagesInHierarchyOrder(contentRoot, contentRoot, results);
    }

    private void AddAllowedImage(Image image)
    {
        if (image != null)
            _allowedSourceImages.Add(image);
    }

    private void CollectAllowedImagesInHierarchyOrder(
        RectTransform contentRoot,
        Transform current,
        List<SourceImageEntry> results)
    {
        if (current == null)
            return;

        if (current.TryGetComponent(out Image image) && _allowedSourceImages.Contains(image))
        {
            if (TryBuildSourceEntry(contentRoot, image, out SourceImageEntry entry))
                results.Add(entry);
        }

        for (int i = 0; i < current.childCount; i++)
            CollectAllowedImagesInHierarchyOrder(contentRoot, current.GetChild(i), results);
    }

    private static bool TryBuildSourceEntry(RectTransform contentRoot, Image image, out SourceImageEntry entry)
    {
        entry = default;

        if (!IsSourceImageAlive(image))
            return false;

        // content까지 누적된 CanvasGroup alpha를 반영(스프라이트 Root는 초기 alpha 0).
        float canvasGroupAlpha = EvaluateCanvasGroupAlpha(image.transform, contentRoot);

        if (canvasGroupAlpha <= 0.001f)
            return false;

        Color effectiveColor = image.color;
        effectiveColor.a *= canvasGroupAlpha;

        if (effectiveColor.a <= 0.001f)
            return false;

        entry = new SourceImageEntry(image, effectiveColor);
        return true;
    }

    private static float EvaluateCanvasGroupAlpha(Transform leaf, Transform stopRoot)
    {
        float alpha = 1f;
        Transform current = leaf;

        while (current != null)
        {
            if (current.TryGetComponent(out CanvasGroup canvasGroup))
                alpha *= canvasGroup.alpha;

            if (current == stopRoot)
                break;

            current = current.parent;
        }

        return alpha;
    }

    private static bool IsSourceImageAlive(Image source)
    {
        return source != null
            && source.enabled
            && source.gameObject.activeInHierarchy
            && source.sprite != null;
    }

    private static bool IsDescendantOf(RectTransform child, RectTransform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;

        while (t != null)
        {
            if (t == parent)
                return true;

            t = t.parent;
        }

        return false;
    }
}

// 수집된 한 source의 표시 정보(블러 proxy로 복사할 대상).
public readonly struct SourceImageEntry
{
    public readonly Image Image;
    public readonly Color EffectiveColor;

    public SourceImageEntry(Image image, Color effectiveColor)
    {
        Image = image;
        EffectiveColor = effectiveColor;
    }
}