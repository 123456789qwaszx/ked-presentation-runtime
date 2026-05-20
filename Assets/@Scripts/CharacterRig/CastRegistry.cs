using System;
using System.Collections.Generic;

public readonly struct CastBinding
{
    public readonly string RoleKey;
    public readonly string CharacterKey;
    public readonly string VariantKey;

    public CastBinding(string roleKey, string characterKey, string variantKey)
    {
        RoleKey = roleKey;
        CharacterKey = characterKey;
        VariantKey = variantKey;
    }
}

public sealed class CastRegistry
{
    private readonly Dictionary<string, CastBinding> _roleToBinding = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _characterToRole = new(StringComparer.Ordinal);

    public bool TryGetBinding(string roleKey, out CastBinding binding)
    {
        return _roleToBinding.TryGetValue(roleKey, out binding);
    }

    public bool TryGetCharacter(string roleKey, out string characterKey)
    {
        if (TryGetBinding(roleKey, out CastBinding binding))
        {
            characterKey = binding.CharacterKey;
            return true;
        }

        characterKey = null;
        return false;
    }

    public bool TryGetVariant(string roleKey, out string variantKey)
    {
        if (TryGetBinding(roleKey, out CastBinding binding))
        {
            variantKey = binding.VariantKey;
            return true;
        }

        variantKey = null;
        return false;
    }

    public bool TryGetRole(string characterKey, out string roleKey)
    {
        return _characterToRole.TryGetValue(characterKey, out roleKey);
    }

    public void Cast(string roleKey, string characterKey, string variantKey)
    {
        if (string.IsNullOrEmpty(roleKey) || string.IsNullOrEmpty(characterKey))
            return;

        UncastRole(roleKey);
        UncastCharacterInternal(characterKey);

        _roleToBinding[roleKey] = new CastBinding(roleKey, characterKey, variantKey);
        _characterToRole[characterKey] = roleKey;
    }

    public bool UncastRole(string roleKey)
    {
        if (string.IsNullOrEmpty(roleKey))
            return false;

        if (!_roleToBinding.TryGetValue(roleKey, out CastBinding binding))
            return false;

        _roleToBinding.Remove(roleKey);

        if (!string.IsNullOrEmpty(binding.CharacterKey))
            _characterToRole.Remove(binding.CharacterKey);

        return true;
    }

    public void Clear()
    {
        _roleToBinding.Clear();
        _characterToRole.Clear();
    }

    private bool UncastCharacterInternal(string characterKey)
    {
        if (string.IsNullOrEmpty(characterKey))
            return false;

        if (!_characterToRole.TryGetValue(characterKey, out string roleKey))
            return false;

        _characterToRole.Remove(characterKey);

        if (!string.IsNullOrEmpty(roleKey))
            _roleToBinding.Remove(roleKey);

        return true;
    }
}