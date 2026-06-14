using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CastRegistry
{
    private readonly struct CastBinding
    {
        public readonly string character;
        public readonly string variant;

        public CastBinding(string character, string variant)
        {
            this.character = character;
            this.variant = variant;
        }

        public CastBinding WithVariant(string newVariant)
        {
            return new CastBinding(character, newVariant);
        }
    }

    private readonly Dictionary<string, CastBinding> _slotToBinding = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _characterToSlot = new(StringComparer.Ordinal);

    public void CastCharRig(string slotKey, string characterKey)
    {
        slotKey = (slotKey ?? "").Trim();
        characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(characterKey);

        if (string.IsNullOrEmpty(slotKey) || string.IsNullOrEmpty(characterKey))
            return;

        if (IsCast(slotKey))
            UncastCharRig(slotKey);

        _slotToBinding[slotKey] = new CastBinding(characterKey, "");
        _characterToSlot[characterKey] = slotKey;
    }

    public bool SetVariant(string slotKey, string variantKey)
    {
        slotKey = (slotKey ?? "").Trim();
        variantKey = PresentationKeyNormalizer.NormalizeVariantKey(variantKey);

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] SetVariant failed. Binding not found. " +
                $"slotKey='{slotKey}', variantKey='{variantKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PoseCommand.");

            return false;
        }

        _slotToBinding[slotKey] = binding.WithVariant(variantKey);
        return true;
    }

    public bool UncastCharRig(string slotKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.Remove(slotKey, out CastBinding binding))
        {
            Debug.LogWarning($"[CastRegistry] Uncast failed. Binding not found. slotKey='{slotKey}'.");
            return false;
        }

        _characterToSlot.Remove(binding.character);
        return true;
    }

    public bool TryGetCharacter(string slotKey, out string characterKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] Binding not found. slotKey='{slotKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PortraitCommand.");

            characterKey = null;
            return false;
        }

        characterKey = binding.character;
        return true;
    }

    public bool TryGetVariant(string slotKey, out string variantKey)
    {
        slotKey = (slotKey ?? "").Trim();

        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning(
                $"[CastRegistry] Binding not found. slotKey='{slotKey}'. " +
                $"Expected order: SetupCommand -> CastCommand -> PoseCommand.");

            variantKey = null;
            return false;
        }

        variantKey = binding.variant;
        return true;
    }

    public bool TryGetSlotKey(string characterKey, out string slotKey)
    {
        characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(characterKey);
        return _characterToSlot.TryGetValue(characterKey, out slotKey);
    }

    public void Clear()
    {
        _slotToBinding.Clear();
        _characterToSlot.Clear();
    }
    
    public bool TryPeekCharacter(string slotKey, out string characterKey)
    {
        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            characterKey = null;
            return false;
        }

        characterKey = binding.character;
        return true;
    }

    private bool IsCast(string targetKey)
    {
        targetKey = (targetKey ?? "").Trim();

        if (_slotToBinding.ContainsKey(targetKey))
            return true;

        string characterKey = PresentationKeyNormalizer.NormalizeCharacterKey(targetKey);
        return _characterToSlot.ContainsKey(characterKey);
    }
}