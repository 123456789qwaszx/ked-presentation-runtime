using UnityEngine;

public sealed partial class PresentationUIRoot : IShotResponseStageProvider
{
    private StageDepthLayerRects[][] _shotResponseStageLayerRects;

    public RectTransform RigSpaceRoot => _stageShotRoot;
    public RectTransform StagePanRoot => _stagePanRoot;
    public RectTransform StageZoomRoot => _stageZoomRoot;

    public StageDepthLayerRects GetLayerRects(
        PresentationStageKey root,
        PresentationDepthLayerKey layer) 
        => _shotResponseStageLayerRects[(int)root][(int)layer];

    private void CacheShotResponseStageProviderRefs()
    {
        _shotResponseStageLayerRects = new StageDepthLayerRects[PresentationStageCount][];

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage00] =
            BuildStage00LayerRects();

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage01] =
            BuildStage01LayerRects();

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage02] =
            BuildStage02LayerRects();
    }

    private StageDepthLayerRects[] BuildStage00LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            _stage00DepthFarRoot,
            _stage00DepthFarFramingTransform,
            _stage00DepthFarFramingScale);

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            _stage00DepthBackRoot,
            _stage00DepthBackFramingTransform,
            _stage00DepthBackFramingScale);

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            _stage00DepthMidRoot,
            _stage00DepthMidFramingTransform,
            _stage00DepthMidFramingScale);

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            _stage00DepthFrontRoot,
            _stage00DepthFrontFramingTransform,
            _stage00DepthFrontFramingScale);

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            _stage00DepthCloseRoot,
            _stage00DepthCloseFramingTransform,
            _stage00DepthCloseFramingScale);

        return rects;
    }

    private StageDepthLayerRects[] BuildStage01LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            _stage01DepthFarRoot,
            _stage01DepthFarFramingTransform,
            _stage01DepthFarFramingScale);

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            _stage01DepthBackRoot,
            _stage01DepthBackFramingTransform,
            _stage01DepthBackFramingScale);

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            _stage01DepthMidRoot,
            _stage01DepthMidFramingTransform,
            _stage01DepthMidFramingScale);

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            _stage01DepthFrontRoot,
            _stage01DepthFrontFramingTransform,
            _stage01DepthFrontFramingScale);

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            _stage01DepthCloseRoot,
            _stage01DepthCloseFramingTransform,
            _stage01DepthCloseFramingScale);

        return rects;
    }

    private StageDepthLayerRects[] BuildStage02LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            _stage02DepthFarRoot,
            _stage02DepthFarFramingTransform,
            _stage02DepthFarFramingScale);

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            _stage02DepthBackRoot,
            _stage02DepthBackFramingTransform,
            _stage02DepthBackFramingScale);

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            _stage02DepthMidRoot,
            _stage02DepthMidFramingTransform,
            _stage02DepthMidFramingScale);

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            _stage02DepthFrontRoot,
            _stage02DepthFrontFramingTransform,
            _stage02DepthFrontFramingScale);

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            _stage02DepthCloseRoot,
            _stage02DepthCloseFramingTransform,
            _stage02DepthCloseFramingScale);

        return rects;
    }
}