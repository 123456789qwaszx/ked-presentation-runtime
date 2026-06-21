using UnityEngine;

public interface IProtagonistCharRigSlotProvider
{
    RectTransform ProtagonistSlot { get; }
}

public sealed partial class DialogueBox00_Portrait : IProtagonistCharRigSlotProvider
{
    public RectTransform ProtagonistSlot => View.Rect(Refs.DialogueBox00ProtagonistCutinViewport_Mask);
}

public sealed class CharRigSlotResolver
{
    private IStageDepthContentSlotProvider _stageSlots;
    private IProtagonistCharRigSlotProvider _protagonistSlot;

    public bool TryResolve(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out RectTransform rect)
    {
        EnsureProviders();

        if (_stageSlots == null)
        {
            rect = null;
            Debug.LogWarning(
                $"[CharRigSlotResolver] Missing stage depth slot provider. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        rect = _stageSlots.GetDepthContent(stage, layer);

        if (rect == null)
        {
            Debug.LogWarning(
                $"[CharRigSlotResolver] Stage depth slot is null. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        return true;
    }

    public bool TryResolveProtagonist(out RectTransform rect)
    {
        EnsureProviders();

        if (_protagonistSlot == null)
        {
            rect = null;
            Debug.LogWarning("[CharRigSlotResolver] Missing protagonist slot provider.");
            return false;
        }

        rect = _protagonistSlot.ProtagonistSlot;

        if (rect == null)
        {
            Debug.LogWarning("[CharRigSlotResolver] Protagonist slot is null.");
            return false;
        }

        return true;
    }

    private void EnsureProviders()
    {
        UIManager ui = UIManager.Instance;

        if (ui == null)
            return;

        if (_stageSlots == null)
            _stageSlots = ui.GetUI<PresentationUIRoot>();

        if (_protagonistSlot == null)
            _protagonistSlot = ui.GetUI<DialogueBox00_Portrait>();
    }
}