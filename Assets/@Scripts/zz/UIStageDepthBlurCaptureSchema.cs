using System;
using UnityEngine;

public static class UIStageDepthBlurCaptureSchema
{
    public enum Refs
    {
        Slot00_far_Root,
        Slot00_back_Root,
        Slot00_mid_Root,
        Slot00_front_Root,
        Slot00_close_Root,

        Slot01_far_Root,
        Slot01_back_Root,
        Slot01_mid_Root,
        Slot01_front_Root,
        Slot01_close_Root,

        Slot02_far_Root,
        Slot02_back_Root,
        Slot02_mid_Root,
        Slot02_front_Root,
        Slot02_close_Root,
    }

    public sealed class NodeDef
    {
        public Refs Id;
    }

    public static readonly NodeDef[] Nodes =
    {
        new() { Id = Refs.Slot00_far_Root },
        new() { Id = Refs.Slot00_back_Root },
        new() { Id = Refs.Slot00_mid_Root },
        new() { Id = Refs.Slot00_front_Root },
        new() { Id = Refs.Slot00_close_Root },

        new() { Id = Refs.Slot01_far_Root },
        new() { Id = Refs.Slot01_back_Root },
        new() { Id = Refs.Slot01_mid_Root },
        new() { Id = Refs.Slot01_front_Root },
        new() { Id = Refs.Slot01_close_Root },

        new() { Id = Refs.Slot02_far_Root },
        new() { Id = Refs.Slot02_back_Root },
        new() { Id = Refs.Slot02_mid_Root },
        new() { Id = Refs.Slot02_front_Root },
        new() { Id = Refs.Slot02_close_Root },
    };
}

public sealed class UIStageDepthBlurCaptureRefs
{
    public RectTransform Slot00_far_Root;
    public RectTransform Slot00_back_Root;
    public RectTransform Slot00_mid_Root;
    public RectTransform Slot00_front_Root;
    public RectTransform Slot00_close_Root;

    public RectTransform Slot01_far_Root;
    public RectTransform Slot01_back_Root;
    public RectTransform Slot01_mid_Root;
    public RectTransform Slot01_front_Root;
    public RectTransform Slot01_close_Root;

    public RectTransform Slot02_far_Root;
    public RectTransform Slot02_back_Root;
    public RectTransform Slot02_mid_Root;
    public RectTransform Slot02_front_Root;
    public RectTransform Slot02_close_Root;

    public bool TryGetRoot(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform root)
    {
        root = null;

        switch (stage)
        {
            case PresentationStageKey.Stage00:
                return TryGetSlot00Root(layer, out root);

            case PresentationStageKey.Stage01:
                return TryGetSlot01Root(layer, out root);

            case PresentationStageKey.Stage02:
                return TryGetSlot02Root(layer, out root);

            default:
                return false;
        }
    }

    private bool TryGetSlot00Root(
        PresentationDepthLayerKey layer,
        out RectTransform root)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                root = Slot00_far_Root;
                return root != null;

            case PresentationDepthLayerKey.Back:
                root = Slot00_back_Root;
                return root != null;

            case PresentationDepthLayerKey.Mid:
                root = Slot00_mid_Root;
                return root != null;

            case PresentationDepthLayerKey.Front:
                root = Slot00_front_Root;
                return root != null;

            case PresentationDepthLayerKey.Close:
                root = Slot00_close_Root;
                return root != null;

            default:
                root = null;
                return false;
        }
    }

    private bool TryGetSlot01Root(
        PresentationDepthLayerKey layer,
        out RectTransform root)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                root = Slot01_far_Root;
                return root != null;

            case PresentationDepthLayerKey.Back:
                root = Slot01_back_Root;
                return root != null;

            case PresentationDepthLayerKey.Mid:
                root = Slot01_mid_Root;
                return root != null;

            case PresentationDepthLayerKey.Front:
                root = Slot01_front_Root;
                return root != null;

            case PresentationDepthLayerKey.Close:
                root = Slot01_close_Root;
                return root != null;

            default:
                root = null;
                return false;
        }
    }

    private bool TryGetSlot02Root(
        PresentationDepthLayerKey layer,
        out RectTransform root)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                root = Slot02_far_Root;
                return root != null;

            case PresentationDepthLayerKey.Back:
                root = Slot02_back_Root;
                return root != null;

            case PresentationDepthLayerKey.Mid:
                root = Slot02_mid_Root;
                return root != null;

            case PresentationDepthLayerKey.Front:
                root = Slot02_front_Root;
                return root != null;

            case PresentationDepthLayerKey.Close:
                root = Slot02_close_Root;
                return root != null;

            default:
                root = null;
                return false;
        }
    }
}