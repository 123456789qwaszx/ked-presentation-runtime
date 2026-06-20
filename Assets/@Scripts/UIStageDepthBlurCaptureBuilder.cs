using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIStageDepthBlurCaptureBuilder
{
    public void EnsureAndBind(
        RectTransform captureRoot,
        out UIStageDepthBlurCaptureRefs refs)
    {
        EnsureGraph(captureRoot);
        BindRefsFromRoot(captureRoot, out refs);
    }

    private void EnsureGraph(RectTransform captureRoot)
    {
        if (captureRoot == null)
            return;

        for (int i = 0; i < UIStageDepthBlurCaptureSchema.Nodes.Length; i++)
        {
            UIStageDepthBlurCaptureSchema.NodeDef node = UIStageDepthBlurCaptureSchema.Nodes[i];

            RectTransform rt = EnsureRect(
                captureRoot,
                node.Id.ToString());

            rt.SetSiblingIndex(i);
        }
    }

    private void BindRefsFromRoot(
        RectTransform captureRoot,
        out UIStageDepthBlurCaptureRefs refs)
    {
        refs = new UIStageDepthBlurCaptureRefs();

        Dictionary<UIStageDepthBlurCaptureSchema.Refs, RectTransform> map = new();

        foreach (UIStageDepthBlurCaptureSchema.Refs id in Enum.GetValues(typeof(UIStageDepthBlurCaptureSchema.Refs)))
        {
            RectTransform rt = FindByName(captureRoot, id.ToString()) as RectTransform;

            if (rt != null)
                map[id] = rt;
        }

        RectTransform GetRt(UIStageDepthBlurCaptureSchema.Refs key)
        {
            if (!map.TryGetValue(key, out RectTransform rt) || rt == null)
            {
                Debug.LogWarning($"[UIStageDepthBlurCaptureBuilder] Missing capture ref '{key}'.");
                return null;
            }

            return rt;
        }

        refs.Slot00_far_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot00_far_Root);
        refs.Slot00_back_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot00_back_Root);
        refs.Slot00_mid_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot00_mid_Root);
        refs.Slot00_front_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot00_front_Root);
        refs.Slot00_close_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot00_close_Root);

        refs.Slot01_far_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot01_far_Root);
        refs.Slot01_back_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot01_back_Root);
        refs.Slot01_mid_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot01_mid_Root);
        refs.Slot01_front_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot01_front_Root);
        refs.Slot01_close_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot01_close_Root);

        refs.Slot02_far_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot02_far_Root);
        refs.Slot02_back_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot02_back_Root);
        refs.Slot02_mid_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot02_mid_Root);
        refs.Slot02_front_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot02_front_Root);
        refs.Slot02_close_Root = GetRt(UIStageDepthBlurCaptureSchema.Refs.Slot02_close_Root);
    }

    public Image EnsureProxyImage(
        RectTransform layerRoot,
        string imageName)
    {
        if (layerRoot == null)
            return null;

        RectTransform existing = FindByName(layerRoot, imageName) as RectTransform;

        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();

            if (existingImage == null)
                existingImage = existing.gameObject.AddComponent<Image>();

            existingImage.raycastTarget = false;
            existingImage.enabled = false;
            return existingImage;
        }

        GameObject go = new GameObject(imageName, typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(layerRoot, false);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;

        StretchFull(rt);

        return image;
    }

    private static RectTransform EnsureRect(
        RectTransform parent,
        string name)
    {
        RectTransform existing = FindByName(parent, name) as RectTransform;

        if (existing != null)
            return existing;

        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        StretchFull(rt);

        return rt;
    }

    private static Transform FindByName(
        Transform root,
        string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindByName(child, name);

            if (found != null)
                return found;
        }

        return null;
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