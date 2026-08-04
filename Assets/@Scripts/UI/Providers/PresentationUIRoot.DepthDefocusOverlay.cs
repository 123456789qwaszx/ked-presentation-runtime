using UnityEngine;
using UnityEngine.UI;

public sealed partial class PresentationUIRoot : IPresentationDepthDefocusOverlayProvider
{
    private PresentationDepthDefocusTarget[][] _depthDefocusTargets;

    public void GetDepthDefocusTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target)
        => target = _depthDefocusTargets[(int)stage][(int)layer];

    private void CacheDepthDefocusOverlayProviderRefs()
    {
        _depthDefocusTargets = new PresentationDepthDefocusTarget[PresentationStageCount][];

        _depthDefocusTargets[(int)PresentationStageKey.Stage00] =
            BuildStage00DepthDefocusTargets();

        _depthDefocusTargets[(int)PresentationStageKey.Stage01] =
            BuildStage01DepthDefocusTargets();

        _depthDefocusTargets[(int)PresentationStageKey.Stage02] =
            BuildStage02DepthDefocusTargets();
    }

    private PresentationDepthDefocusTarget[] BuildStage00DepthDefocusTargets()
    {
        var targets = new PresentationDepthDefocusTarget[PresentationDepthLayerCount];

        targets[(int)PresentationDepthLayerKey.Far] = BuildDepthDefocusTarget(
            _stage00DepthFarContent,
            _stage00FarFrostedGlassMask,
            _stage00FarFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Back] = BuildDepthDefocusTarget(
            _stage00DepthBackContent,
            _stage00BackFrostedGlassMask,
            _stage00BackFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Mid] = BuildDepthDefocusTarget(
            _stage00DepthMidContent,
            _stage00MidFrostedGlassMask,
            _stage00MidFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Front] = BuildDepthDefocusTarget(
            _stage00DepthFrontContent,
            _stage00FrontFrostedGlassMask,
            _stage00FrontFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Close] = BuildDepthDefocusTarget(
            _stage00DepthCloseContent,
            _stage00CloseFrostedGlassMask,
            _stage00CloseFrostedGlassRawImage);

        return targets;
    }

    private PresentationDepthDefocusTarget[] BuildStage01DepthDefocusTargets()
    {
        var targets = new PresentationDepthDefocusTarget[PresentationDepthLayerCount];

        targets[(int)PresentationDepthLayerKey.Far] = BuildDepthDefocusTarget(
            _stage01DepthFarContent,
            _stage01FarFrostedGlassMask,
            _stage01FarFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Back] = BuildDepthDefocusTarget(
            _stage01DepthBackContent,
            _stage01BackFrostedGlassMask,
            _stage01BackFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Mid] = BuildDepthDefocusTarget(
            _stage01DepthMidContent,
            _stage01MidFrostedGlassMask,
            _stage01MidFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Front] = BuildDepthDefocusTarget(
            _stage01DepthFrontContent,
            _stage01FrontFrostedGlassMask,
            _stage01FrontFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Close] = BuildDepthDefocusTarget(
            _stage01DepthCloseContent,
            _stage01CloseFrostedGlassMask,
            _stage01CloseFrostedGlassRawImage);

        return targets;
    }

    private PresentationDepthDefocusTarget[] BuildStage02DepthDefocusTargets()
    {
        var targets = new PresentationDepthDefocusTarget[PresentationDepthLayerCount];

        targets[(int)PresentationDepthLayerKey.Far] = BuildDepthDefocusTarget(
            _stage02DepthFarContent,
            _stage02FarFrostedGlassMask,
            _stage02FarFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Back] = BuildDepthDefocusTarget(
            _stage02DepthBackContent,
            _stage02BackFrostedGlassMask,
            _stage02BackFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Mid] = BuildDepthDefocusTarget(
            _stage02DepthMidContent,
            _stage02MidFrostedGlassMask,
            _stage02MidFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Front] = BuildDepthDefocusTarget(
            _stage02DepthFrontContent,
            _stage02FrontFrostedGlassMask,
            _stage02FrontFrostedGlassRawImage);

        targets[(int)PresentationDepthLayerKey.Close] = BuildDepthDefocusTarget(
            _stage02DepthCloseContent,
            _stage02CloseFrostedGlassMask,
            _stage02CloseFrostedGlassRawImage);

        return targets;
    }

    private static PresentationDepthDefocusTarget BuildDepthDefocusTarget(
        RectTransform contentRoot,
        RectTransform maskRoot,
        RawImage rawImage)
    {
        CanvasGroup canvasGroup = maskRoot.GetComponent<CanvasGroup>();

        return new PresentationDepthDefocusTarget(
            contentRoot,
            canvasGroup,
            rawImage);
    }
}