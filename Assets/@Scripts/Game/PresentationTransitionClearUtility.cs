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
}

public enum PresentationTransitionLayer
{
    None = 0,
    VerticalStripWipe = 10,
    FocusBlurCurtain = 40,
}