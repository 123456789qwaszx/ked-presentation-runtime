using System;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 컷인 프리셋 — 게임마다 하나. 판의 모양 · 기본 크기와 등장 · 퇴장 모션(docs/PLAN_cutin.md §4).
/// 자리는 대본이 정한다(<<cutin_open 무대 판 x y [배율]>>). 런타임에서 편집하는 값이다.
///
/// 기본값은 원본 게임 실측이다(2026-11-09, 아미야가 녹화한 GIF — 화면 전체 1327×613 = 영상 1920×886을 줄인 것,
/// 캔버스는 높이 기준 2340×1080, 1 GIF px = 1.762 캔버스 px):
///   판 바깥 사각형 312×171 GIF px → 550×301 캔버스 px. 모서리 비율은 그 사각형 안의 (x, y-위로).
///   모션 키는 GIF 프레임(약 70ms 간격)에서 잰 카드 배율 · 회전이다 — 거칠다. 원본 영상에서 다시 재면 바꾼다.
/// 판 그림(Resources/CutInPanels/*)은 이 판 크기 · 모서리 비율 · 외곽선(maskInsetPixels)으로 그린다 —
/// 메뉴 Ked › Cut-in › 판 그림 만들기(CutInPanelGenerator). 값을 바꾸면 그 메뉴로 다시 그린다.
/// </summary>
[CreateAssetMenu(menuName = "Ked/Presentation/Cut-in Preset", fileName = "CutInPreset")]
public sealed class CutInPresetDBSO : ScriptableObject
{
    /// <summary>판 그림의 Resources 뿌리. 대본의 판 키 = 이 아래 스프라이트 이름.</summary>
    public const string PanelResourcesRoot = "CutInPanels";

    /// <summary>창 모션 클립의 리그 이름 — 캐릭터 리그 모션(motions.json)과 섞이지 않게.</summary>
    public const string WindowRig = "stageWindow";
    public const string WindowNode = "Window";

    [Header("Panel (캔버스 기준 px)")]
    [Tooltip("판 바깥 사각형의 기본 크기. 대본의 배율이 곱해진다.")]
    public Vector2 panelSize = new(550f, 301f);

    [Tooltip("판 네 모서리 — 바깥 사각형에 대한 비율 (x, y-위로). 비튼 사각형이다.")]
    public Vector2 cornerBottomLeft = new(0f, 0.012f);
    public Vector2 cornerTopLeft = new(0.029f, 1f);
    public Vector2 cornerTopRight = new(0.965f, 0.918f);
    public Vector2 cornerBottomRight = new(1f, 0f);

    [Header("Mask")]
    [Tooltip("아래 변을 판 외곽선 안쪽으로 올리는 양(px). 인물이 외곽선을 덮지 않게 — 그림의 외곽선 두께와 같다.")]
    public float maskInsetPixels = 3.5f;

    [Tooltip("양옆도 판 모서리 선으로 자르나. 끄면 아래 변의 연장선 아래만 자른다(원본: 머리 · 옆머리가 판 밖으로 나온다).")]
    public bool clipSides;

    [Tooltip("열린 쪽(위 · 옆)을 화면 밖까지 늘리는 길이(px).")]
    public float maskReachPixels = 6000f;

    [Header("Motion (창 — 배율 · 회전, relative)")]
    public MotionClip popIn = DefaultPopIn();
    public MotionClip popOut = DefaultPopOut();

    /// <summary>재생에 쓰는 등장 클립 — 직렬화가 채운 빈 성분을 "성분 없음"으로 읽은 것(ReadClip).</summary>
    public MotionClip PopIn => ReadClip(popIn);

    /// <summary>재생에 쓰는 퇴장 클립 — ReadClip.</summary>
    public MotionClip PopOut => ReadClip(popOut);

    /// <summary>
    /// 에셋에서 읽은 클립을 규칙(MotionRules)이 보는 모양으로 — 유니티 직렬화는 null 배열을 **빈 배열**로 저장한다.
    /// 그러면 배율 채널의 Z가 빈 배열로 돌아와 "Scale에는 Z 성분이 없다"에 걸린다(2026-11-09 실제로 났다).
    /// motions.json 로더(MotionLibrary.ToKeys)와 같은 규칙: 빈 것은 성분 없음(null)이다. 원본 필드는 고치지 않는다.
    /// </summary>
    public static MotionClip ReadClip(MotionClip clip)
    {
        if (clip == null)
            return null;

        MotionChannel[] source = clip.Channels ?? Array.Empty<MotionChannel>();
        MotionChannel[] channels = new MotionChannel[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            channels[i] = new MotionChannel
            {
                Node = source[i].Node,
                Kind = source[i].Kind,
                X = source[i].X is { Length: > 0 } ? source[i].X : null,
                Y = source[i].Y is { Length: > 0 } ? source[i].Y : null,
                Z = source[i].Z is { Length: > 0 } ? source[i].Z : null,
            };
        }

        return new MotionClip { Name = clip.Name, Rig = clip.Rig, Duration = clip.Duration, End = clip.End, Channels = channels };
    }

    /// <summary>에셋이 없을 때의 기본값 — 조립이 경고하고 쓴다.</summary>
    public static CutInPresetDBSO CreateDefault()
    {
        CutInPresetDBSO preset = CreateInstance<CutInPresetDBSO>();
        preset.name = "CutInPreset (코드 기본값)";
        preset.hideFlags = HideFlags.DontSave;
        return preset;
    }

    /// <summary>판 네 모서리(무대 중심 기준 px, y 위). center는 판 바깥 사각형의 중심.</summary>
    public void GetCorners(Vector2 center, float scale, out Vector2 bl, out Vector2 tl, out Vector2 tr, out Vector2 br)
    {
        Vector2 size = panelSize * scale;
        Vector2 origin = center - size * 0.5f;

        bl = origin + Vector2.Scale(cornerBottomLeft, size);
        tl = origin + Vector2.Scale(cornerTopLeft, size);
        tr = origin + Vector2.Scale(cornerTopRight, size);
        br = origin + Vector2.Scale(cornerBottomRight, size);
    }

    /// <summary>
    /// 마스크 네 점(무대 중심 기준 px) — 판 아래 변(외곽선 안쪽)으로 자르고 위는 화면 밖까지 연다.
    /// 순서는 StageMaskGraphic의 사각형 규약(왼아래 · 왼위 · 오른위 · 오른아래).
    /// </summary>
    public void GetMaskQuad(Vector2 center, float scale, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
    {
        GetCorners(center, scale, out Vector2 bl, out Vector2 tl, out Vector2 tr, out Vector2 br);

        Vector2 along = (br - bl).normalized;
        Vector2 up = new(-along.y, along.x);                 // 아래 변의 왼쪽 법선 = 판 안쪽(위)
        Vector2 inset = up * maskInsetPixels;
        float reach = Mathf.Max(0f, maskReachPixels);

        if (clipSides)
        {
            // 옆 변을 따라 위로 연다.
            p0 = bl + inset;
            p3 = br + inset;
            p1 = p0 + (tl - bl).normalized * reach;
            p2 = p3 + (tr - br).normalized * reach;
            return;
        }

        // 아래 변의 연장선 위가 전부 보인다.
        p0 = bl + inset - along * reach;
        p3 = br + inset + along * reach;
        p1 = p0 + up * reach;
        p2 = p3 + up * reach;
    }

    // ── 기본 모션 — GIF 실측(docs/PLAN_cutin.md §1) ─────────────────────────────

    /// <summary>등장: 0.45배 · −6.7°에서 튀어 1.19배를 지나 1배 · 0°로. 0.42초, commit.</summary>
    public static MotionClip DefaultPopIn()
        => WindowClip(
            "cutin_pop_in",
            new[] { 0f, 0.07f, 0.14f, 0.21f, 0.28f, 0.35f, 0.42f },
            new[] { 0.45f, 1.10f, 1.19f, 1.05f, 0.98f, 1.00f, 1.00f },
            new[] { -6.7f, 0.2f, 3.6f, 4.7f, 0.2f, -1.9f, 0f });

    /// <summary>퇴장: 살짝 커지며 기울었다가(1.17배 · +7.5°) 0으로. 0.35초, commit — 창은 배율 0으로 남는다.</summary>
    public static MotionClip DefaultPopOut()
        => WindowClip(
            "cutin_pop_out",
            new[] { 0f, 0.07f, 0.14f, 0.21f, 0.28f, 0.35f },
            new[] { 1.00f, 1.04f, 1.16f, 1.17f, 0.60f, 0f },
            new[] { 0f, 1.7f, 6.2f, 7.5f, -0.9f, 0f });

    private static MotionClip WindowClip(string name, float[] times, float[] scale, float[] rotationZ)
    {
        CurveKey[] scaleKeys = PresetCurveKeys.Smooth(times, scale);

        return new MotionClip
        {
            Name = name,
            Rig = WindowRig,
            Duration = times[times.Length - 1],
            End = MotionEnd.Commit,
            Channels = new[]
            {
                new MotionChannel { Node = WindowNode, Kind = MotionChannelKind.Scale, X = scaleKeys, Y = (CurveKey[])scaleKeys.Clone() },
                new MotionChannel { Node = WindowNode, Kind = MotionChannelKind.Euler, Z = PresetCurveKeys.Smooth(times, rotationZ) },
            },
        };
    }
}
