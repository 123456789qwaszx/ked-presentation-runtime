using System;
using System.Collections.Generic;

public sealed class CastRegistry
{
    private readonly Dictionary<string, string> _roleToCharacter = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _characterToRole = new(StringComparer.Ordinal);

    public bool TryGetCharacter(string roleKey, out string characterKey)
    {
        roleKey = SafeTrim(roleKey);
        return _roleToCharacter.TryGetValue(roleKey, out characterKey);
    }

    public bool TryGetRole(string characterKey, out string roleKey)
    {
        characterKey = SafeTrim(characterKey);
        return _characterToRole.TryGetValue(characterKey, out roleKey);
    }

    public void Cast(string roleKey, string characterKey)
    {
        roleKey = SafeTrim(roleKey);
        characterKey = SafeTrim(characterKey);

        if (string.IsNullOrEmpty(roleKey) || string.IsNullOrEmpty(characterKey))
            return;

        // 같은 슬롯에 이미 누가 캐스팅되어 있으면 해제
        UncastRole(roleKey);

        // 같은 캐릭터가 다른 슬롯에 캐스팅되어 있으면 해제
        UncastCharacterInternal(characterKey);

        _roleToCharacter[roleKey] = characterKey;
        _characterToRole[characterKey] = roleKey;
    }

    public bool UncastRole(string roleKey)
    {
        roleKey = SafeTrim(roleKey);

        if (string.IsNullOrEmpty(roleKey))
            return false;

        if (!_roleToCharacter.TryGetValue(roleKey, out string characterKey))
            return false;

        _roleToCharacter.Remove(roleKey);

        if (!string.IsNullOrEmpty(characterKey))
            _characterToRole.Remove(characterKey);

        return true;
    }

    public void Clear()
    {
        _roleToCharacter.Clear();
        _characterToRole.Clear();
    }

    private bool UncastCharacterInternal(string characterKey)
    {
        characterKey = SafeTrim(characterKey);

        if (string.IsNullOrEmpty(characterKey))
            return false;

        if (!_characterToRole.TryGetValue(characterKey, out string roleKey))
            return false;

        _characterToRole.Remove(characterKey);

        if (!string.IsNullOrEmpty(roleKey))
            _roleToCharacter.Remove(roleKey);

        return true;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}