using UnityEngine;

/// <summary>
/// Shot command가 focus point를 얻어오는 대상 계약.
/// 반환값은 Rig 공간(Stage_Root 기준)이어야 한다.
/// </summary>
public interface IPresentationFocusProvider
{
    Vector2 GetFocusPoint();
    bool TryGetFocusRect(out Rect rect);
}

/// <summary>
/// RectTransform 하나를 focus point provider로 노출하는 기본 구현.
/// CharSlotLeftFocus_Root / Center / Right 같은 노드에 붙여 사용한다.
/// </summary>
public sealed class RectTransformFocusProvider : MonoBehaviour, IPresentationFocusProvider
{
    [SerializeField] private RectTransform _focusRect;
    [SerializeField] private PresentationResponseRig _rig;
    [SerializeField] private Vector2 _localOffset;

    private void Reset()
    {
        _focusRect = GetComponent<RectTransform>();
        _rig = GetComponentInParent<PresentationResponseRig>(true);
    }

    public Vector2 GetFocusPoint()
    {
        if (_focusRect == null)
            return Vector2.zero;

        if (_rig == null)
            _rig = GetComponentInParent<PresentationResponseRig>(true);

        Vector3 localPoint = new Vector3(_localOffset.x, _localOffset.y, 0f);
        Vector3 worldPoint = _focusRect.TransformPoint(localPoint);

        if (_rig == null)
            return new Vector2(worldPoint.x, worldPoint.y);

        return _rig.WorldToSpacePoint(worldPoint);
    }

    public bool TryGetFocusRect(out Rect rect)
    {
        rect = Rect.zero;

        if (_focusRect == null)
            return false;

        if (_rig == null)
            _rig = GetComponentInParent<PresentationResponseRig>(true);

        Vector3[] corners = new Vector3[4];
        _focusRect.GetWorldCorners(corners);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 p = _rig != null
                ? _rig.WorldToSpacePoint(corners[i])
                : new Vector2(corners[i].x, corners[i].y);

            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }
}
