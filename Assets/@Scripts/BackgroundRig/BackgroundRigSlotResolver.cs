using UnityEngine;

public enum BackgroundRigSlot
{
    Stage00BackgroundSlot = 1,
    Stage01BackgroundSlot = 2,
    Stage02BackgroundSlot = 3,
}

public sealed class BackgroundRigSlotResolver
{
    private IStageDepthContentSlotProvider _stageSlots;

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform parent)
    {
        EnsureCachedSlots();

        if (_stageSlots == null)
        {
            parent = null;
            Debug.LogWarning(
                $"[BackgroundRigSlotResolver] Failed to resolve stage depth slot provider. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        parent = _stageSlots.GetDepthContent(stage, layer);

        if (parent == null)
        {
            Debug.LogWarning(
                $"[BackgroundRigSlotResolver] Resolved slot is null. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        return true;
    }

    // Legacy path. Background used to default to Far.
    public bool TryResolve(BackgroundRigSlot slot, out RectTransform parent)
    {
        switch (slot)
        {
            case BackgroundRigSlot.Stage00BackgroundSlot:
                return TryResolve(
                    PresentationStageKey.Stage00,
                    PresentationDepthLayerKey.Far,
                    out parent);

            case BackgroundRigSlot.Stage01BackgroundSlot:
                return TryResolve(
                    PresentationStageKey.Stage01,
                    PresentationDepthLayerKey.Far,
                    out parent);

            case BackgroundRigSlot.Stage02BackgroundSlot:
                return TryResolve(
                    PresentationStageKey.Stage02,
                    PresentationDepthLayerKey.Far,
                    out parent);

            default:
                parent = null;
                Debug.LogWarning($"[BackgroundRigSlotResolver] Unknown slot. slot='{slot}'.");
                return false;
        }
    }

    private void EnsureCachedSlots()
    {
        if (_stageSlots != null)
            return;

        _stageSlots = UIManager.Instance?.GetUI<PresentationUIRoot>();
    }
}