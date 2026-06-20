using UnityEngine;

public enum CharRigSlot
{
    Stage00CharacterSlot = 1,
    Stage01CharacterSlot = 2,
    Stage02CharacterSlot = 3,
    ProtagonistSlot = 10
}

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
                $"[CharRigSlotResolver] Failed to resolve stage depth slot provider. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        rect = _stageSlots.GetDepthContent(stage, layer);

        if (rect == null)
        {
            Debug.LogWarning(
                $"[CharRigSlotResolver] Resolved slot is null. " +
                $"stage='{stage}', layer='{layer}'.");
            return false;
        }

        return true;
    }

    // Legacy path. Character used to default to Mid.
    public bool TryResolve(CharRigSlot slot, out RectTransform rect)
    {
        switch (slot)
        {
            case CharRigSlot.Stage00CharacterSlot:
                return TryResolve(
                    PresentationStageKey.Stage00,
                    PresentationDepthLayerKey.Mid,
                    out rect);

            case CharRigSlot.Stage01CharacterSlot:
                return TryResolve(
                    PresentationStageKey.Stage01,
                    PresentationDepthLayerKey.Mid,
                    out rect);

            case CharRigSlot.Stage02CharacterSlot:
                return TryResolve(
                    PresentationStageKey.Stage02,
                    PresentationDepthLayerKey.Mid,
                    out rect);

            case CharRigSlot.ProtagonistSlot:
                return TryResolveProtagonist(out rect);

            default:
                rect = null;
                Debug.LogWarning($"[CharRigSlotResolver] Unknown slot. slot='{slot}'.");
                return false;
        }
    }

    private bool TryResolveProtagonist(out RectTransform rect)
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