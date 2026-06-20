using UnityEngine;
using UnityEngine.UI;

public readonly struct PresentationDepthDefocusTarget
{
    public readonly RectTransform SourceContentRoot;
    public readonly CanvasGroup OverlayCanvasGroup;
    public readonly RawImage OverlayRawImage;

    public bool IsValid =>
        SourceContentRoot != null &&
        OverlayCanvasGroup != null &&
        OverlayRawImage != null;

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
    bool TryGetDepthDefocusTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target);
}