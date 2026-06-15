using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Set Character Emoji Material", Order = -699)]
public sealed class SetCharacterEmojiMaterialCommandSpecCharR : CharacterRigCommandSpecBase
{
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Image;

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
        CharacterEmojiMaterialRuntime materialRuntime = rigRefs.GetEmojiMaterialRuntime(_spec.target);
        
        ApplyPresetStatic(materialRuntime.RuntimeMaterial, _characterEmojiVisualPresetSo, _spec.initialReveal);

        yield break;
    }

    private static void ApplyPresetStatic(Material material, CharacterEmojiVisualPresetSO preset, float reveal)
    {
        material.SetFloat(CharacterEmojiShaderIds.Reveal, reveal);
        material.SetFloat(CharacterEmojiShaderIds.RevealSoftness, preset.revealSoftness);
        material.SetFloat(CharacterEmojiShaderIds.RevealDirection, GetDirectionValue(preset));

        material.SetFloat(CharacterEmojiShaderIds.EdgeRimAmount, preset.edgeRimAmount);
        material.SetFloat(CharacterEmojiShaderIds.EdgeRimWidth, preset.edgeRimWidth);
        material.SetColor(CharacterEmojiShaderIds.EdgeRimColor, preset.edgeRimColor);

        material.SetFloat(CharacterEmojiShaderIds.GlowAmount, preset.glowAmount);
        material.SetColor(CharacterEmojiShaderIds.GlowColor, preset.glowColor);
    }

    private static float GetDirectionValue(CharacterEmojiVisualPresetSO preset)
    {
        return preset.revealDirection == CharacterEmojiRevealDirection.BottomToTop
            ? 1f
            : 0f;
    }
}