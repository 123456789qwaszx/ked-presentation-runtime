using System;
using UnityEngine;

/// <summary>
/// 저작자가 조작하는 샷 의도값.
/// 실제 Transform 수치가 아니라, Rig가 해석할 상태값만 보관한다.
/// </summary>
[Serializable]
public struct PresentationIntentState
{
    [Range(-10f, 10f)] public float zoom;

    /// <summary>
    /// Rig 공간(Stage_Root 기준)에서의 최종 pan 픽셀값.
    /// manual pan + focus reframing 결과가 모두 여기에 합쳐진다.
    /// </summary>
    public Vector2 pan;

    /// <summary>
    /// Rig 공간(Stage_Root 기준)에서의 focus point.
    /// </summary>
    public Vector2 focusPoint;

    public static PresentationIntentState Default => new PresentationIntentState
    {
        zoom = 0f,
        pan = Vector2.zero,
        focusPoint = Vector2.zero,
    };

    public bool IsDefault =>
        Mathf.Approximately(zoom, 0f) &&
        pan == Vector2.zero &&
        focusPoint == Vector2.zero;
}
