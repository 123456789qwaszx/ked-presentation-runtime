using UnityEngine;

public sealed class CharacterRigResponseTarget : IResponseTarget
{
    private readonly CharacterRigRefs _refs;

    public RectTransform MeasureRect => _refs.CharSlot_Scale;
    public RectTransform PositionRect => _refs.CharSlot_FramingTransform;
    public RectTransform ScaleRect => _refs.CharSlot_FramingScale;
    public CanvasGroup CanvasGroup { get; }

    public CharacterRigResponseTarget(CharacterRigRefs refs, CanvasGroup canvasGroup)
    {
        _refs = refs;
        CanvasGroup = canvasGroup;
    }

    public void ApplyResponse(in PresentationResponseBinding.Response response)
    {
        if (PositionRect != null)
            PositionRect.anchoredPosition = response.anchoredPosition;

        if (ScaleRect != null)
            ScaleRect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);

        if (CanvasGroup != null)
            CanvasGroup.alpha = response.alpha;
    }
}