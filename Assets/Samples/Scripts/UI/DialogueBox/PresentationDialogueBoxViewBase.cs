using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public abstract class PresentationDialogueBoxViewBase<TRefs>
    : UIBase<TRefs>, IManagedUI, IPresentationDialogueBoxView
    where TRefs : struct, Enum
{
    public abstract RectTransform Root { get; }
    public abstract CanvasGroup CanvasGroup { get; }
    public abstract Image BoxImage { get; }
    public abstract TMP_Text LineText { get; }
    public abstract TMP_Text NameText { get; }

    private bool _currentLayoutUsesName = true;
    private bool _clearNameWhenHidden = true;

    public virtual bool HasName => NameText != null;

    public virtual TMP_Text GetLineText() => LineText;
    public virtual TMP_Text GetNameText() => NameText;

    public virtual void ApplySurfaceLayout(
        DialogueSurfaceLayoutPresetDBSO.Entry entry)
    {
        ApplyBoxImage(entry);
        ApplyLineLayout(entry);
        ApplyNameLayout(entry);
    }

    // 박스는 이미지 한 장에 텍스트를 얹은 것이므로, 어떤 이미지인가도 레이아웃의 일부다.
    // 교체 시점은 컨트롤러가 정한다 — 종류가 바뀌는 전환에서는 감춘 뒤에 불린다.
    private void ApplyBoxImage(
        DialogueSurfaceLayoutPresetDBSO.Entry entry)
    {
        Image image = BoxImage;

        if (image == null)
            return;

        // 프리셋이 이미지를 정하지 않으면 손대지 않는다 — 씬에 놓인 상태를 그대로 둔다.
        if (!entry.overrideImage)
            return;

        image.sprite = entry.image;

        // SetActive가 아니라 enabled — RectTransform이 살아 있어야 레이아웃이 안 흔들린다.
        image.enabled = entry.image != null;
    }

    private void ApplyLineLayout(
        DialogueSurfaceLayoutPresetDBSO.Entry entry)
    {
        TMP_Text text = LineText;
        RectTransform rect = text.rectTransform;

        rect.anchorMin = entry.lineAnchorMin;
        rect.anchorMax = entry.lineAnchorMax;
        rect.pivot = entry.linePivot;
        rect.anchoredPosition = entry.lineAnchoredPosition;
        rect.sizeDelta = entry.lineSizeDelta;

        text.alignment = entry.lineAlignment;
        text.fontSize = entry.lineFontSize;
        text.lineSpacing = entry.lineSpacing;
        text.paragraphSpacing = entry.paragraphSpacing;
        text.margin = entry.lineMargin;
        text.overflowMode = entry.lineOverflowMode;
        text.textWrappingMode = entry.lineTextWrappingMode;
    }

    private void ApplyNameLayout(
        DialogueSurfaceLayoutPresetDBSO.Entry entry)
    {
        _currentLayoutUsesName = entry.useName && HasName;
        _clearNameWhenHidden = entry.clearNameWhenHidden;

        TMP_Text nameText = NameText;
        if (nameText == null)
            return;

        nameText.gameObject.SetActive(_currentLayoutUsesName);

        if (!_currentLayoutUsesName)
            return;

        RectTransform rect = nameText.rectTransform;

        rect.anchorMin = entry.nameAnchorMin;
        rect.anchorMax = entry.nameAnchorMax;
        rect.pivot = entry.namePivot;
        rect.anchoredPosition = entry.nameAnchoredPosition;
        rect.sizeDelta = entry.nameSizeDelta;

        nameText.alignment = entry.nameAlignment;
        nameText.fontSize = entry.nameFontSize;
        nameText.margin = entry.nameMargin;
        nameText.overflowMode = entry.nameOverflowMode;
        nameText.textWrappingMode = entry.nameTextWrappingMode;
    }

    public virtual void ResetPresentationTransform()
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.localPosition = Vector3.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public virtual void PrimeText(
        string text,
        string characterName,
        bool hasCharacterName)
    {
        TMP_Text lineText = LineText;

        lineText.text = text ?? string.Empty;
        lineText.maxVisibleCharacters = 0;
        lineText.ForceMeshUpdate();

        TMP_Text nameText = NameText;
        if (nameText == null)
            return;

        bool showName = _currentLayoutUsesName && hasCharacterName;

        if (showName)
        {
            nameText.text = characterName ?? string.Empty;
            nameText.gameObject.SetActive(true);
            return;
        }

        if (_clearNameWhenHidden)
            nameText.text = string.Empty;

        nameText.gameObject.SetActive(false);
    }

    public virtual void SetVisibleImmediate(bool visible)
    {
        if (visible && gameObject != null && !gameObject.activeSelf)
            gameObject.SetActive(true);

        SetCanvas(CanvasGroup, visible);
    }

    public virtual void PrepareHidden()
    {
        if (gameObject != null)
            gameObject.SetActive(true);

        SetCanvas(CanvasGroup, false);
    }

    public virtual async YarnTask FadeInAsync(float duration, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        CanvasGroup canvasGroup = CanvasGroup;

        SetVisibleImmediate(true);

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(canvasGroup, 0f, 1f, duration, token)
            .SuppressCancellationThrow();

        if (token.IsCancellationRequested)
            return;

        SetCanvas(canvasGroup, true);
    }

    public virtual async YarnTask FadeOutAsync(float duration, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        CanvasGroup canvasGroup = CanvasGroup;

        if (canvasGroup == null)
            return;

        if (gameObject != null && !gameObject.activeSelf)
            gameObject.SetActive(true);

        float fromAlpha = canvasGroup.alpha;

        await Effects
            .FadeAlphaAsync(canvasGroup, fromAlpha, 0f, duration, token)
            .SuppressCancellationThrow();

        if (token.IsCancellationRequested)
            return;

        SetCanvas(canvasGroup, false);
    }

    public virtual async YarnTask FadeInAsync(float duration, LinePresentationRun run)
    {
        if (!run.IsValid)
            return;

        CanvasGroup canvasGroup = CanvasGroup;

        SetVisibleImmediate(true);

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(canvasGroup, 0f, 1f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        SetCanvas(canvasGroup, true);
    }

    public virtual async YarnTask FadeOutAsync(float duration, LinePresentationRun run)
    {
        if (!run.IsValid)
            return;

        CanvasGroup canvasGroup = CanvasGroup;

        if (canvasGroup == null)
            return;

        float fromAlpha = canvasGroup.alpha;

        await Effects
            .FadeAlphaAsync(canvasGroup, fromAlpha, 0f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        SetCanvas(canvasGroup, false);
    }

    private static void SetCanvas(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}