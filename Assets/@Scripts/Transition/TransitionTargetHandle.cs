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