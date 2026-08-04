using UnityEngine;

public sealed class CharRigSlotResolver
{
    private readonly IStageDepthContentSlotProvider _stageSlots;
    private readonly IProtagonistCharRigSlotProvider _protagonistSlot;

    public CharRigSlotResolver(
        IStageDepthContentSlotProvider stageSlots,
        IProtagonistCharRigSlotProvider protagonistSlot)
    {
        _stageSlots = stageSlots;
        _protagonistSlot = protagonistSlot;
    }

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform rect)
    {
        rect = null;

        if (_stageSlots == null)
            return false;

        rect = _stageSlots.GetDepthContent(stage, layer);
        
        return true;
    }

    public bool TryResolveProtagonist(out RectTransform rect)
    {
        rect = null;

        if (_protagonistSlot == null)
            return false;

        rect = _protagonistSlot.ProtagonistSlot;

        return true;
    }
}