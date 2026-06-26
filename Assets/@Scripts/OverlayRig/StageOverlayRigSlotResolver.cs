using UnityEngine;

public sealed class StageOverlayRigSlotResolver
{
    private IStageOverlayRigSlotProvider _slotProvider;

    public bool TryResolve(
        StageOverlayRigRootKind kind,
        out RectTransform rect)
    {
        EnsureProvider();

        if (_slotProvider == null)
        {
            rect = null;
            Debug.LogWarning(
                $"[StageOverlayRigSlotResolver] Missing overlay rig slot provider. kind='{kind}'.");
            return false;
        }

        rect = _slotProvider.GetStageOverlayRigRoot(kind);

        if (rect == null)
        {
            Debug.LogWarning(
                $"[StageOverlayRigSlotResolver] Overlay rig root is null. kind='{kind}'.");
            return false;
        }

        return true;
    }

    private void EnsureProvider()
    {
        UIManager ui = UIManager.Instance;

        if (ui == null)
            return;

        if (_slotProvider == null)
            _slotProvider = ui.GetUI<PresentationUIRoot>();
    }
}