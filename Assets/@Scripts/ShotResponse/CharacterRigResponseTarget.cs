using UnityEngine;

public sealed class CharacterRigResponseTarget : IResponseTarget
{
    private readonly CharacterRigRefs _refs;

    // MeasureRect(CharSlot_Scale)는 placement는 반영하되 framing response(FramingTransform/Scale)는
    // 반영하지 않는 노드여야 한다. 그래야 base가 response에 오염되지 않는다.
    public RectTransform MeasureRect => _refs.CharSlot_Scale;
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
            ScaleRect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);
    }
}