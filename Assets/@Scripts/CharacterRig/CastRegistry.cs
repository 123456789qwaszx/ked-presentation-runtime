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
    }
    
    private readonly Dictionary<string, CastBinding> _slotToBinding = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _characterToSlot = new(StringComparer.Ordinal);
    
    public void CastCharRig(string slotKey, string characterKey, string variantKey)
    {
        if (string.IsNullOrEmpty(slotKey) || string.IsNullOrEmpty(characterKey))
            return;
        
        if(IsCast(slotKey))
            UncastCharRig(slotKey);

        _slotToBinding[slotKey] = new CastBinding(characterKey, variantKey);
        _characterToSlot[characterKey] = slotKey;
    }

    public bool UncastCharRig(string slotKey)
    {
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
        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning($"[CastRegistry] Binding not found. slotKey='{slotKey}'." +
                             $" Expected order: SetupCommand -> CastCommand -> PortraitCommand.");
            
            characterKey = null;
            return false;
        }
        
        characterKey = binding.character;
        return true;
    }

    public bool TryGetVariant(string slotKey, out string variantKey)
    {
        if (!_slotToBinding.TryGetValue(slotKey, out CastBinding binding))
        {
            Debug.LogWarning($"[CastRegistry] Binding not found. slotKey='{slotKey}'." +
                             $" Expected order: SetupCommand -> CastCommand -> PortraitCommand.");
            
            variantKey = null;
            return false;
        }

        variantKey = binding.variant;
        return true;
    }
    
    public bool TryGetSlotKey(string characterKey, out string slotKey)
    {
        return _characterToSlot.TryGetValue(characterKey, out slotKey);
    }
    
    public void Clear()
    {
        _slotToBinding.Clear();
        _characterToSlot.Clear();
    }
    
    
    private bool IsCast(string targetKey)
    {
        if (_characterToSlot.ContainsKey(targetKey))
            return true;

        return _slotToBinding.ContainsKey(targetKey);
    }
}