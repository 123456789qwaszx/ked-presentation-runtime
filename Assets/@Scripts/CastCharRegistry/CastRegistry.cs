using System.Collections.Generic;

public sealed class CastRegistry
{
    private readonly Dictionary<string, string> _roleToCharacter = new();
    private readonly Dictionary<string, string> _characterToRole = new();

    public bool TryGetCharacter(string roleKey, out string characterKey)
        => _roleToCharacter.TryGetValue(roleKey, out characterKey);

    public bool TryGetRole(string characterKey, out string roleKey)
        => _characterToRole.TryGetValue(characterKey, out roleKey);

    public void Bind(string roleKey, string characterKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey) || string.IsNullOrWhiteSpace(characterKey))
            return;

        if (_roleToCharacter.TryGetValue(roleKey, out string oldCharacter))
        {
            if (!string.IsNullOrEmpty(oldCharacter))
                _characterToRole.Remove(oldCharacter);
        }

        if (_characterToRole.TryGetValue(characterKey, out string oldRole))
        {
            if (!string.IsNullOrEmpty(oldRole))
                _roleToCharacter.Remove(oldRole);
        }

        _roleToCharacter[roleKey] = characterKey;
        _characterToRole[characterKey] = roleKey;
    }

    public void UnbindRole(string roleKey)
    {
        if (!_roleToCharacter.TryGetValue(roleKey, out string characterKey))
            return;

        _roleToCharacter.Remove(roleKey);
        if (!string.IsNullOrEmpty(characterKey))
            _characterToRole.Remove(characterKey);
    }

    public void UnbindCharacter(string characterKey)
    {
        if (!_characterToRole.TryGetValue(characterKey, out string roleKey))
            return;

        _characterToRole.Remove(characterKey);
        if (!string.IsNullOrEmpty(roleKey))
            _roleToCharacter.Remove(roleKey);
    }
}