using UnityEngine;

public static class BackgroundRigSlotResolver
{
    public static bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform parent)
    {
        IStageDepthContentSlotProvider slots =
            UIManager.Instance?.GetUI<PresentationUIRoot>();
        
        parent = slots?.GetDepthContent(stage, layer);
        return parent != null;
    }
}