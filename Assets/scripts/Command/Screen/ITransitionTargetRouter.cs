using System.Collections;
using System;
using UnityEngine;

public interface ITransitionTargetRouter
{
    bool TryResolve(
        TransitionTargetKind kind,
        string customTargetKey,
        out TransitionTargetHandle handle);
}

public interface ITransitionTargetPlayer
{
    void SetInstant(TransitionTargetHandle target, float alpha, bool blockRaycasts);

    IEnumerator FadeTo(
        TransitionTargetHandle target,
        float targetAlpha,
        float duration,
        bool blockRaycasts,
        AnimationCurve ease);
}

public interface ITransitionSwapParticipant
{
    void OnTransitionSwapEntered(TransitionContext context);
}


[Serializable]
public sealed class TransitionTargetHandle
{
    public string routeKey;
    public TransitionTargetKind kind;
    public CanvasGroup canvasGroup;

    public bool IsValid => canvasGroup != null;
}