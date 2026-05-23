using UnityEngine;

public sealed class CharacterFocusAnchorView : MonoBehaviour
{
    [Header("Focus Anchors")]
    [SerializeField] private RectTransform _face;
    [SerializeField] private RectTransform _bust;
    [SerializeField] private RectTransform _body;
    [SerializeField] private RectTransform _feet;
    [SerializeField] private RectTransform _custom1;
    [SerializeField] private RectTransform _custom2;

    public bool TryGetAnchor(CharacterFocusAnchor anchor, out RectTransform rect)
    {
        rect = null;

        switch (anchor)
        {
            case CharacterFocusAnchor.Face:
                rect = _face;
                break;

            case CharacterFocusAnchor.Bust:
                rect = _bust;
                break;

            case CharacterFocusAnchor.Body:
                rect = _body;
                break;

            case CharacterFocusAnchor.Feet:
                rect = _feet;
                break;

            case CharacterFocusAnchor.Custom1:
                rect = _custom1;
                break;

            case CharacterFocusAnchor.Custom2:
                rect = _custom2;
                break;
        }

        return rect != null;
    }
}