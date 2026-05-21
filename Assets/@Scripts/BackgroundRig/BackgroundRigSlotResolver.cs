using UnityEngine;

public enum BackgroundRigSlot
{
    Stage00BackgroundSlot = 1,
    Stage01BackgroundSlot = 2,
    Stage02BackgroundSlot = 3,
}

public interface IStageBackgroundRigSlotProvider
{
    RectTransform Stage00BackgroundSlot { get; }
    RectTransform Stage01BackgroundSlot { get; }
    RectTransform Stage02BackgroundSlot { get; }
}

public sealed partial class PresentationUIRoot : IStageBackgroundRigSlotProvider
{
    public RectTransform Stage00BackgroundSlot => View.Rect(Refs.Stage00BGContent_Root);
    public RectTransform Stage01BackgroundSlot => View.Rect(Refs.Stage01BGContent_Root);
    public RectTransform Stage02BackgroundSlot => View.Rect(Refs.Stage02BGContent_Root);
}

public sealed class BackgroundRigSlotResolver
{
    private IStageBackgroundRigSlotProvider _stageSlots;

    public bool TryResolve(BackgroundRigSlot slot, out RectTransform parent)
    {
        EnsureCachedSlots();

        parent = null;

        if (_stageSlots == null)
        {
            Debug.LogWarning($"[BackgroundRigSlotResolver] Failed to resolve slot provider. slot='{slot}'.");
            return false;
        }

        switch (slot)
        {
            case BackgroundRigSlot.Stage00BackgroundSlot:
                parent = _stageSlots.Stage00BackgroundSlot;
                break;

            case BackgroundRigSlot.Stage01BackgroundSlot:
                parent = _stageSlots.Stage01BackgroundSlot;
                break;

            case BackgroundRigSlot.Stage02BackgroundSlot:
                parent = _stageSlots.Stage02BackgroundSlot;
                break;

            default:
                Debug.LogWarning($"[BackgroundRigSlotResolver] Unknown slot. slot='{slot}'.");
                return false;
        }

        if (parent == null)
        {
            Debug.LogWarning($"[BackgroundRigSlotResolver] Resolved slot is null. slot='{slot}'.");
            return false;
        }

        return true;
    }

    private void EnsureCachedSlots()
    {
        if (_stageSlots != null)
            return;

        UIManager ui = UIManager.Instance;
        _stageSlots = ui?.GetUI<PresentationUIRoot>();
    }
}