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