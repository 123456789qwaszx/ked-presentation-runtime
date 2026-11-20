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

    /// <summary>
    /// 판 그림의 해상도 — 캔버스 1px에 그림 몇 px인가(CutInPanelGenerator가 이 값으로 그린다).
    /// 판은 배경처럼 원본 픽셀 크기로 서는 리그라, 파서가 판 리그의 배율에 1/이 값을 곱한다(docs/PLAN_stage-visibility.md §3.2).
    /// </summary>
    public const int PanelPictureResolution = 2;

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

    [Tooltip("판 아래 외곽선의 색 — 판 그림의 외곽선 색이다(생성기가 이 색으로 그린다). 판도 마스크 안이라 아래 외곽선 띠가 같이 잘리므로, 마스크 테두리가 이 색 · maskInsetPixels 두께로 다시 그린다.")]
    public Color outlineColor = new(34f / 255f, 30f / 255f, 38f / 255f, 1f);

    [Tooltip("양옆도 판 모서리 선으로 자르나. 끄면 아래 변의 연장선 아래만 자른다(원본: 머리 · 옆머리가 판 밖으로 나온다).")]
    public bool clipSides;

    [Tooltip("열린 쪽(위 · 옆)을 화면 밖까지 늘리는 길이(px).")]
    public float maskReachPixels = 6000f;

    [Header("Motion (창 — 배율 · 회전, relative)")]
    public MotionClip popIn = DefaultPopIn();
    public MotionClip popOut = DefaultPopOut();

    [Header("Flash open — cutin_flash_in (흰 선 → 흰 판 → 걷힘)")]
    [Tooltip("① 흰 선이 판 가운데 높이를 가로질러 자라는 길이(초). 원본 30fps 실측 4 ~ 5프레임.")]
    [Min(0f)] public float flashLineSeconds = 0.15f;

    [Tooltip("③ 온통 흰 판으로 머무는 길이(초). 실측 2 ~ 3프레임. 선이 판이 되는 것(②)은 한 프레임이다 — 길이가 없다.")]
    [Min(0f)] public float flashHoldSeconds = 0.08f;

    [Tooltip("④ 흰빛이 걷히는 길이(초) — 흰 정도가 1 → 0으로 고르게. 실측 8 ~ 10프레임.")]
    [Min(0f)] public float flashClearSeconds = 0.3f;

    [Tooltip("흰 덮개의 색.")]
    public Color flashColor = Color.white;

    [Tooltip("흰 선이 다 자랐을 때의 굵기(px — 대본의 배율이 곱해진다). 자라는 동안 이 값의 30%에서 시작한다. 눈으로 읽은 어림이다.")]
    [Min(0f)] public float flashLineThickness = 10f;

    [Header("Fade close — cutin_fade_out")]
    [Tooltip("판과 그 무대의 내용이 함께 투명해지는 길이(초). 실측 0.2 ~ 0.6초.")]
    [Min(0f)] public float fadeOutSeconds = 0.3f;

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
    /// 판 아래 외곽선 띠의 가운데 선(무대 중심 기준 px) — 판 바깥 아래 변과 마스크 아래 변(외곽선 안쪽 maskInsetPixels) 사이.
    /// maskInsetPixels 두께로 그리면 마스크가 자른 외곽선 띠를 그대로 덮는다. 판 너비만큼만이다(마스크 아래 변은 화면 끝까지 뻗는다).
    /// </summary>
    public void GetOutlineSegment(Vector2 center, float scale, out Vector2 a, out Vector2 b)
    {
        GetCorners(center, scale, out Vector2 bl, out _, out _, out Vector2 br);

        Vector2 along = (br - bl).normalized;
        Vector2 halfInset = new Vector2(-along.y, along.x) * (maskInsetPixels * 0.5f);

        a = bl + halfInset;
        b = br + halfInset;
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

    // ── 흰빛 열림(cutin_flash_in) — 30fps 실측(docs/PLAN_cutin.md §5.1) ────────────

    /// <summary>흰빛 열림의 전체 길이 — 선 + 머묾 + 걷힘.</summary>
    public float FlashDuration =>
        Mathf.Max(0f, flashLineSeconds) + Mathf.Max(0f, flashHoldSeconds) + Mathf.Max(0f, flashClearSeconds);

    /// <summary>
    /// 흰빛 열림의 그 시각 — 선인가 판인가, 선이면 얼마나 자랐나(0 ~ 1), 판이면 얼마나 흰가(1 ~ 0).
    /// 선(①)이 끝나는 순간 판(②)이다 — 그때부터 카드는 다 선 크기 · 제자리이고 흰 정도만 내려간다.
    /// </summary>
    public CutInFlashFrame EvaluateFlash(float seconds)
    {
        float line = Mathf.Max(0f, flashLineSeconds);
        float hold = Mathf.Max(0f, flashHoldSeconds);
        float clear = Mathf.Max(0f, flashClearSeconds);

        if (seconds < line)
            return new CutInFlashFrame(false, Mathf.Clamp01(seconds / line), 1f);

        float sinceClear = seconds - line - hold;

        if (sinceClear <= 0f)
            return new CutInFlashFrame(true, 1f, 1f);

        return new CutInFlashFrame(true, 1f, clear > 0f ? 1f - Mathf.Clamp01(sinceClear / clear) : 0f);
    }

    /// <summary>
    /// 흰 선의 네 점(무대 중심 기준 px) — 판의 왼쪽 변 가운데에서 오른쪽 변 가운데로 가는 선 위에서, 왼쪽부터 progress만큼 자란 띠.
    /// 판의 기울기를 따른다. 굵기는 자라며 flashLineThickness의 30% → 100%. 순서는 마스크의 사각형 규약.
    /// </summary>
    public void GetFlashLineQuad(Vector2 center, float scale, float progress, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
    {
        GetCorners(center, scale, out Vector2 bl, out Vector2 tl, out Vector2 tr, out Vector2 br);

        progress = Mathf.Clamp01(progress);

        Vector2 left = (bl + tl) * 0.5f;
        Vector2 right = (br + tr) * 0.5f;
        Vector2 end = Vector2.LerpUnclamped(left, right, progress);

        Vector2 along = (right - left).normalized;
        Vector2 up = new(-along.y, along.x);
        Vector2 half = up * (Mathf.Max(0f, flashLineThickness) * scale * Mathf.Lerp(0.3f, 1f, progress) * 0.5f);

        p0 = left - half;
        p1 = left + half;
        p2 = end + half;
        p3 = end - half;
    }

    // ── 기본 모션 — GIF 실측(docs/PLAN_cutin.md §1) ─────────────────────────────

    /// <summary>등장: 0.45배 · −6.7°에서 튀어 1.19배를 지나 1배 · 0°로. 0.42초, commit.</summary>
    public static MotionClip DefaultPopIn()
        => WindowClip(
            "cutin_pop_in",
            new[] { 0f, 0.07f, 0.14f, 0.21f, 0.28f, 0.35f, 0.42f },
            new[] { 0.45f, 1.10f, 1.19f, 1.05f, 0.98f, 1.00f, 1.00f },
            new[] { -6.7f, 0.2f, 3.6f, 4.7f, 0.2f, -1.9f, 0f });

    /// <summary>퇴장: 살짝 커지며 기울었다가(1.17배 · +7.5°) 0으로. 0.35초, commit — 끝에서 슬롯이 창을 항등으로 돌리고 마스크로 숨긴다.</summary>
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

/// <summary>흰빛 열림(cutin_flash_in)의 한 순간 — CutInPresetDBSO.EvaluateFlash.</summary>
public readonly struct CutInFlashFrame
{
    /// <summary>흰 판인가(참) 아직 흰 선인가(거짓). 판이 되는 순간부터 카드(마스크 · 판 · 사람)가 선다.</summary>
    public readonly bool IsPlate;

    /// <summary>흰 선이 자란 정도 0 ~ 1. 판이면 1.</summary>
    public readonly float LineProgress;

    /// <summary>흰 정도 1(온통 흰) ~ 0(다 드러남). 선일 때는 1.</summary>
    public readonly float Whiteness;

    public CutInFlashFrame(bool isPlate, float lineProgress, float whiteness)
    {
        IsPlate = isPlate;
        LineProgress = lineProgress;
        Whiteness = whiteness;
    }
}
