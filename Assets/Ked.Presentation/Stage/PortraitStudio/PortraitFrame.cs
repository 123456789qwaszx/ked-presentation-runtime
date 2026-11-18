using System;
using UnityEngine;

/// <summary>
/// 초상의 값 전부 — 카메라가 인물의 어디를 찍나(위 넷)와, 찍은 그림을 대사창의 어디에 얼마나 크게 보이나(아래 둘).
/// 찍는 범위는 리그의 캐스팅 축(CharacterPortrait_VisualOffset) 기준이다 — 중심은 그 캐릭터의 포커스 점
/// (CharacterFocusTuningDB)에 centerOffset을 더한 자리, 크기는 그 노드의 로컬 px.
/// 대본에는 숫자가 없다. 값의 집은 프리셋 에셋 하나다(PortraitFramePresetDBSO — docs/PLAN_portrait-capture.md §3 규칙 5).
/// </summary>
[Serializable]
public struct PortraitFrame
{
    [Tooltip("카메라가 보는 점 — 캐릭터별 포커스 점(focus-tuning) 가운데 어느 것인가.")]
    public CharacterFocusPreset focus;

    [Tooltip("찍는 곳을 옮긴다 — 포커스 점에서 범위의 중심까지(리그 px). Y를 올리면 카메라가 위로 간다. 좌우는 인물의 방향을 따라 뒤집힌다.")]
    public Vector2 centerOffset;

    [Tooltip("찍는 범위(리그 px). 키우면 더 넓게 찍힌다(인물이 작아 보인다). 이 범위의 아래 · 옆 변에서 인물이 잘린다. 칸의 모양도 이 비율이다.")]
    public Vector2 size;

    [Tooltip("텍스처 해상도의 배율. 보통 1 — 칸이 화면에 그려지는 크기에 이미 맞춰져 있다.")]
    public float textureScale;

    [Tooltip("칸을 옮긴다 — 대사창 그림의 왼쪽 아래 귀에서 칸의 왼쪽 아래 귀까지(캔버스 px). X를 줄이면 왼쪽, Y를 올리면 위.")]
    public Vector2 cellOffset;

    [Tooltip("칸의 크기 = 찍는 범위 × 이 값. 찍히는 내용은 그대로 두고 화면에서만 키우고 줄인다.")]
    public float cellScale;

    /// <summary>가슴 위 — 소유자가 예제 대사창에서 맞춘 값이다(2026-11-18, Samples/Settings/PortraitFramePreset.asset과 같다).</summary>
    public static PortraitFrame Default => new()
    {
        focus = CharacterFocusPreset.Face,
        centerOffset = new Vector2(0f, 400f),
        size = new Vector2(800f, 820f),
        textureScale = 1f,
        cellOffset = new Vector2(-160f, 14f),
        cellScale = 0.8f,
    };

    /// <summary>칸의 자리(대사창 그림의 왼쪽 아래 귀 기준)와 크기 — 캔버스 px. 모양은 찍는 범위와 같아 그림이 찌그러지지 않는다.</summary>
    public Rect CellRect => new(cellOffset, size * cellScale);

    /// <summary>인스펙터에서 0으로 남은 값을 기본값으로 메운다 — 범위가 0이면 텍스처를 만들 수 없다.</summary>
    public PortraitFrame Sanitized()
    {
        PortraitFrame frame = this;
        PortraitFrame fallback = Default;

        if (frame.size.x <= 0f || frame.size.y <= 0f)
            frame.size = fallback.size;

        if (frame.textureScale <= 0f)
            frame.textureScale = fallback.textureScale;

        if (frame.cellScale <= 0f)
            frame.cellScale = fallback.cellScale;

        return frame;
    }
}
