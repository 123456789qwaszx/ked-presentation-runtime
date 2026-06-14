using UnityEngine;

// 무대 깊이감(pseudo camera) 레이어 식별자.
// far → close 순으로 카메라 반응이 점점 커짐.
public enum StageDepthLayer
{
    Far,
    Slot1,
    Slot2,
    Slot3,
    Close,
}

// StageDepthLayer 한 칸은 응답 시스템 관점에서 3노드로 구성.
//  Root            : 중립 측정점 (IResponseTarget.MeasureRect)
//  FramingTransform: pan-follow + focus-spread를 anchoredPosition으로 받는 노드 (PositionRect)
//  FramingScale    : zoom을 localScale로 받는 노드 (ScaleRect)
public interface IStageDepthLayerProvider
{
    bool TryGetLayerRects(
        StageDepthLayer layer,
        out RectTransform measure,
        out RectTransform position,
        out RectTransform scale);
}

public sealed partial class PresentationUIRoot : IStageDepthLayerProvider
{
    public bool TryGetLayerRects(
        StageDepthLayer layer,
        out RectTransform measure,
        out RectTransform position,
        out RectTransform scale)
    {
        switch (layer)
        {
            case StageDepthLayer.Far:
                measure  = View.Rect(Refs.Stage00Depth_Far_Root);
                position = View.Rect(Refs.Stage00Depth_Far_FramingTransform);
                scale    = View.Rect(Refs.Stage00Depth_Far_FramingScale);
                break;

            case StageDepthLayer.Slot1:
                measure  = View.Rect(Refs.Stage00Depth_Back_Root);
                position = View.Rect(Refs.Stage00Depth_Back_FramingTransform);
                scale    = View.Rect(Refs.Stage00Depth_Back_FramingScale);
                break;

            case StageDepthLayer.Slot2:
                measure  = View.Rect(Refs.Stage00Depth_Mid_Root);
                position = View.Rect(Refs.Stage00Depth_Mid_FramingTransform);
                scale    = View.Rect(Refs.Stage00Depth_Mid_FramingScale);
                break;

            case StageDepthLayer.Slot3:
                measure  = View.Rect(Refs.Stage00Depth_Front_Root);
                position = View.Rect(Refs.Stage00Depth_Front_FramingTransform);
                scale    = View.Rect(Refs.Stage00Depth_Front_FramingScale);
                break;

            case StageDepthLayer.Close:
                measure  = View.Rect(Refs.Stage00Depth_Close_Root);
                position = View.Rect(Refs.Stage00Depth_Close_FramingTransform);
                scale    = View.Rect(Refs.Stage00Depth_Close_FramingScale);
                break;

            default:
                measure = null;
                position = null;
                scale = null;
                break;
        }

        return measure != null && position != null && scale != null;
    }
}