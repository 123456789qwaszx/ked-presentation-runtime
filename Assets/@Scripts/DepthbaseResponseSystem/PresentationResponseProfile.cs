using System;
using UnityEngine;

// 기준 상태, 반응 강도, 역할별 preset
// base 값은 모두 Rig 공간(Stage_Root 기준)에서 캡처된다.
[Serializable]
public sealed class PresentationResponseProfile
{
    [Header("Base Pose (captured in rig space)")]
    public Vector2 basePositionInRigSpace = Vector2.zero;
    public Vector2 baseScale = Vector2.one;
    public float baseAlpha = 1f;

    [Header("Response")]
    [Tooltip("zoom +10 일 때 최대 scale delta. 0.25 = 최대 25% 확대")]
    public float maxZoomScaleDelta = 0.25f;

    [Tooltip("focus 기준 위치 퍼짐 강도. 0이면 퍼지지 않음")]
    public float maxZoomSpreadPixels = 40f;

    [Tooltip("pan 픽셀값을 얼마나 따라갈지. CharacterSlot=1, BG는 낮게")]
    public float panResponse = 1f;

    public static PresentationResponseProfile Background => new PresentationResponseProfile
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        baseAlpha = 1f,
        maxZoomScaleDelta = 0.06f,
        maxZoomSpreadPixels = 0f,
        panResponse = 0.15f,
    };

    public static PresentationResponseProfile Prop => new PresentationResponseProfile
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        baseAlpha = 1f,
        maxZoomScaleDelta = 0.14f,
        maxZoomSpreadPixels = 18f,
        panResponse = 0.55f,
    };

    public static PresentationResponseProfile CharacterSlot => new PresentationResponseProfile
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        baseAlpha = 1f,
        maxZoomScaleDelta = 0.25f,
        maxZoomSpreadPixels = 40f,
        panResponse = 1f,
    };

    public static PresentationResponseProfile Foreground => new PresentationResponseProfile
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        baseAlpha = 1f,
        maxZoomScaleDelta = 0.33f,
        maxZoomSpreadPixels = 64f,
        panResponse = 1.2f,
    };
}
