using System;
using UnityEngine;

[Serializable]
public sealed class PresentationResponseProfile
{
    [Tooltip("zoom intent 1당 focusPoint 기준으로 대상이 바깥쪽으로 밀려나는 거리. 0이면 퍼지지 않음")]
    public float focusSpreadPixelsPerZoom = 40f;

    [Tooltip("pan 픽셀값을 얼마나 따라갈지. CharacterSlot=1, BG는 낮게")]
    public float panResponse = 1f;

    public static PresentationResponseProfile Background => new()
    {
        focusSpreadPixelsPerZoom = 0f,
        panResponse = 0.1f,
    };

    public static PresentationResponseProfile Prop => new()
    {
        focusSpreadPixelsPerZoom = 6f,
        panResponse = 0.08f,
    };

    public static PresentationResponseProfile CharacterSlot => new()
    {
        focusSpreadPixelsPerZoom = 3f,
        panResponse = 0.8f,
    };

    public static PresentationResponseProfile Foreground => new()
    {
        focusSpreadPixelsPerZoom = 16f,
        panResponse = 0.25f,
    };
}