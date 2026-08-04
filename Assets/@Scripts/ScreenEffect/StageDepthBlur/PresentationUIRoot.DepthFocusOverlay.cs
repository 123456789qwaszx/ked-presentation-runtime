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