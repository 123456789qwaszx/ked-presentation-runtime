using UnityEngine;

public readonly struct StageDepthLayerRects
{
    public readonly RectTransform Measure;
    public readonly RectTransform Position;
    public readonly RectTransform Scale;

    public StageDepthLayerRects(
        RectTransform measure,
        RectTransform position,
        RectTransform scale)
    {
        Measure = measure;
        Position = position;
        Scale = scale;
    }
}

public interface IShotResponseStageProvider
{
    // Shot Response System이 사용하는 공통 계산 좌표계.
    // 캐릭터 focus, screen point, response offset 등은 이 Root의 local space 기준으로 계산된다.
    RectTransform RigSpaceRoot { get; }

    // Shot의 pan 값을 실제 UI 계층에 적용하는 노드.
    // Stage 전체를 반대로 밀어 카메라가 이동한 것처럼 보이게 만든다.
    RectTransform StagePanRoot { get; }

    // Shot의 zoom 값을 실제 UI 계층에 적용하는 노드.
    // Stage 전체를 확대/축소해서 카메라 줌처럼 보이게 만든다.
    RectTransform StageZoomRoot { get; }

    // StageDepthLayer 한 칸은 응답 시스템 관점에서 3노드로 구성.
    //  Root            : 중립 측정점 (IResponseTarget.MeasureRect)
    //  FramingTransform: pan-follow + focus-spread를 anchoredPosition으로 받는 노드 (PositionRect)
    //  FramingScale    : zoom을 localScale로 받는 노드 (ScaleRect)
    
    StageDepthLayerRects GetLayerRects(
        PresentationStageKey root, 
        PresentationDepthLayerKey layer);
}

public sealed partial class PresentationUIRoot : IShotResponseStageProvider
{
    private const int PresentationStageKeyCount = (int)PresentationStageKey.Count;
    private const int PresentationDepthLayerKeyCount = (int)PresentationDepthLayerKey.Count;

    private StageDepthLayerRects[][] _shotResponseStageLayerRects;

    private RectTransform _shotResponseRigSpaceRoot;
    private RectTransform _shotResponseStagePanRoot;
    private RectTransform _shotResponseStageZoomRoot;

    public RectTransform RigSpaceRoot => _shotResponseRigSpaceRoot;
    public RectTransform StagePanRoot => _shotResponseStagePanRoot;
    public RectTransform StageZoomRoot => _shotResponseStageZoomRoot;

    public StageDepthLayerRects GetLayerRects(
        PresentationStageKey root,
        PresentationDepthLayerKey layer) 
        => _shotResponseStageLayerRects[(int)root][(int)layer];
    

    private void CacheShotResponseStageProviderRefs()
    {
        _shotResponseRigSpaceRoot = View.Rect(Refs.StageShot_Root);
        _shotResponseStagePanRoot = View.Rect(Refs.StagePan_Root);
        _shotResponseStageZoomRoot = View.Rect(Refs.StageZoom_Root);

        _shotResponseStageLayerRects = new StageDepthLayerRects[PresentationStageKeyCount][];

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage00] =
            BuildStage00LayerRects();

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage01] =
            BuildStage01LayerRects();

        _shotResponseStageLayerRects[(int)PresentationStageKey.Stage02] =
            BuildStage02LayerRects();
    }

    private StageDepthLayerRects[] BuildStage00LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerKeyCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            View.Rect(Refs.Stage00Depth_Far_Root),
            View.Rect(Refs.Stage00Depth_Far_FramingTransform),
            View.Rect(Refs.Stage00Depth_Far_FramingScale));

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            View.Rect(Refs.Stage00Depth_Back_Root),
            View.Rect(Refs.Stage00Depth_Back_FramingTransform),
            View.Rect(Refs.Stage00Depth_Back_FramingScale));

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            View.Rect(Refs.Stage00Depth_Mid_Root),
            View.Rect(Refs.Stage00Depth_Mid_FramingTransform),
            View.Rect(Refs.Stage00Depth_Mid_FramingScale));

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            View.Rect(Refs.Stage00Depth_Front_Root),
            View.Rect(Refs.Stage00Depth_Front_FramingTransform),
            View.Rect(Refs.Stage00Depth_Front_FramingScale));

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            View.Rect(Refs.Stage00Depth_Close_Root),
            View.Rect(Refs.Stage00Depth_Close_FramingTransform),
            View.Rect(Refs.Stage00Depth_Close_FramingScale));

        return rects;
    }

    private StageDepthLayerRects[] BuildStage01LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerKeyCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            View.Rect(Refs.Stage01Depth_Far_Root),
            View.Rect(Refs.Stage01Depth_Far_FramingTransform),
            View.Rect(Refs.Stage01Depth_Far_FramingScale));

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            View.Rect(Refs.Stage01Depth_Back_Root),
            View.Rect(Refs.Stage01Depth_Back_FramingTransform),
            View.Rect(Refs.Stage01Depth_Back_FramingScale));

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            View.Rect(Refs.Stage01Depth_Mid_Root),
            View.Rect(Refs.Stage01Depth_Mid_FramingTransform),
            View.Rect(Refs.Stage01Depth_Mid_FramingScale));

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            View.Rect(Refs.Stage01Depth_Front_Root),
            View.Rect(Refs.Stage01Depth_Front_FramingTransform),
            View.Rect(Refs.Stage01Depth_Front_FramingScale));

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            View.Rect(Refs.Stage01Depth_Close_Root),
            View.Rect(Refs.Stage01Depth_Close_FramingTransform),
            View.Rect(Refs.Stage01Depth_Close_FramingScale));

        return rects;
    }

    private StageDepthLayerRects[] BuildStage02LayerRects()
    {
        var rects = new StageDepthLayerRects[PresentationDepthLayerKeyCount];

        rects[(int)PresentationDepthLayerKey.Far] = new StageDepthLayerRects(
            View.Rect(Refs.Stage02Depth_Far_Root),
            View.Rect(Refs.Stage02Depth_Far_FramingTransform),
            View.Rect(Refs.Stage02Depth_Far_FramingScale));

        rects[(int)PresentationDepthLayerKey.Back] = new StageDepthLayerRects(
            View.Rect(Refs.Stage02Depth_Back_Root),
            View.Rect(Refs.Stage02Depth_Back_FramingTransform),
            View.Rect(Refs.Stage02Depth_Back_FramingScale));

        rects[(int)PresentationDepthLayerKey.Mid] = new StageDepthLayerRects(
            View.Rect(Refs.Stage02Depth_Mid_Root),
            View.Rect(Refs.Stage02Depth_Mid_FramingTransform),
            View.Rect(Refs.Stage02Depth_Mid_FramingScale));

        rects[(int)PresentationDepthLayerKey.Front] = new StageDepthLayerRects(
            View.Rect(Refs.Stage02Depth_Front_Root),
            View.Rect(Refs.Stage02Depth_Front_FramingTransform),
            View.Rect(Refs.Stage02Depth_Front_FramingScale));

        rects[(int)PresentationDepthLayerKey.Close] = new StageDepthLayerRects(
            View.Rect(Refs.Stage02Depth_Close_Root),
            View.Rect(Refs.Stage02Depth_Close_FramingTransform),
            View.Rect(Refs.Stage02Depth_Close_FramingScale));

        return rects;
    }
}