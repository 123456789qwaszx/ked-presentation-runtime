using UnityEngine;

public sealed class CharacterRigResponseTarget : IResponseTarget
{
    private readonly CharacterRigRefs _refs;

    public RectTransform MeasureRect => _refs.CharSlot_Size;
    public RectTransform PositionRect => _refs.CharSlot_FramingTransform;
    public RectTransform ScaleRect => _refs.CharSlot_FramingScale;

    public CharacterRigResponseTarget(CharacterRigRefs refs)
    {
        _refs = refs;
    }

    public void ApplyResponse(in PresentationTargetResponse response)
    {
        if (PositionRect != null)
            PositionRect.anchoredPosition = response.anchoredPosition;

        if (ScaleRect != null)
            ScaleRect.localScale = new Vector3(1+ response.scale.x * 0.05f, 1 + response.scale.y * 0.05f, 1f);
    }
}