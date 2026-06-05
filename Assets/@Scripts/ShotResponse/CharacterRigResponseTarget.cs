using UnityEngine;

public sealed class CharacterRigResponseTarget : IResponseTarget
{
    [SerializeField] private bool _logResponsePosition = true;
    [SerializeField] private float _responsePositionLogInterval = 0.5f;

    private float _nextResponsePositionLogTime;
    
    private readonly CharacterRigRefs _refs;

    public RectTransform MeasureRect => _refs.CharSlot_Scale;
    public RectTransform PositionRect => _refs.CharSlot_FramingTransform;
    public RectTransform ScaleRect => _refs.CharSlot_FramingScale;

    public CharacterRigResponseTarget(CharacterRigRefs refs)
    {
        _refs = refs;
    }

    public void ApplyResponse(in PresentationTargetResponse response)
    {
        Vector2 beforePosition = PositionRect != null
            ? PositionRect.anchoredPosition
            : Vector2.zero;

        if (PositionRect != null)
            PositionRect.anchoredPosition = response.anchoredPosition;

        if (ScaleRect != null)
            ScaleRect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);

        if (_logResponsePosition && Time.unscaledTime >= _nextResponsePositionLogTime)
        {
            Vector2 afterPosition = PositionRect != null
                ? PositionRect.anchoredPosition
                : Vector2.zero;

            Debug.Log(
                $"[{nameof(ApplyResponse)}] " +
                $"response.anchoredPosition={response.anchoredPosition}, " +
                $"PositionRect.before={beforePosition}, " +
                $"PositionRect.after={afterPosition}");

            _nextResponsePositionLogTime = Time.unscaledTime + _responsePositionLogInterval;
        }
    }
}