using System;
using UnityEngine;

/// <summary>
/// 초상 카메라가 보는 범위 — 리그의 캐스팅 축(CharacterPortrait_VisualOffset) 기준이다.
/// 중심은 그 캐릭터의 포커스 점(CharacterFocusTuningDB)에 centerOffset을 더한 자리, 크기는 그 노드의 로컬 px.
/// 대본에는 숫자가 없다 — 구도는 이 값 하나가 정한다(docs/PLAN_portrait-capture.md §3 규칙 5).
/// </summary>
[Serializable]
public struct PortraitFrame
{
    [Tooltip("카메라가 보는 점 — 캐릭터별 포커스 점(focus-tuning) 가운데 어느 것인가.")]
    public CharacterFocusPreset focus;

    [Tooltip("포커스 점에서 범위의 중심까지(리그 로컬 px). 좌우는 인물의 방향을 따라 뒤집힌다.")]
    public Vector2 centerOffset;

    [Tooltip("보는 범위의 크기(리그 로컬 px). 이 범위의 아래 · 옆 변에서 인물이 잘린다.")]
    public Vector2 size;

    [Tooltip("텍스처 해상도 = 범위 × 화면 배율 × 이 값. 초상 칸을 범위보다 크게 그리면(칸의 배율) 그만큼 올린다.")]
    public float textureScale;

    /// <summary>가슴 위 — 머리 위로 여유를 두고 아래는 가슴께에서 자른다. 값은 눈대중이다(Unity에서 맞춘다).</summary>
    public static PortraitFrame Default => new()
    {
        focus = CharacterFocusPreset.Face,
        centerOffset = new Vector2(0f, -60f),
        size = new Vector2(520f, 620f),
        textureScale = 1f,
    };

    /// <summary>인스펙터에서 0으로 남은 값을 기본값으로 메운다 — 범위가 0이면 텍스처를 만들 수 없다.</summary>
    public PortraitFrame Sanitized()
    {
        PortraitFrame frame = this;
        PortraitFrame fallback = Default;

        if (frame.size.x <= 0f || frame.size.y <= 0f)
            frame.size = fallback.size;

        if (frame.textureScale <= 0f)
            frame.textureScale = fallback.textureScale;

        return frame;
    }
}
