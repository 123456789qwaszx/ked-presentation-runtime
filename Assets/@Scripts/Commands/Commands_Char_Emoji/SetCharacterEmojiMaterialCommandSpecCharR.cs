using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Set Character Emoji Material", Order = -699)]
public sealed class SetCharacterEmojiMaterialCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Rig Targets")]
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Reveal Initial State")]
    [Range(0f, 1f)]
    public float initialReveal = 1f;
}

public sealed class SetCharacterEmojiMaterialCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiMaterialCommandSpecCharR _spec;
    private readonly CharacterEmojiVisualPresetSO _characterEmojiVisualPresetSo;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetCharacterEmojiMaterialCommandCharR(
        SetCharacterEmojiMaterialCommandSpecCharR spec,
        CharacterEmojiVisualPresetSO characterEmojiVisualPresetSo)
    {
        _spec = spec;
        _characterEmojiVisualPresetSo = characterEmojiVisualPresetSo;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        CharacterEmojiMaterialRuntime materialRuntime = rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);
        
        materialRuntime.KillTween(true);
        materialRuntime.EnsureMaterial(_characterEmojiVisualPresetSo.baseMaterial);
        materialRuntime.ApplyPresetStatic(_characterEmojiVisualPresetSo, _spec.initialReveal);
        yield break;
    }
}