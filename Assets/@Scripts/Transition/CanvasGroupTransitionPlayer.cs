using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class TransitionTargetHandle
{
    public string routeKey;
    public TransitionTargetKind kind;
    public CanvasGroup canvasGroup;

    public bool IsValid => canvasGroup != null;
}

public sealed class CanvasGroupTransitionPlayer : ITransitionTargetPlayer
{
    public void SetInstant(TransitionTargetHandle target, float alpha, bool blockRaycasts)
    {
        CanvasGroup cg = target.canvasGroup;
        
        cg.alpha          = Mathf.Clamp01(alpha);
        cg.blocksRaycasts = blockRaycasts;
        cg.interactable   = false;
    }

    public IEnumerator FadeTo(TransitionTargetHandle target, float targetAlpha, float duration, bool blockRaycasts, AnimationCurve ease)
    {
        if (target == null || !target.IsValid)
            yield break;

        CanvasGroup cg = target.canvasGroup;
        targetAlpha = Mathf.Clamp01(targetAlpha);

        if (duration <= 0.0001f)
        {
            SetInstant(target, targetAlpha, blockRaycasts);
            yield break;
        }

        float startAlpha = cg.alpha;
        float t = 0f;

        cg.blocksRaycasts = blockRaycasts;
        cg.interactable   = false;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u      = Mathf.Clamp01(t / duration);
            float eased  = ease != null ? Mathf.Clamp01(ease.Evaluate(u)) : u;
            cg.alpha     = Mathf.Lerp(startAlpha, targetAlpha, eased);
            yield return null;
        }

        cg.alpha          = targetAlpha;
        cg.blocksRaycasts = blockRaycasts;
        cg.interactable   = false;
    }
}