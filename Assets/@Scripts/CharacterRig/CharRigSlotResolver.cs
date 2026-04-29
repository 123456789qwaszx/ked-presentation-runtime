using System;
using UnityEngine;

public sealed class CharRigSlotResolver : ICharRigSlotResolver
{
    private DialogueUIRoot _dialogueUIRoot;
    private DialogueBox00_WithPortrait _withPortraitBox;

    private bool _init;
    public void Initialize(DialogueUIRoot ui, DialogueBox00_WithPortrait withPortraitBox)
    {
        if (_init)
            return;
        
        _dialogueUIRoot = ui;
        _withPortraitBox = withPortraitBox;
        
        _init = true;
    }

    public RectTransform Resolve(CharRigSlot slot, bool strict)
    {
        if (!_init)
            Initialize(UIManager.Instance.GetUI<DialogueUIRoot>(), UIManager.Instance.GetUI<DialogueBox00_WithPortrait>());
        
        RectTransform rt = slot switch
        {
            CharRigSlot.CharacterStageSlot00 => _dialogueUIRoot.CharRigSlot,
            CharRigSlot.CharacterStageSlot01 => _dialogueUIRoot.CharRigSlot1,
            CharRigSlot.CharacterStageSlot02 => _dialogueUIRoot.CharRigSlot2,
            CharRigSlot.ProtagonistSlot => _withPortraitBox.ProtagonistRect,
            
            _ => null
        };

        if (rt == null && strict)
            throw new InvalidOperationException($"[CharRigSlot] Missing slot '{slot}'.");

        return rt;
    }
}