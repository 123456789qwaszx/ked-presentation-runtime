using UnityEngine;
using UnityEngine.UI;

public readonly struct PresentationDepthDefocusTarget
{
    public readonly RectTransform SourceContentRoot;
    public readonly CanvasGroup OverlayCanvasGroup;
    public readonly RawImage OverlayRawImage;

    public PresentationDepthDefocusTarget(
        RectTransform sourceContentRoot,
        CanvasGroup overlayCanvasGroup,
        RawImage overlayRawImage)
    {
        SourceContentRoot = sourceContentRoot;
        OverlayCanvasGroup = overlayCanvasGroup;
        OverlayRawImage = overlayRawImage;
    }
}

public interface IPresentationDepthDefocusOverlayProvider
{
    void GetDepthDefocusTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target);
}

public sealed partial class PresentationUIRoot : IPresentationDepthDefocusOverlayProvider
{
    public void GetDepthDefocusTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target)
    {
        target = default;

        ResolveDepthDefocusRefs(
            stage,
            layer, 
            out Refs contentRef,
            out Refs maskRef,
            out Refs rawImageRef);

        RectTransform contentRoot = View.Rect(contentRef);
        CanvasGroup canvasGroup = View.Rect(maskRef).GetComponent<CanvasGroup>();
        RawImage rawImage = View.Rect(rawImageRef).GetComponent<RawImage>();

        target = new PresentationDepthDefocusTarget(
            contentRoot,
            canvasGroup,
            rawImage);
    }

    private static void ResolveDepthDefocusRefs(
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
                 ResolveStage00DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);
                 return;

            case PresentationStageKey.Stage01:
                 ResolveStage01DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);
                 return;

            case PresentationStageKey.Stage02:
                 ResolveStage02DepthDefocusRefs(layer, out contentRef, out maskRef, out rawImageRef);
                 return;

            default:
                return;
        }
    }

    private static void ResolveStage00DepthDefocusRefs(
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
                return;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage00Depth_Back_Content;
                maskRef = Refs.Stage00BackFrostedGlassMask;
                rawImageRef = Refs.Stage00BackFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage00Depth_Mid_Content;
                maskRef = Refs.Stage00MidFrostedGlassMask;
                rawImageRef = Refs.Stage00MidFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage00Depth_Front_Content;
                maskRef = Refs.Stage00FrontFrostedGlassMask;
                rawImageRef = Refs.Stage00FrontFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage00Depth_Close_Content;
                maskRef = Refs.Stage00CloseFrostedGlassMask;
                rawImageRef = Refs.Stage00CloseFrostedGlassRawImage;
                return;

            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return;
        }
    }

    private static void ResolveStage01DepthDefocusRefs(
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
                return;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage01Depth_Back_Content;
                maskRef = Refs.Stage01BackFrostedGlassMask;
                rawImageRef = Refs.Stage01BackFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage01Depth_Mid_Content;
                maskRef = Refs.Stage01MidFrostedGlassMask;
                rawImageRef = Refs.Stage01MidFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage01Depth_Front_Content;
                maskRef = Refs.Stage01FrontFrostedGlassMask;
                rawImageRef = Refs.Stage01FrontFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage01Depth_Close_Content;
                maskRef = Refs.Stage01CloseFrostedGlassMask;
                rawImageRef = Refs.Stage01CloseFrostedGlassRawImage;
                return;
            
            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return;
        }
    }

    private static void ResolveStage02DepthDefocusRefs(
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
                return;

            case PresentationDepthLayerKey.Back:
                contentRef = Refs.Stage02Depth_Back_Content;
                maskRef = Refs.Stage02BackFrostedGlassMask;
                rawImageRef = Refs.Stage02BackFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Mid:
                contentRef = Refs.Stage02Depth_Mid_Content;
                maskRef = Refs.Stage02MidFrostedGlassMask;
                rawImageRef = Refs.Stage02MidFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Front:
                contentRef = Refs.Stage02Depth_Front_Content;
                maskRef = Refs.Stage02FrontFrostedGlassMask;
                rawImageRef = Refs.Stage02FrontFrostedGlassRawImage;
                return;

            case PresentationDepthLayerKey.Close:
                contentRef = Refs.Stage02Depth_Close_Content;
                maskRef = Refs.Stage02CloseFrostedGlassMask;
                rawImageRef = Refs.Stage02CloseFrostedGlassRawImage;
                return;

            default:
                contentRef = default;
                maskRef = default;
                rawImageRef = default;
                return;
        }
    }
}