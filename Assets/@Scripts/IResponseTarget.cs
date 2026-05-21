using UnityEngine;

public interface IResponseTarget
{
    RectTransform MeasureRect { get; }
    RectTransform PositionRect { get; }
    RectTransform ScaleRect { get; }
    CanvasGroup CanvasGroup { get; }

    void ApplyResponse(in PresentationResponseBinding.Response response);
}