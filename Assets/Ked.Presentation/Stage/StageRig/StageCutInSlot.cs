using System;
using DG.Tweening;
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
/// 모션(창의 배율 · 회전)은 커맨드가 쓴다(CutInCommandBase). 여기는 자리와 모양만 안다.
/// tx · 마스크 지우기 · 장면 경계가 Reset으로 중립에 돌린다 — 마스크 모양은 그 커맨드의 것이 이긴다.
/// </summary>
public sealed class StageCutInSlot
{
    public string StageKey { get; }
    public RectTransform Window { get; }
    public RectTransform Panel { get; }
    public Image PanelImage { get; }

    private readonly StageMaskSlot _mask;

    public bool IsOpen { get; private set; }

    // 창 모션은 한 번에 하나 — 지금 도는 컷인 커맨드의 트윈과 확정 함수. 새 커맨드는 앞 것을 확정하고(CompleteActive),
    // Reset은 확정 없이 끊는다. DOTween.Kill(target)을 쓰지 않는 것은 에디트 모드(빌더 · 테스트)에서 DOTween을 깨우지 않으려는 것이다.
    private Tween _activeTween;
    private Action _activeFinish;

    public void SetActive(Tween tween, Action finish)
    {
        _activeTween = tween;
        _activeFinish = finish;
    }

    /// <summary>도는 컷인 모션을 끝값으로 확정한다(없으면 아무것도 안 한다).</summary>
    public void CompleteActive()
    {
        Action finish = _activeFinish;
        _activeTween = null;
        _activeFinish = null;
        finish?.Invoke();
    }

    /// <summary>모션이 스스로 끝났다 — 기록만 지운다(그 커맨드의 것일 때만).</summary>
    public void ClearActive(Tween tween)
    {
        if (_activeTween != tween)
            return;

        _activeTween = null;
        _activeFinish = null;
    }

    public StageCutInSlot(string stageKey, RectTransform window, RectTransform panel, Image panelImage, StageMaskSlot mask)
    {
        StageKey = stageKey;
        Window = window;
        Panel = panel;
        PanelImage = panelImage;
        _mask = mask;
    }

    /// <summary>중립 — 창 항등, 판 꺼짐. 마스크는 건드리지 않는다(부른 쪽이 정한다).</summary>
    public void Reset()
    {
        KillWindowTweens();

        Window.pivot = new Vector2(0.5f, 0.5f);
        Window.anchoredPosition = Vector2.zero;
        Window.sizeDelta = Vector2.zero;
        Window.localScale = Vector3.one;
        Window.localEulerAngles = Vector3.zero;

        PanelImage.sprite = null;
        PanelImage.enabled = false;

        IsOpen = false;
    }

    /// <summary>창 트윈을 끊는다 — 끝값을 쓰지 않는다(Reset이 바로 덮는다).</summary>
    public void KillWindowTweens()
    {
        _activeTween?.Kill(false);
        _activeTween = null;
        _activeFinish = null;
    }

    /// <summary>
    /// 연다 — 판 자리 · 크기 · 모양을 세우고, 창 피벗을 판 아래 가운데로, 마스크를 판 아래 변으로.
    /// 창의 배율 · 회전은 1 · 0으로 둔다(등장 모션이 그 위에 relative로 얹힌다).
    /// center는 판 중심(무대 중심 기준 px, y 위), scale은 프리셋 판 크기에 곱한다.
    /// </summary>
    public void Layout(CutInPresetDBSO preset, Sprite panelSprite, Vector2 center, float scale)
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

    /// <summary>닫힘 — 퇴장 모션의 끝. 창 배율 0으로 남는다(무대 내용도 함께 안 보인다).</summary>
    public void SetClosed()
    {
        Window.localScale = new Vector3(0f, 0f, 1f);
        Window.localEulerAngles = Vector3.zero;
        IsOpen = false;
    }
}
