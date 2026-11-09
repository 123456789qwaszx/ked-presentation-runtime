using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무대 하나의 그림 팝업 — docs/PLAN_popup.md.
///
///   {stage}_Popup             그룹(CanvasGroup). 불투명도 하나가 뒷막과 그림을 같이 페이드한다
///     {stage}_Popup_Backdrop  화면 전체 어두운 막. 색 · 세기는 프리셋(원본 실측: 거의 검정, 0.47)
///     {stage}_Popup_Image     그림. 원본 픽셀 × 프리셋 배율(원본 실측 0.91) × 대본 배율, 무대 중심 기준 자리
///
/// 원본에서 막의 세기는 프레임마다 정확히 "팝업 불투명도 × 0.47"이었다 — 효과 둘이 아니라 한 페이드다.
/// 그래서 막의 세기는 고정이고, 움직이는 값은 그룹 불투명도 하나뿐이다.
///
/// 상태는 둘이다. 닫힘(그룹 불투명도 0 · 두 그림 꺼짐 — 중립), 열림(Layout 뒤).
/// 페이드는 이 슬롯이 하나만 소유한다(StageMaskSlot의 tx 모션과 같은 규율). 새 열기 · 닫기는 앞 페이드를 먼저 확정한다.
/// 장면 경계 · 그 무대의 tx · 마스크 지우기가 Reset으로 끝값 없이 끊고 닫는다(컷인과 같은 규칙).
/// </summary>
public sealed class StagePopupSlot
{
    public string StageKey { get; }
    public RectTransform Group { get; }
    public CanvasGroup Canvas { get; }
    public Image Backdrop { get; }
    public RectTransform Picture { get; }
    public Image PictureImage { get; }

    public bool IsOpen { get; private set; }

    public bool IsFading => _fade != null;

    /// <summary>그룹 불투명도 — 뒷막과 그림에 같이 곱해진다.</summary>
    public float Alpha => Canvas.alpha;

    // 에디트 모드(빌더 · 테스트)에서 DOTween을 깨우지 않으려고 DOTween.Kill(target) 대신 이 참조로 끊는다.
    private Tween _fade;

    public StagePopupSlot(string stageKey, RectTransform group, CanvasGroup canvas, Image backdrop, RectTransform picture, Image pictureImage)
    {
        StageKey = stageKey;
        Group = group;
        Canvas = canvas;
        Backdrop = backdrop;
        Picture = picture;
        PictureImage = pictureImage;
    }

    /// <summary>
    /// 연다 — 뒷막 · 그림 · 자리를 세우고 지금 불투명도에서 1로 페이드한다.
    /// 열린 팝업에 다시 쓰면 1에서 1로 — 그림만 바로 바뀐다(번쩍이지 않게). keys가 null이면 끝값만 쓴다.
    /// </summary>
    public void PlayOpen(PopupPresetDBSO preset, Sprite picture, Vector2 center, float scale, CurveKey[] keys)
    {
        CompleteFade();
        Layout(preset, picture, center, scale);
        PlayFade(keys, 1f, null);
    }

    // 압축 — 열린 끝 상태로 바로.
    public void SnapOpen(PopupPresetDBSO preset, Sprite picture, Vector2 center, float scale)
    {
        CompleteFade();
        Layout(preset, picture, center, scale);
        Canvas.alpha = 1f;
    }

    /// <summary>닫는다 — 지금 불투명도에서 0으로 페이드하고, 끝에서 닫힘으로 둔다. keys가 null이면 바로 닫는다.</summary>
    public void PlayClose(CurveKey[] keys)
    {
        CompleteFade();
        PlayFade(keys, 0f, SetClosed);
    }

    // 압축 — 닫힌 끝 상태로 바로.
    public void SnapClose()
    {
        CompleteFade();
        SetClosed();
    }

    /// <summary>중립 — 닫힘. 도는 페이드는 끝값 없이 끊는다.</summary>
    public void Reset()
    {
        _fade?.Kill(false);
        _fade = null;

        Canvas.alpha = 0f;
        Backdrop.enabled = false;
        PictureImage.sprite = null;
        PictureImage.enabled = false;

        Picture.anchorMin = new Vector2(0.5f, 0.5f);
        Picture.anchorMax = new Vector2(0.5f, 0.5f);
        Picture.pivot = new Vector2(0.5f, 0.5f);
        Picture.anchoredPosition = Vector2.zero;
        Picture.sizeDelta = Vector2.zero;
        Picture.localScale = Vector3.one;
        Picture.localEulerAngles = Vector3.zero;

        IsOpen = false;
    }

    // 뒷막 색 · 그림 · 자리 · 크기를 세운다. 불투명도는 건드리지 않는다.
    // center는 그림 중심(무대 중심 기준 px, y 위), scale은 프리셋 배율에 곱한다. 그림이 없으면 뒷막만 뜬다.
    private void Layout(PopupPresetDBSO preset, Sprite picture, Vector2 center, float scale)
    {
        Backdrop.color = preset.backdropColor;
        Backdrop.enabled = true;

        Picture.anchorMin = new Vector2(0.5f, 0.5f);
        Picture.anchorMax = new Vector2(0.5f, 0.5f);
        Picture.pivot = new Vector2(0.5f, 0.5f);
        Picture.anchoredPosition = center;
        Picture.sizeDelta = picture != null ? picture.rect.size * (preset.pictureScale * scale) : Vector2.zero;
        Picture.localScale = Vector3.one;
        Picture.localEulerAngles = Vector3.zero;

        PictureImage.sprite = picture;
        PictureImage.preserveAspect = false;
        PictureImage.color = Color.white;
        PictureImage.enabled = picture != null;

        IsOpen = true;
    }

    private void SetClosed()
    {
        Canvas.alpha = 0f;
        Backdrop.enabled = false;
        PictureImage.sprite = null;
        PictureImage.enabled = false;
        IsOpen = false;
    }

    // 앞 페이드를 끝값으로 확정한다(OnComplete가 돈다). 없으면 아무것도 안 한다.
    private void CompleteFade() => _fade?.Kill(true);

    // 곡선은 페이드의 진행(0 → 1)으로 읽는다: 불투명도 = 시작값 + (끝값 − 시작값) × 진행.
    private void PlayFade(CurveKey[] keys, float to, Action onEnd)
    {
        if (keys == null)
        {
            Canvas.alpha = to;
            onEnd?.Invoke();
            return;
        }

        float from = Canvas.alpha;
        float duration = keys[keys.Length - 1].Time;

        _fade = DOTween
            .To(() => 0f, seconds => Canvas.alpha = Mathf.Lerp(from, to, Progress(keys, seconds)), duration, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(Canvas)
            .OnComplete(() =>
            {
                _fade = null;
                Canvas.alpha = to;
                onEnd?.Invoke();
            });
    }

    // 곡선 값 → 진행(0 → 1). 등장 곡선은 0 → 1, 퇴장 곡선은 1 → 0이라 양끝으로 정규화한다.
    private static float Progress(CurveKey[] keys, float seconds)
    {
        float a = keys[0].Value;
        float b = keys[keys.Length - 1].Value;
        float v = CurveFunctions.Evaluate(keys, seconds);
        return Mathf.Approximately(a, b) ? 1f : Mathf.Clamp01((v - a) / (b - a));
    }
}
