using UnityEngine;

public sealed class BackgroundRigSlotResolver
{
    private IStageDepthContentSlotProvider _stageSlots;

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform parent)
    {
        EnsureCachedSlots();
        
        parent = _stageSlots.GetDepthContent(stage, layer);
        
        return true;
    } 

    private void EnsureCachedSlots()
    {
        if (_stageSlots != null)
            return;

        _stageSlots = UIManager.Instance?.GetUI<PresentationUIRoot>();
    }
}