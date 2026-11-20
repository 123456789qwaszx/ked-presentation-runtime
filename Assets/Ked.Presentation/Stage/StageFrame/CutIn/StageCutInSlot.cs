using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 무대 하나의 컷인 창 — docs/PLAN_cutin.md · docs/PLAN_stage-visibility.md.
///
///   {stage}_Window        무대 루트의 부모. 무대를 한 덩어리로 들고 팝 · 회전한다
///     {stage}_Stage_Root  (무대 리그) — 마스크가 판 아래 변으로만 자른다(위는 열림). 판 그림은 그 무대의 far 층에 선 리그다
///                         (파서가 cutin_open을 [판 리그, 창]으로 푼다). 판도 마스크 안이라 잘린 아래 외곽선은 마스크 테두리가 다시 그린다
///     {stage}_CutInFlash  (흰빛으로 여는 컷인이 처음 올 때만 선다) 판 모양의 흰 덮개 — 무대 내용 위를 덮는다
///
/// 여는 모양은 둘이다(docs/PLAN_cutin.md §5.1). 튀어나옴(PlayOpen — 창의 배율 · 회전 클립)과
/// 흰빛(PlayFlashOpen — 흰 선이 긋고 흰 판이 된 뒤 걷힌다. 창은 움직이지 않는다).
/// 닫는 모양도 둘이다. 튀어 닫힘(PlayClose — 창 클립)과 흐려짐(PlayFadeClose — 창의 불투명도). 끝 상태는 같다.
///
/// 상태는 셋이다. 중립(창 항등 — 컷인을 안 쓰는 무대), 열림(Layout 뒤), 닫힘(퇴장 모션 끝 — 창 항등 · 마스크 숨김).
/// 보이기 · 숨기기는 마스크 하나가 정한다 — 창은 자세(팝 · 회전)만 맡고 무대를 숨기지 않는다(docs/PLAN_stage-visibility.md §2).
/// 창 모션(배율 · 회전 — 캐릭터 motion과 같은 식, MotionMath.Compose relative)은 이 슬롯이 하나만 소유한다
/// (StageMaskSlot의 stage_mask 모션과 같은 규율). 새 열기 · 닫기는 앞 모션을 먼저 확정한다.
/// 장면 경계(StageFrameRegistry.ResetAll)가 Reset으로 끝값 없이 끊고 중립에 돌린다. 마스크 커맨드는 창을 모른다.
/// </summary>
public sealed class StageCutInSlot
{
    public string StageKey { get; }
    public RectTransform Window { get; }

    private readonly StageMaskSlot _mask;

    public bool IsOpen { get; private set; }

    public bool IsPlayingMotion => _motion != null;

    // 에디트 모드(빌더 · 테스트)에서 DOTween을 깨우지 않으려고 DOTween.Kill(target) 대신 이 참조로 끊는다.
    private Tween _motion;

    // 흰 덮개 · 창의 불투명도 — 쓰는 모션이 처음 올 때 세운다. 쓰지 않는 무대에는 없다.
    private StageCutInFlashGraphic _flash;
    private CanvasGroup _group;

    /// <summary>흰 덮개 노드의 이름 — 빌더가 창 아래에서 이 이름은 치우지 않는다(StageFrameBuilder).</summary>
    public static string FlashNodeName(string stageKey) => $"{stageKey}_CutInFlash";

    public StageCutInSlot(string stageKey, RectTransform window, StageMaskSlot mask)
    {
        StageKey = stageKey;
        Window = window;
        _mask = mask;
    }

    /// <summary>
    /// 연다 — 창 피벗 · 마스크(네 점 · 아래 외곽선)를 세우고(창은 1 · 0), 그 위에 등장 모션을 relative로 건다. clip이 null이면 열린 자리만 세운다.
    /// </summary>
    public void PlayOpen(CutInPresetDBSO preset, Vector2 center, float scale, MotionClip clip)
    {
        CompleteMotion();
        Layout(preset, center, scale);
        PlayMotion(clip, null);
    }

    // 압축 — 등장 모션의 끝값으로 바로.
    public void SnapOpen(CutInPresetDBSO preset, Vector2 center, float scale, MotionClip clip)
    {
        CompleteMotion();
        Layout(preset, center, scale);
        WriteEnd(clip, Vector3.one.ToCore(), Vector3.zero.ToCore());
    }

    /// <summary>
    /// 흰빛으로 연다(cutin_flash_in) — 흰 선이 판 가운데 높이를 긋고(①), 판 모양의 흰 판이 되고(② — 이 순간 카드가 다 선 크기 · 제자리로 선다),
    /// 머문 뒤(③) 흰빛이 걷힌다(④). 창은 움직이지 않는다(배율 1 · 회전 0).
    /// 선이 긋는 동안 그 무대는 마스크가 숨긴다 — 대본이 미리 세운 판 · 사람이 흰 판보다 먼저 보이지 않게.
    /// </summary>
    public void PlayFlashOpen(CutInPresetDBSO preset, Vector2 center, float scale)
    {
        CompleteMotion();

        float duration = preset.FlashDuration;

        if (duration <= 0f)
        {
            Layout(preset, center, scale);
            return;
        }

        LayoutWindow(preset, center, scale);
        _mask.SetMaskedHidden();

        StageCutInFlashGraphic flash = EnsureFlash();
        bool revealed = false;

        void WriteAt(float seconds)
        {
            CutInFlashFrame frame = preset.EvaluateFlash(seconds);
            Vector2 p0, p1, p2, p3;

            if (frame.IsPlate)
            {
                // ② 선이 판이 되는 순간 — 마스크를 판 모양으로 열어 카드를 세운다. 흰 판이 판 안을 덮고 있다.
                if (!revealed)
                {
                    revealed = true;
                    LayoutMask(preset, center, scale);
                }

                preset.GetCorners(center, scale, out p0, out p1, out p2, out p3);
            }
            else
            {
                preset.GetFlashLineQuad(center, scale, frame.LineProgress, out p0, out p1, out p2, out p3);
            }

            Color color = preset.flashColor;
            color.a *= frame.Whiteness;

            flash.SetQuad(p0, p1, p2, p3);
            flash.color = color;
            flash.enabled = true;
        }

        WriteAt(0f);

        _motion = DOTween
            .To(() => 0f, seconds => WriteAt(seconds), duration, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(Window)
            .OnComplete(() =>
            {
                _motion = null;

                // 끝 상태 — 다 드러난 카드. 줄을 넘겨 도중에 확정해도 여기로 온다.
                if (!revealed)
                    LayoutMask(preset, center, scale);

                HideFlash();
            });
    }

    /// <summary>닫는다 — 지금 창 값에서 퇴장 모션을 걸고, 끝에서 닫힘(창 항등 · 마스크 숨김)으로 둔다. clip이 null이면 바로 닫는다.</summary>
    public void PlayClose(MotionClip clip)
    {
        CompleteMotion();
        PlayMotion(clip, SetClosed);
    }

    /// <summary>
    /// 흐려져 닫는다(cutin_fade_out) — 판과 그 무대의 내용이 함께 투명해진다. 배율 · 회전은 변하지 않는다.
    /// 끝 상태는 PlayClose와 같다(창 항등 · 마스크 숨김). seconds가 0 이하면 바로 닫는다.
    /// </summary>
    public void PlayFadeClose(float seconds)
    {
        CompleteMotion();

        if (!(seconds > 0f))
        {
            SetClosed();
            return;
        }

        CanvasGroup group = EnsureGroup();
        group.alpha = 1f;

        _motion = DOTween
            .To(() => 1f, alpha => group.alpha = alpha, 0f, seconds)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(Window)
            .OnComplete(() =>
            {
                _motion = null;
                SetClosed();
            });
    }

    // 압축 — 닫힌 끝 상태로 바로.
    public void SnapClose()
    {
        CompleteMotion();
        SetClosed();
    }

    /// <summary>중립 — 창 항등. 마스크는 건드리지 않는다(부른 쪽이 정한다).</summary>
    public void Reset()
    {
        _motion?.Kill(false);
        _motion = null;

        // 다시 묶인 슬롯(빌더가 새로 만든다)은 앞 슬롯이 세운 덮개 · 불투명도를 모른다 — 창에서 되찾아 함께 중립으로.
        if (_flash == null)
        {
            Transform flash = Window.Find(FlashNodeName(StageKey));
            if (flash != null)
                flash.TryGetComponent(out _flash);
        }

        if (_group == null)
            Window.TryGetComponent(out _group);

        SetWindowNeutral();
        RestoreVisibility();
        IsOpen = false;
    }

    // 창 피벗을 판 아래 가운데로, 마스크를 판 아래 변으로, 테두리를 판 아래 외곽선으로.
    // 창의 배율 · 회전은 1 · 0으로 둔다(등장 모션이 그 위에 relative로 얹힌다).
    // center는 판 중심(무대 중심 기준 px, y 위), scale은 프리셋 판 크기에 곱한다 — 판 리그도 같은 값으로 선다.
    private void Layout(CutInPresetDBSO preset, Vector2 center, float scale)
    {
        LayoutWindow(preset, center, scale);
        LayoutMask(preset, center, scale);
    }

    // 창 — 피벗을 판 아래 가운데로, 배율 · 회전은 1 · 0. 여기서부터 열림이다(흰빛 열림은 마스크를 조금 뒤에 연다).
    private void LayoutWindow(CutInPresetDBSO preset, Vector2 center, float scale)
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

        RestoreVisibility();
        IsOpen = true;
    }

    // 마스크 — 판 아래 변으로 자르고(위는 열림) 테두리가 판 아래 외곽선을 그린다. 이때부터 그 무대가 보인다.
    private void LayoutMask(CutInPresetDBSO preset, Vector2 center, float scale)
    {
        // 마스크 — 무대 루트 · Mask_Root는 창과 같은 rect, 원점은 가운데다. 판 리그와 같은 좌표로 잡는다.
        preset.GetMaskQuad(center, scale, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3);
        _mask.SetCustomQuad(p0, p1, p2, p3);

        // 판 리그도 마스크 안이라 아래 외곽선 띠가 같이 잘린다 — 테두리가 그 띠를 다시 그린다.
        preset.GetOutlineSegment(center, scale, out Vector2 outlineA, out Vector2 outlineB);
        _mask.ShowCustomQuadEdge(outlineA, outlineB, preset.outlineColor, preset.maskInsetPixels);
    }

    // 닫힘 — 퇴장 모션의 끝(창 배율 0). 같은 순간 창을 항등으로 돌리고 무대는 마스크가 숨긴다 — 화면은 그대로다.
    // 창을 배율 0으로 남기면 다음에 그 무대를 stage_mask로 열어도 보이지 않는다(docs/PLAN_stage-visibility.md §1).
    private void SetClosed()
    {
        SetWindowNeutral();
        _mask.SetMaskedHidden();
        RestoreVisibility();
        IsOpen = false;
    }

    // 흰 덮개를 끄고 창의 불투명도를 1로 — 열림 · 닫힘 · 중립 어느 끝 상태에도 흰빛 · 흐려짐이 남지 않는다.
    private void RestoreVisibility()
    {
        HideFlash();

        if (_group != null)
            _group.alpha = 1f;
    }

    private void HideFlash()
    {
        if (_flash != null)
            _flash.enabled = false;
    }

    // 흰 덮개 — 창 아래, 무대 루트 다음(그 무대의 내용 위). 창을 꽉 채우고 피벗이 가운데라 네 점이 판과 같은 좌표다.
    private StageCutInFlashGraphic EnsureFlash()
    {
        if (_flash != null)
            return _flash;

        string name = FlashNodeName(StageKey);
        Transform existing = Window.Find(name);

        if (existing != null && existing.TryGetComponent(out _flash))
            return _flash;

        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(StageCutInFlashGraphic));
        go.layer = Window.gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(Window, false);
        rect.SetAsLastSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        _flash = go.GetComponent<StageCutInFlashGraphic>();
        _flash.raycastTarget = false;
        _flash.enabled = false;

        return _flash;
    }

    // 창의 불투명도 — 흐려져 닫힘이 쓴다. 불투명도만 쓴다(입력 설정은 기본값 그대로).
    private CanvasGroup EnsureGroup()
    {
        if (_group != null)
            return _group;

        if (!Window.TryGetComponent(out _group))
            _group = Window.gameObject.AddComponent<CanvasGroup>();

        return _group;
    }

    // 창 항등. 마스크는 건드리지 않는다.
    private void SetWindowNeutral()
    {
        Window.pivot = new Vector2(0.5f, 0.5f);
        Window.anchoredPosition = Vector2.zero;
        Window.sizeDelta = Vector2.zero;
        Window.localScale = Vector3.one;
        Window.localEulerAngles = Vector3.zero;
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
