using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무대 하나의 컷인 창 · 판 — docs/PLAN_cutin.md.
///
///   {stage}_Window        무대 루트의 부모. 컷인 카드(판 + 무대 내용)를 한 덩어리로 들고 팝 · 회전한다
///     {stage}_Panel       판 그림. 무대 루트보다 뒤
///     {stage}_Stage_Root  (그대로) — 마스크가 판 아래 변으로만 자른다(위는 열림)
///
/// 상태는 셋이다. 중립(창 항등 · 판 꺼짐 — 컷인을 안 쓰는 무대), 열림(Layout 뒤), 닫힘(창 배율 0 — 퇴장 모션 끝).
/// 창 모션(배율 · 회전 — 캐릭터 motion과 같은 식, MotionMath.Compose relative)은 이 슬롯이 하나만 소유한다
/// (StageMaskSlot의 tx 모션과 같은 규율). 새 열기 · 닫기는 앞 모션을 먼저 확정한다.
/// tx · 마스크 지우기 · 장면 경계가 Reset으로 끝값 없이 끊고 중립에 돌린다 — 마스크 모양은 그 커맨드의 것이 이긴다.
/// </summary>
public sealed class StageCutInSlot
{
    public string StageKey { get; }
    public RectTransform Window { get; }
    public RectTransform Panel { get; }
    public Image PanelImage { get; }

    private readonly StageMaskSlot _mask;

    public bool IsOpen { get; private set; }

    public bool IsPlayingMotion => _motion != null;

    // 에디트 모드(빌더 · 테스트)에서 DOTween을 깨우지 않으려고 DOTween.Kill(target) 대신 이 참조로 끊는다.
    private Tween _motion;

    public StageCutInSlot(string stageKey, RectTransform window, RectTransform panel, Image panelImage, StageMaskSlot mask)
    {
        StageKey = stageKey;
        Window = window;
        Panel = panel;
        PanelImage = panelImage;
        _mask = mask;
    }

    /// <summary>
    /// 연다 — 판 · 창 피벗 · 마스크를 세우고(창은 1 · 0), 그 위에 등장 모션을 relative로 건다. clip이 null이면 열린 자리만 세운다.
    /// </summary>
    public void PlayOpen(CutInPresetDBSO preset, Sprite panelSprite, Vector2 center, float scale, MotionClip clip)
    {
        CompleteMotion();
        Layout(preset, panelSprite, center, scale);
        PlayMotion(clip, null);
    }

    // 압축 — 등장 모션의 끝값으로 바로.
    public void SnapOpen(CutInPresetDBSO preset, Sprite panelSprite, Vector2 center, float scale, MotionClip clip)
    {
        CompleteMotion();
        Layout(preset, panelSprite, center, scale);
        WriteEnd(clip, Vector3.one.ToCore(), Vector3.zero.ToCore());
    }

    /// <summary>닫는다 — 지금 창 값에서 퇴장 모션을 걸고, 끝에서 닫힘(창 배율 0)으로 둔다. clip이 null이면 바로 닫는다.</summary>
    public void PlayClose(MotionClip clip)
    {
        CompleteMotion();
        PlayMotion(clip, SetClosed);
    }

    // 압축 — 닫힌 끝 상태로 바로.
    public void SnapClose()
    {
        CompleteMotion();
        SetClosed();
    }

    /// <summary>중립 — 창 항등, 판 꺼짐. 마스크는 건드리지 않는다(부른 쪽이 정한다).</summary>
    public void Reset()
    {
        _motion?.Kill(false);
        _motion = null;

        Window.pivot = new Vector2(0.5f, 0.5f);
        Window.anchoredPosition = Vector2.zero;
        Window.sizeDelta = Vector2.zero;
        Window.localScale = Vector3.one;
        Window.localEulerAngles = Vector3.zero;

        PanelImage.sprite = null;
        PanelImage.enabled = false;

        IsOpen = false;
    }

    // 판 자리 · 크기 · 모양을 세우고, 창 피벗을 판 아래 가운데로, 마스크를 판 아래 변으로.
    // 창의 배율 · 회전은 1 · 0으로 둔다(등장 모션이 그 위에 relative로 얹힌다).
    // center는 판 중심(무대 중심 기준 px, y 위), scale은 프리셋 판 크기에 곱한다.
    private void Layout(CutInPresetDBSO preset, Sprite panelSprite, Vector2 center, float scale)
    {
        Vector2 size = preset.panelSize * scale;
        Vector2 bottomCenter = new(center.x, center.y - size.y * 0.5f);

        // 창은 무대 rect와 같다(늘림 앵커 · 위치 0 · 크기 증감 0) — 피벗을 옮겨도 rect는 그대로이고, 배율 · 회전의 중심만 옮겨 간다.
        Vector2 windowSize = Window.rect.size;
        Window.pivot = new Vector2(
            windowSize.x > 0f ? 0.5f + bottomCenter.x / windowSize.x : 0.5f,
            windowSize.y > 0f ? 0.5f + bottomCenter.y / windowSize.y : 0.5f);
        Window.anchoredPosition = Vector2.zero;
        Window.sizeDelta = Vector2.zero;
        Window.localScale = Vector3.one;
        Window.localEulerAngles = Vector3.zero;

        // 판 — 창 rect 가운데 기준. 그림은 비튼 사각형을 투명 여백 안에 그린 것이다(모서리 비율 = 프리셋).
        Panel.anchorMin = new Vector2(0.5f, 0.5f);
        Panel.anchorMax = new Vector2(0.5f, 0.5f);
        Panel.pivot = new Vector2(0.5f, 0f);
        Panel.anchoredPosition = bottomCenter;
        Panel.sizeDelta = size;
        Panel.localScale = Vector3.one;
        Panel.localEulerAngles = Vector3.zero;

        PanelImage.sprite = panelSprite;
        PanelImage.preserveAspect = false;
        PanelImage.enabled = panelSprite != null;

        // 마스크 — 무대 루트 · Mask_Root는 창과 같은 rect, 원점은 가운데다. 판과 같은 좌표로 잡는다.
        preset.GetMaskQuad(center, scale, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3);
        _mask.SetCustomQuad(p0, p1, p2, p3);

        IsOpen = true;
    }

    // 닫힘 — 퇴장 모션의 끝. 창 배율 0으로 남는다(무대 내용도 함께 안 보인다).
    private void SetClosed()
    {
        Window.localScale = new Vector3(0f, 0f, 1f);
        Window.localEulerAngles = Vector3.zero;
        IsOpen = false;
    }

    // 앞 모션을 끝값으로 확정한다(OnComplete가 돈다). 없으면 아무것도 안 한다.
    private void CompleteMotion() => _motion?.Kill(true);

    // 시작값은 창의 지금 값(열기는 Layout이 세운 1 · 0), 끝값은 클립의 마지막 키.
    private void PlayMotion(MotionClip clip, Action onEnd)
    {
        Vec3 startScale = Window.localScale.ToCore();
        Vec3 startEuler = SignedEuler(Window.localEulerAngles).ToCore();

        if (clip == null || clip.Duration <= 0f)
        {
            WriteEnd(clip, startScale, startEuler);
            onEnd?.Invoke();
            return;
        }

        _motion = DOTween
            .To(() => 0f, seconds => WriteAt(clip, startScale, startEuler, seconds), clip.Duration, clip.Duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(Window)
            .OnComplete(() =>
            {
                _motion = null;
                WriteEnd(clip, startScale, startEuler);
                onEnd?.Invoke();
            });
    }

    private void WriteAt(MotionClip clip, Vec3 startScale, Vec3 startEuler, float seconds)
    {
        foreach (MotionChannel channel in clip.Channels)
            Write(channel, MotionMath.Compose(channel, MotionChannelMode.Relative, StartOf(channel, startScale, startEuler), seconds));
    }

    private void WriteEnd(MotionClip clip, Vec3 startScale, Vec3 startEuler)
    {
        if (clip == null)
            return;

        foreach (MotionChannel channel in clip.Channels)
            Write(channel, MotionMath.EndValue(channel, MotionChannelMode.Relative, StartOf(channel, startScale, startEuler)));
    }

    private static Vec3 StartOf(in MotionChannel channel, Vec3 startScale, Vec3 startEuler)
        => channel.Kind == MotionChannelKind.Scale ? startScale : startEuler;

    private void Write(in MotionChannel channel, Vec3 value)
    {
        switch (channel.Kind)
        {
            case MotionChannelKind.Scale:
                Window.localScale = new Vector3(value.X, value.Y, 1f);
                break;

            case MotionChannelKind.Euler:
                Window.localEulerAngles = value.ToUnity();
                break;

            // 창은 자리를 움직이지 않는다 — 자리는 Layout(대본의 x y)이 정한다.
            case MotionChannelKind.Position:
                break;
        }
    }

    private static Vector3 SignedEuler(Vector3 e)
        => new(Signed(e.x), Signed(e.y), Signed(e.z));

    private static float Signed(float degrees)
        => degrees > 180f ? degrees - 360f : degrees;
}
