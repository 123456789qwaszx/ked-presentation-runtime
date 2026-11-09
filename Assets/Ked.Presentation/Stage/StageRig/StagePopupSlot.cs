using System;
using DG.Tweening;
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
/// 상태는 둘이다. 닫힘(그룹 불투명도 0 · 두 그림 꺼짐 — 중립), 열림(Layout 뒤). 페이드는 커맨드가 쓴다(PopupCommandBase).
/// 장면 경계 · 그 무대의 tx · 마스크 지우기가 Reset으로 닫는다(컷인과 같은 규칙).
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

    /// <summary>그룹 불투명도 — 뒷막과 그림에 같이 곱해진다.</summary>
    public float Alpha => Canvas.alpha;

    // 페이드는 한 번에 하나 — 컷인 창과 같은 규율(StageCutInSlot). 에디트 모드에서 DOTween을 깨우지 않으려고 DOTween.Kill(target)을 쓰지 않는다.
    private Tween _activeTween;
    private Action _activeFinish;

    public StagePopupSlot(string stageKey, RectTransform group, CanvasGroup canvas, Image backdrop, RectTransform picture, Image pictureImage)
    {
        StageKey = stageKey;
        Group = group;
        Canvas = canvas;
        Backdrop = backdrop;
        Picture = picture;
        PictureImage = pictureImage;
    }

    public void SetActive(Tween tween, Action finish)
    {
        _activeTween = tween;
        _activeFinish = finish;
    }

    /// <summary>도는 페이드를 끝값으로 확정한다(없으면 아무것도 안 한다).</summary>
    public void CompleteActive()
    {
        Action finish = _activeFinish;
        _activeTween = null;
        _activeFinish = null;
        finish?.Invoke();
    }

    /// <summary>페이드가 스스로 끝났다 — 기록만 지운다(그 커맨드의 것일 때만).</summary>
    public void ClearActive(Tween tween)
    {
        if (_activeTween != tween)
            return;

        _activeTween = null;
        _activeFinish = null;
    }

    /// <summary>중립 — 닫힘. 도는 페이드는 끝값 없이 끊는다.</summary>
    public void Reset()
    {
        _activeTween?.Kill(false);
        _activeTween = null;
        _activeFinish = null;

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

    /// <summary>
    /// 연다 — 뒷막 색 · 그림 · 자리 · 크기를 세운다. 불투명도는 건드리지 않는다(등장 페이드가 지금 값에서 1로 올린다).
    /// center는 그림 중심(무대 중심 기준 px, y 위), scale은 프리셋 배율에 곱한다. 그림이 없으면 뒷막만 뜬다.
    /// </summary>
    public void Layout(PopupPresetDBSO preset, Sprite picture, Vector2 center, float scale)
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

    public void SetAlpha(float alpha) => Canvas.alpha = Mathf.Clamp01(alpha);

    /// <summary>닫힘 — 퇴장 페이드의 끝. 그림은 끄고 키는 지운다.</summary>
    public void SetClosed()
    {
        Canvas.alpha = 0f;
        Backdrop.enabled = false;
        PictureImage.sprite = null;
        PictureImage.enabled = false;
        IsOpen = false;
    }
}
