using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIStageDepthBlurCaptureRefs
{
    private readonly Dictionary<string, RectTransform> _roots = new();

    public void SetRoot(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        RectTransform root)
        => _roots[MakeKey(stage, layer)] = root;

    public bool TryGetRoot(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform root)
        => _roots.TryGetValue(MakeKey(stage, layer), out root) && root != null;
    

    private static string MakeKey(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer) 
        => $"{stage}_{layer}";
}

public sealed class UIStageDepthBlurCaptureBuilder
{
    private static readonly PresentationStageKey[] Stages =
    {
        PresentationStageKey.Stage00,
        PresentationStageKey.Stage01,
        PresentationStageKey.Stage02,
    };

    private static readonly PresentationDepthLayerKey[] Layers =
    {
        PresentationDepthLayerKey.Far,
        PresentationDepthLayerKey.Back,
        PresentationDepthLayerKey.Mid,
        PresentationDepthLayerKey.Front,
        PresentationDepthLayerKey.Close,
    };

    public void EnsureAndBind(RectTransform captureRoot, out UIStageDepthBlurCaptureRefs refs)
    {
        refs = new UIStageDepthBlurCaptureRefs();
        
        int siblingIndex = 0;

        for (int stageIndex = 0; stageIndex < Stages.Length; stageIndex++)
        {
            PresentationStageKey stage = Stages[stageIndex];

            for (int layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
            {
                PresentationDepthLayerKey layer = Layers[layerIndex];

                RectTransform root = EnsureRect(captureRoot, MakeRootName(stageIndex, layer));

                root.SetSiblingIndex(siblingIndex);
                refs.SetRoot(stage, layer, root);

                siblingIndex++;
            }
        }
    }

    public Image EnsureProxyImage(RectTransform layerRoot, string imageName)
    {
        RectTransform existing = FindDirectChildByName(layerRoot, imageName) as RectTransform;

        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();

            if (existingImage == null)
                existingImage = existing.gameObject.AddComponent<Image>();

            existingImage.raycastTarget = false;
            existingImage.enabled = false;

            StretchFull(existing);

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

    private static RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform existing = FindDirectChildByName(parent, name) as RectTransform;

        if (existing != null)
        {
            StretchFull(existing);
            return existing;
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        StretchFull(rt);

        return rt;
    }

    private static string MakeRootName(int stageIndex, PresentationDepthLayerKey layer)
    {
        return $"Slot{stageIndex:00}_{ToLayerName(layer)}_Root";
    }

    private static string ToLayerName(PresentationDepthLayerKey layer)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                return "far";

            case PresentationDepthLayerKey.Back:
                return "back";

            case PresentationDepthLayerKey.Mid:
                return "mid";

            case PresentationDepthLayerKey.Front:
                return "front";

            case PresentationDepthLayerKey.Close:
                return "close";

            default:
                return layer.ToString();
        }
    }

    private static Transform FindDirectChildByName(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return child;
        }

        return null;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}