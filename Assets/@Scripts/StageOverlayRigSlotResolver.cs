using UnityEngine;

public sealed class StageOverlayRigSlotResolver
{
    private IStageOverlayRigSlotProvider _slotProvider;

    public bool TryResolve(
        StageOverlayRigRootKind kind,
        out RectTransform rect)
    {
        EnsureProvider();

        rect = _slotProvider.GetStageOverlayRigRoot(kind);

        return true;
    }

    private void EnsureProvider()
    {
        UIManager ui = UIManager.Instance;

        if (_slotProvider == null)
            _slotProvider = ui.GetUI<PresentationUIRoot>();
    }
}