using UnityEngine;

/// <summary>
/// Shot command가 focus point를 얻어오는 대상 계약.
/// 반환값은 world 좌표여야 한다.
/// Rig가 Stage_Root 기준으로 다시 변환한다.
/// </summary>
public interface IPresentationFocusProvider
{
    Vector3 GetFocusWorldPoint();
    bool TryGetFocusWorldRect(out Bounds bounds);
}

/// <summary>
/// RectTransform 하나를 world focus provider로 노출하는 기본 구현.
/// CharSlotLeftFocus_Root / Center / Right 같은 노드에 붙여 사용한다.
/// </summary>
public sealed class RectTransformFocusProvider : MonoBehaviour, IPresentationFocusProvider
{
    [SerializeField] private RectTransform _focusRect;
    [SerializeField] private Vector2 _localOffset;

    private void Reset()
    {
        _focusRect = GetComponent<RectTransform>();
    }

    public Vector3 GetFocusWorldPoint()
    {
        if (_focusRect == null)
            return Vector3.zero;

        return _focusRect.TransformPoint(new Vector3(_localOffset.x, _localOffset.y, 0f));
    }

    public bool TryGetFocusWorldRect(out Bounds bounds)
    {
        bounds = default;

        if (_focusRect == null)
            return false;

        Vector3[] corners = new Vector3[4];
        _focusRect.GetWorldCorners(corners);

        bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(corners[i]);

        return true;
    }
}