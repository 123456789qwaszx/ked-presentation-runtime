using System;
using UnityEngine;
using UnityEngine.EventSystems;

public static class CanvasGroupExtensions
{
    public static void SetVisible(this CanvasGroup cg, bool visible, bool blockRaycasts = false)
    {
        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible && blockRaycasts;
        cg.blocksRaycasts = visible && blockRaycasts;
    }
}