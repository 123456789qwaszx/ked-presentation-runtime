using UnityEngine;

public enum CharRigSlot
{ 
    Stage00CharacterSlot = 1,
    Stage01CharacterSlot = 2,
    Stage02CharacterSlot = 3,
    ProtagonistSlot = 10
}

public sealed class CharRigSlotResolver : ICharRigSlotResolver
{
    private readonly PresentationUIRoot _presentationUIRoot;
    private readonly DialogueBox00_Portrait _withPortraitBox;

    public CharRigSlotResolver(PresentationUIRoot ui, DialogueBox00_Portrait withPortraitBox)
    {
        _presentationUIRoot = ui;
        _withPortraitBox = withPortraitBox;
    }
    
    public bool TryResolve(CharRigSlot slot, out RectTransform rect)
    {
        rect = slot switch
        {
            CharRigSlot.Stage00CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage00CharSlotRig_Root),
            CharRigSlot.Stage01CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage01CharSlotRig_Root),
            CharRigSlot.Stage02CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage02CharSlotRig_Root),
            CharRigSlot.ProtagonistSlot => _withPortraitBox.ProtagonistRect,
            
            _ => null
        };

        if (rect == null)
        {
            Debug.LogWarning($"[CharRigSlotResolver] Missing slot '{slot}'.");
            return false;
        }

        return true;
    }
}