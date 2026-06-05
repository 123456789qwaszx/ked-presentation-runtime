using UnityEngine;

public sealed class BackgroundRigResponseTarget : IResponseTarget
{
    private readonly BackgroundRigRefs _refs;

    public RectTransform MeasureRect => _refs.Background_Root;
    public RectTransform PositionRect => _refs.Background_FramingTransform;
    public RectTransform ScaleRect => _refs.Background_FramingScale;

    public BackgroundRigResponseTarget(BackgroundRigRefs refs)
    {
        _refs = refs;
    }

    public void ApplyResponse(in PresentationTargetResponse response)
    {
        if (PositionRect != null)
            PositionRect.anchoredPosition = response.anchoredPosition;

        if (ScaleRect != null)
            ScaleRect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);
    }
}