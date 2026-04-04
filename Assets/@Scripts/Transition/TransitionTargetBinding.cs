using System;
using UnityEngine;

public enum TransitionTargetKind
{
    Blackout,
    WhiteFlash,
    BroadcastOverlay,
    MemoryOverlay,
    Custom,
}

[Serializable]
public sealed class TransitionTargetBinding
{
    public TransitionTargetKind kind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";
    public CanvasGroup canvasGroup;
}