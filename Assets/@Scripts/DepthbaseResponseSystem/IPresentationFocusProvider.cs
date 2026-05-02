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