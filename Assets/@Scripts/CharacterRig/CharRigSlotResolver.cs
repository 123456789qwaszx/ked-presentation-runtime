using UnityEngine;

public sealed class CharRigSlotResolver
{
    private IStageDepthContentSlotProvider _stageSlots;
    private IProtagonistCharRigSlotProvider _protagonistSlot;

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform rect)
    {
        rect = null;

        if (!TryEnsureStageSlotsProvider())
            return false;

        rect = _stageSlots.GetDepthContent(stage, layer);
        
        return true;
    }

    public bool TryResolveProtagonist(out RectTransform rect)
    {
        rect = null;

        if (!TryEnsureProtagonistSlotProvider())
            return false;

        rect = _protagonistSlot.ProtagonistSlot;

        return true;
    }

    private bool TryEnsureStageSlotsProvider()
    {
        if (_stageSlots != null)
            return true;

        _stageSlots = UIManager.Instance?.GetUI<PresentationUIRoot>();

        return _stageSlots != null;
    }

    private bool TryEnsureProtagonistSlotProvider()
    {
        if (_protagonistSlot != null)
            return true;

        _protagonistSlot = UIManager.Instance?.GetUI<DialogueBox00_Portrait>();

        return _protagonistSlot != null;
    }
}