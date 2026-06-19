using System;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public abstract class PresentationOptionsBoxViewBase<TRefs>
    : UIBase<TRefs>, IManagedUI, IPresentationOptionsBoxView
    where TRefs : struct, Enum
{
    public abstract RectTransform Root { get; }
    public abstract RectTransform ItemContainer { get; }
    public abstract CanvasGroup CanvasGroup { get; }

    public virtual void ResetPresentationTransform()
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.localPosition = Vector3.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public virtual void SetVisibleImmediate(bool visible)
    {
        if (visible && gameObject != null && !gameObject.activeSelf)
            gameObject.SetActive(true);

        SetCanvasVisible(CanvasGroup, visible);
    }

    public virtual void SetInputEnabled(bool enabled)
    {
        CanvasGroup canvasGroup = CanvasGroup;

        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    public virtual void PrepareHidden()
    {
        if (gameObject != null)
            gameObject.SetActive(true);

        SetCanvasVisible(CanvasGroup, false);
    }

    public virtual async YarnTask FadeInAsync(float duration, CancellationToken token)
    {
        CanvasGroup canvasGroup = CanvasGroup;

        SetVisibleImmediate(true);
        SetInputEnabled(false);

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(canvasGroup, 0f, 1f, duration, token)
            .SuppressCancellationThrow();

        if (token.IsCancellationRequested)
            return;

        SetCanvasVisible(canvasGroup, true);
        SetInputEnabled(false);
    }

    public virtual async YarnTask FadeOutAsync(float duration, CancellationToken token)
    {
        CanvasGroup canvasGroup = CanvasGroup;

        if (canvasGroup == null)
            return;

        float fromAlpha = canvasGroup.alpha;

        await Effects
            .FadeAlphaAsync(canvasGroup, fromAlpha, 0f, duration, token)
            .SuppressCancellationThrow();

        if (token.IsCancellationRequested)
            return;

        SetCanvasVisible(canvasGroup, false);
    }

    private static void SetCanvasVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}