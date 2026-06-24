using System;
using UnityEngine;

public enum StageDepthLayer
{
    Far,
    Back,
    Mid,
    Front,
    Close,
}

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
    StageDepthLayerRects GetLayerRects(StageDepthLayer layer);
}

public sealed partial class PresentationUIRoot : IShotResponseStageProvider
{
    public RectTransform RigSpaceRoot => View.Rect(Refs.StageShot_Root);
    public RectTransform StagePanRoot => View.Rect(Refs.StagePan_Root);
    public RectTransform StageZoomRoot => View.Rect(Refs.StageZoom_Root);

    public StageDepthLayerRects GetLayerRects(StageDepthLayer layer)
    {
        switch (layer)
        {
            case StageDepthLayer.Far:
                return new StageDepthLayerRects(
                    View.Rect(Refs.Stage00Depth_Far_Root),
                    View.Rect(Refs.Stage00Depth_Far_FramingTransform),
                    View.Rect(Refs.Stage00Depth_Far_FramingScale));

            case StageDepthLayer.Back:
                return new StageDepthLayerRects(
                    View.Rect(Refs.Stage00Depth_Back_Root),
                    View.Rect(Refs.Stage00Depth_Back_FramingTransform),
                    View.Rect(Refs.Stage00Depth_Back_FramingScale));

            case StageDepthLayer.Mid:
                return new StageDepthLayerRects(
                    View.Rect(Refs.Stage00Depth_Mid_Root),
                    View.Rect(Refs.Stage00Depth_Mid_FramingTransform),
                    View.Rect(Refs.Stage00Depth_Mid_FramingScale));

            case StageDepthLayer.Front:
                return new StageDepthLayerRects(
                    View.Rect(Refs.Stage00Depth_Front_Root),
                    View.Rect(Refs.Stage00Depth_Front_FramingTransform),
                    View.Rect(Refs.Stage00Depth_Front_FramingScale));

            case StageDepthLayer.Close:
                return new StageDepthLayerRects(
                    View.Rect(Refs.Stage00Depth_Close_Root),
                    View.Rect(Refs.Stage00Depth_Close_FramingTransform),
                    View.Rect(Refs.Stage00Depth_Close_FramingScale));

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(layer),
                    layer,
                    "[PresentationUIRoot] Unknown StageDepthLayer.");
        }
    }
}