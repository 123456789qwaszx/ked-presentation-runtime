using System;
using UnityEngine;

public sealed class CharRigSlotResolver : ICharRigSlotResolver
{
    private readonly DialogueUIRoot _dialogueUIRoot;
    private readonly DialogueBox00_WithPortrait _withPortraitBox;
    

    public CharRigSlotResolver(DialogueUIRoot ui, DialogueBox00_WithPortrait withPortraitBox)
    {
        _dialogueUIRoot = ui;
        _withPortraitBox = withPortraitBox;
    }

    public RectTransform Resolve(CharRigSlot slot, bool strict)
    {
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