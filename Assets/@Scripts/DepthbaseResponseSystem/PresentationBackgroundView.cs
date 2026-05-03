using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class PresentationBackgroundView : MonoBehaviour
{
    public RectTransform Root { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }
    public Image Image { get; private set; }

    public void EnsureBound(bool strict = true)
    {
        Root = transform as RectTransform;
        if (Root == null)
        {
            if (strict)
                throw new InvalidOperationException($"[PresentationBackgroundView] Root must be RectTransform. go={name}");
            return;
        }

        if (!TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CanvasGroup = canvasGroup;

        Image = GetComponentInChildren<Image>(true);
        if (Image == null && strict)
            throw new InvalidOperationException($"[PresentationBackgroundView] Missing Image under '{name}'.");
    }
}