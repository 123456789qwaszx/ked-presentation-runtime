using UnityEngine;

public interface IStageCharRigSlotProvider
{
    RectTransform Stage00CharacterSlot { get; }
    RectTransform Stage01CharacterSlot { get; }
    RectTransform Stage02CharacterSlot { get; }
}

public sealed partial class PresentationUIRoot : IStageCharRigSlotProvider
{
    public RectTransform Stage00CharacterSlot => View.Rect(Refs.Stage00CharSlotRig_Root);
    public RectTransform Stage01CharacterSlot => View.Rect(Refs.Stage01CharSlotRig_Root);
    public RectTransform Stage02CharacterSlot => View.Rect(Refs.Stage02CharSlotRig_Root);
}

public interface IProtagonistCharRigSlotProvider
{
    RectTransform ProtagonistSlot { get; }
}

public sealed partial class DialogueBox00_Portrait : IProtagonistCharRigSlotProvider
{
    public RectTransform ProtagonistSlot => View.Rect(Refs.DialogueBox00ProtagonistCutinViewport_Mask);
}

public enum CharRigSlot
{
    Stage00CharacterSlot = 1,
    Stage01CharacterSlot = 2,
    Stage02CharacterSlot = 3,
    ProtagonistSlot = 10
}

public sealed class CharRigSlotResolver
{
    private IStageCharRigSlotProvider _stageSlots;
    private IProtagonistCharRigSlotProvider _protagonistSlot;
    
    private bool _init;
    
    public bool TryResolve(CharRigSlot slot, out RectTransform rect)
    {
        rect = null;
        
        if (!_init)
            EnsureProviders();

        rect = slot switch
        {
            CharRigSlot.Stage00CharacterSlot => _stageSlots?.Stage00CharacterSlot,
            CharRigSlot.Stage01CharacterSlot => _stageSlots?.Stage01CharacterSlot,
            CharRigSlot.Stage02CharacterSlot => _stageSlots?.Stage02CharacterSlot,
            CharRigSlot.ProtagonistSlot => _protagonistSlot?.ProtagonistSlot,

            _ => null
        };

        if (rect == null)
        {
            Debug.LogWarning($"[CharRigSlotResolver] Missing slot '{slot}'.");
            return false;
        }

        return true;
    }
    
    
    private void EnsureProviders()
    {
        UIManager ui = UIManager.Instance;

        if (ui != null)
        {
            _stageSlots = ui.GetUI<PresentationUIRoot>();
            _protagonistSlot = ui.GetUI<DialogueBox00_Portrait>();
        }

        if (_stageSlots != null && _protagonistSlot != null)
            _init = true;
    }
}