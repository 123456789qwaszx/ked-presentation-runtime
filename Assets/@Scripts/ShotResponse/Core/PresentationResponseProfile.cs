using System;
using UnityEngine;

[Serializable]
public sealed class PresentationResponseProfile
{
    [Tooltip("zoom intent 1당 focusPoint 기준으로 대상이 X/Y축별로 밀려나는 거리. " +
             "X(좌우 구도)와 Y(높이/여백)는 서로 완전히 독립적으로 계산됩니다. 0이면 그 축은 퍼지지 않음.")]
    public Vector2 focusSpreadPixelsPerZoom = Vector2.zero;

    [Tooltip("pan 픽셀값을 X/Y축별로 얼마나 따라갈지. X/Y는 서로 독립입니다. " +
             "보통 세로 pan(y)을 가로 pan(x)보다 훨씬 약하게 둡니다.")]
    public Vector2 panResponse = Vector2.zero;

    [Tooltip("zoom intent 1당 대상 localScale이 base 대비 얼마나 커질지. " +
             "1이면 base*(1+zoom)으로 완전히 따라가고, 0이면 스케일 반응 없음. " +
             "스케일은 X/Y 분리 대상이 아니므로(찌그러짐) 단일 배율입니다. 캐릭터는 보통 작게.")]
    public float zoomScaleResponse = 1f;

    // 아래 기본값들은 "튜닝 시작점"이다. 인스펙터에서 조정하는 걸 전제로 한다.
    // 공통 원칙: 가로(X)는 살리고 세로(Y)는 약하게.
    // 캐릭터는 세로로 들썩이면 쉽게 어색해지므로 Y spread를 0에 가깝게,
    // 스케일도 zoom에 살짝만 반응하도록 둔다(zoomScaleResponse를 작게).

    public static PresentationResponseProfile Background => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(0f, 0f),
        panResponse = new Vector2(0.1f, 0.03f),
        zoomScaleResponse = 1f,
    };

    public static PresentationResponseProfile Prop => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(6f, 1.5f),
        panResponse = new Vector2(0.08f, 0.025f),
        zoomScaleResponse = 1f,
    };

    public static PresentationResponseProfile CharacterSlot0 => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.12f, 0.01f),
        zoomScaleResponse = -0.36f,
    };
    
    public static PresentationResponseProfile CharacterSlot1 => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };
    
    public static PresentationResponseProfile DepthFar => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };

    public static PresentationResponseProfile DepthBack => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };

    public static PresentationResponseProfile DepthMid => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };

    public static PresentationResponseProfile DepthFront => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };

    public static PresentationResponseProfile DepthClose => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-3f, -0.1f),
        panResponse = new Vector2(0.2f, 0.01f),
        zoomScaleResponse = -0.36f,
    };
}