using UnityEngine;
using UnityEngine.UI;

public sealed partial class PresentationUIRoot : IPresentationDepthDefocusOverlayProvider
{
    public bool TryGetDepthDefocusTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target)
    {
        target = default;

        if (!TryResolveDepthDefocusRefs(
                stage,
                layer,
                out Refs contentRef,
                out Refs maskRef,
                out Refs rawImageRef))
            return false;

        RectTransform contentRoot = View.Rect(contentRef);
        RectTransform maskRoot = View.Rect(maskRef);
        RectTransform rawImageRoot = View.Rect(rawImageRef);

        if (contentRoot == null || maskRoot == null || rawImageRoot == null)
            return false;

        CanvasGroup canvasGroup = maskRoot.GetComponent<CanvasGroup>();
        RawImage rawImage = rawImageRoot.GetComponent<RawImage>();

        if (canvasGroup == null || rawImage == null)
            return false;

        target = new PresentationDepthDefocusTarget(
            contentRoot,
            canvasGroup,
            rawImage);

        return true;
    }

    private static bool TryResolveDepthDefocusRefs(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out Refs contentRef,
        out Refs maskRef,
        out Refs rawImageRef)
    {
        contentRef = default;
        maskRef = default;
        rawImageRef = default;

        switch (stage)
        {
            case PresentationStageKey.Stage00:
                return TryResolveStage00DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);

            case PresentationStageKey.Stage01:
                return TryResolveStage01DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);

            case PresentationStageKey.Stage02:
                return TryResolveStage02DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);

            default:
                return false;
        }
    }

    private static bool TryResolveStage00DepthDefocusRefs(
        PresentationDepthLayerKey layer,
        out Refs contentRef,
        out Refs maskRef,
        out Refs rawImageRef)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                contentRef = Refs.Stage00Depth_Far_Content;
                maskRef = Refs.Stage00FarFrostedGlassMask;
                rawImageRef = Refs.Stage00FarFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage00Depth_Back_Content;
                maskRef = Refs.Stage00BackFrostedGlassMask;
                rawImageRef = Refs.Stage00BackFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage00Depth_Mid_Content;
                maskRef = Refs.Stage00MidFrostedGlassMask;
                rawImageRef = Refs.Stage00MidFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage00Depth_Front_Content;
                maskRef = Refs.Stage00FrontFrostedGlassMask;
                rawImageRef = Refs.Stage00FrontFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage00Depth_Close_Content;
                maskRef = Refs.Stage00CloseFrostedGlassMask;
                rawImageRef = Refs.Stage00CloseFrostedGlassRawImage;
                return true;

            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return false;
        }
    }

    private static bool TryResolveStage01DepthDefocusRefs(
        PresentationDepthLayerKey layer,
        out Refs contentRef,
        out Refs maskRef,
        out Refs rawImageRef)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                contentRef = Refs.Stage01Depth_Far_Content;
                maskRef = Refs.Stage01FarFrostedGlassMask;
                rawImageRef = Refs.Stage01FarFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage01Depth_Back_Content;
                maskRef = Refs.Stage01BackFrostedGlassMask;
                rawImageRef = Refs.Stage01BackFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage01Depth_Mid_Content;
                maskRef = Refs.Stage01MidFrostedGlassMask;
                rawImageRef = Refs.Stage01MidFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage01Depth_Front_Content;
                maskRef = Refs.Stage01FrontFrostedGlassMask;
                rawImageRef = Refs.Stage01FrontFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage01Depth_Close_Content;
                maskRef = Refs.Stage01CloseFrostedGlassMask;
                rawImageRef = Refs.Stage01CloseFrostedGlassRawImage;
                return true;
            
            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return false;
        }
    }

    private static bool TryResolveStage02DepthDefocusRefs(
        PresentationDepthLayerKey layer,
        out Refs contentRef,
        out Refs maskRef,
        out Refs rawImageRef)
    {
        switch (layer)
        {
            case PresentationDepthLayerKey.Far:
                contentRef = Refs.Stage02Depth_Far_Content;
                maskRef = Refs.Stage02FarFrostedGlassMask;
                rawImageRef = Refs.Stage02FarFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage02Depth_Back_Content;
                maskRef = Refs.Stage02BackFrostedGlassMask;
                rawImageRef = Refs.Stage02BackFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage02Depth_Mid_Content;
                maskRef = Refs.Stage02MidFrostedGlassMask;
                rawImageRef = Refs.Stage02MidFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage02Depth_Front_Content;
                maskRef = Refs.Stage02FrontFrostedGlassMask;
                rawImageRef = Refs.Stage02FrontFrostedGlassRawImage;
                return true;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage02Depth_Close_Content;
                maskRef = Refs.Stage02CloseFrostedGlassMask;
                rawImageRef = Refs.Stage02CloseFrostedGlassRawImage;
                return true;

            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return false;
        }
    }
}