using System;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public abstract class PresentationDialogueBoxViewBase<TRefs>
    : UIBase<TRefs>, IManagedUI, IPresentationDialogueBoxView
    where TRefs : struct, Enum
{
    public abstract RectTransform Root { get; }
    public abstract CanvasGroup CanvasGroup { get; }
    public abstract TMP_Text LineText { get; }
    public abstract TMP_Text NameText { get; }

    public virtual bool HasName => NameText != null;

    public virtual void ResetPresentationTransform()
    {
        RectTransform rect = Root;

        if (rect == null)
            return;

        rect.localPosition = Vector3.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public virtual void PrimeText(VNDialogueLine line)
    {
        TMP_Text lineText = LineText;

        if (lineText != null)
        {
            lineText.text = line != null ? line.Text : string.Empty;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
        }

        TMP_Text nameText = NameText;

        if (nameText != null)
        {
            bool showName = line != null && line.HasCharacterName;

            nameText.text = showName
                ? line.CharacterName
                : string.Empty;

            nameText.gameObject.SetActive(showName);
        }
    }

    public virtual void SetVisible(bool visible)
    {
        SetCanvas(CanvasGroup, visible);
    }

    public virtual void PrepareHidden()
    {
        if (gameObject != null)
            gameObject.SetActive(true);

        SetCanvas(CanvasGroup, false);
    }

    public virtual async YarnTask FadeInAsync(float duration, LinePresentationRun run)
    {
        if (!run.IsValid)
            return;

        CanvasGroup canvasGroup = CanvasGroup;

        SetVisible(true);

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

    public virtual void ClearText()
    {
        if (LineText != null)
            LineText.text = string.Empty;

        if (NameText != null)
            NameText.text = string.Empty;
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