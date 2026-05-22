using System;
using UnityEngine;

[Serializable]
public sealed class PresentationResponseProfile
{
    [Header("Base Pose (captured in rig space)")]
    public Vector2 basePositionInRigSpace = Vector2.zero;
    public Vector2 baseScale = Vector2.one;

    [Header("Response")]
    [Tooltip("zoom +10 일 때 최대 scale delta. 0.25 = 최대 25% 확대")]
    public float maxZoomScaleDelta = 0.25f;

    [Tooltip("focus 기준 위치 퍼짐 강도. 0이면 퍼지지 않음")]
    public float maxZoomSpreadPixels = 40f;

    [Tooltip("pan 픽셀값을 얼마나 따라갈지. CharacterSlot=1, BG는 낮게")]
    public float panResponse = 1f;
    
    public static PresentationResponseProfile Background => new()
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        maxZoomScaleDelta = 0.012f,
        maxZoomSpreadPixels = 0f,
        panResponse = 0.03f,
    };

    public static PresentationResponseProfile Prop => new()
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        maxZoomScaleDelta = 0.03f,
        maxZoomSpreadPixels = 6f,
        panResponse = 0.08f,
    };

    public static PresentationResponseProfile CharacterSlot => new()
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        maxZoomScaleDelta = 0.15f,
        maxZoomSpreadPixels = 10f,
        panResponse = 0.15f,
    };

    public static PresentationResponseProfile Foreground => new()
    {
        basePositionInRigSpace = Vector2.zero,
        baseScale = Vector2.one,
        maxZoomScaleDelta = 0.07f,
        maxZoomSpreadPixels = 16f,
        panResponse = 0.25f,
    };
}