using DG.Tweening;
using UnityEngine;

public static class PresentationTransitionClearUtility
{
    public static void ClearAll()
    {
        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null)
            return;

        ClearVerticalStrip(provider.VerticalStripWipe);
        ClearFocusBlurCurtain(provider.FocusBlurCurtain);
        ClearSlantedMasks(provider.SlantedMaskEdgeGraphic);
    }

    public static void ClearAllExcept(PresentationTransitionLayer except)
    {
        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null)
            return;

        if (except != PresentationTransitionLayer.VerticalStripWipe)
            ClearVerticalStrip(provider.VerticalStripWipe);

        if (except != PresentationTransitionLayer.FocusBlurCurtain)
            ClearFocusBlurCurtain(provider.FocusBlurCurtain);

        if (except != PresentationTransitionLayer.SlantedMask)
            ClearSlantedMasks(provider.SlantedMaskEdgeGraphic);
    }

    private static void ClearVerticalStrip(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(false);

        VerticalStripWipeGraphic graphic = rect.GetComponent<VerticalStripWipeGraphic>();
        if (graphic == null)
            return;

        DOTween.Kill(graphic, false);
        graphic.ClearImmediate();
    }

    private static void ClearFocusBlurCurtain(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(false);

        FocusBlurCurtainGraphic graphic = rect.GetComponent<FocusBlurCurtainGraphic>();
        if (graphic == null)
            return;

        DOTween.Kill(graphic, false);
        graphic.ClearImmediate();
    }

    private static void ClearSlantedMasks(RectTransform root)
    {
        if (root == null)
            return;

        SlantedMaskGraphic[] masks = root.GetComponentsInChildren<SlantedMaskGraphic>(true);

        for (int i = 0; i < masks.Length; i++)
        {
            SlantedMaskGraphic mask = masks[i];

            if (mask == null)
                continue;

            DOTween.Kill(mask, false);
            mask.ResetToHiddenOffset();
        }
    }
}

public enum PresentationTransitionLayer
{
    None = 0,
    VerticalStripWipe = 10,
    SlantedShutter = 20,
    FocusBlurFade = 30,
    FocusBlurCurtain = 40,
    SlantedMask = 50
}