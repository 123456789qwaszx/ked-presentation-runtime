using System;
using UnityEngine;

public sealed class CharRigSlotResolver : ICharRigSlotResolver
{
    private PresentationUIRoot _presentationUIRoot;
    private DialogueBox00_WithPortrait _withPortraitBox;

    private bool _init;
    public void Initialize(PresentationUIRoot ui, DialogueBox00_WithPortrait withPortraitBox)
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
            Initialize(UIManager.Instance.GetUI<PresentationUIRoot>(), UIManager.Instance.GetUI<DialogueBox00_WithPortrait>());
        
        RectTransform rt = slot switch
        {
            CharRigSlot.CharacterSlotRight => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotRightRig_Root),
            CharRigSlot.CharacterSlotCenter => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotCenterRig_Root),
            CharRigSlot.CharacterSlotLeft => _presentationUIRoot.ResolveRect(PresentationUIRoot.Refs.CharSlotLeftRig_Root),
            CharRigSlot.ProtagonistSlot => _withPortraitBox.ProtagonistRect,
            
            _ => null
        };

        if (rt == null && strict)
            throw new InvalidOperationException($"[CharRigSlot] Missing slot '{slot}'.");

        return rt;
    }
}