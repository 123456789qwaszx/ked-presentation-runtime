using System;
using UnityEngine;

public sealed class CharRigSlotResolver : ICharRigSlotResolver
{
    private PresentationUIRoot _presentationUIRoot;
    private DialogueBox00_Portrait _withPortraitBox;

    private bool _init;
    
    public void Initialize(PresentationUIRoot ui, DialogueBox00_Portrait withPortraitBox)
    {
        if (_init)
            return;
        
        _presentationUIRoot = ui;
        _withPortraitBox = withPortraitBox;
        
        _init = true;
    }

    public RectTransform Resolve(CharRigSlot slot, bool strict)
    {
        if (!_init)
            Initialize(UIManager.Instance.GetUI<PresentationUIRoot>(), UIManager.Instance.GetUI<DialogueBox00_Portrait>());
        
        RectTransform rt = slot switch
        {
            CharRigSlot.Stage00CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage00CharSlotRig_Root),
            CharRigSlot.Stage01CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage01CharSlotRig_Root),
            CharRigSlot.Stage02CharacterSlot => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.Stage02CharSlotRig_Root),
            CharRigSlot.ProtagonistSlot => _withPortraitBox.ProtagonistRect,
            
            _ => null
        };

        if (rt == null && strict)
            throw new InvalidOperationException($"[CharRigSlot] Missing slot '{slot}'.");

        return rt;
    }
}