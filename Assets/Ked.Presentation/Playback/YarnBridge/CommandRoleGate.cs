using System;
using System.Collections.Generic;

public sealed class CommandRoleGate
{
    private readonly PresentationStage _stage;

    private readonly Dictionary<string, string> _pendingAliases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingCharacters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingSlots = new(StringComparer.Ordinal);

    public CommandRoleGate(PresentationStage stage)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
    }

    public bool TryAccept(
        IReadOnlyList<CommandSpecBase> pending,
        IReadOnlyList<CommandSpecBase> specs,
        out string reason)
    {
        _pendingAliases.Clear();
        _pendingCharacters.Clear();
        _pendingSlots.Clear();

        for (int i = 0; i < pending.Count; i++)
            Declare(pending[i]);

        for (int i = 0; i < specs.Count; i++)
        {
            if (TryGetRoleKey(specs[i], out string roleKey) && !CanResolve(roleKey))
            {
                reason = string.IsNullOrEmpty(roleKey)
                    ? "대상 인자가 없다"
                    : $"대상 '{roleKey}'를 슬롯으로 풀 수 없다 (slot/cast/actor 선행 필요)";

                return false;
            }

            Declare(specs[i]);
        }

        reason = null;
        return true;
    }

    private static bool TryGetRoleKey(CommandSpecBase spec, out string roleKey)
    {
        switch (spec)
        {
            case DepthFocusCommandSpecCharR _:
                break;

            case CharacterRigCommandSpecBase charRig:
                roleKey = charRig.roleKey;
                return true;

            case CastCharacterCommandSpec cast:
                roleKey = cast.roleKey;
                return true;

            case ShotZoomFocusCommandSpec shotFocus:
                roleKey = shotFocus.focusRoleKey;
                return true;
        }

        roleKey = null;
        return false;
    }

    private void Declare(CommandSpecBase spec)
    {
        switch (spec)
        {
            case SetupCharRigCommandSpec setup:
                _pendingSlots.Add(setup.roleKey ?? "");
                break;

            case CastCharacterCommandSpec cast:
                _pendingCharacters.Add(PresentationKeyNormalizer.NormalizeCharacterKey(cast.characterKey));
                break;

            case SetPresentationActorAliasCommandSpec actor:
                _pendingAliases[actor.aliasSymbol ?? ""] = actor.roleKey;
                break;
        }
    }

    private bool CanResolve(string roleKey)
    {
        if (string.IsNullOrEmpty(roleKey))
            return false;

        string key = ResolveAlias(roleKey);

        return _pendingCharacters.Contains(PresentationKeyNormalizer.NormalizeCharacterKey(key))
               || _pendingSlots.Contains(key)
               || _stage.castRegistry.TryGetSlotKey(key, out _)
               || _stage.characterRigs.TryPeekRig(key, out _);
    }

    private string ResolveAlias(string roleKey)
    {
        if (_pendingAliases.TryGetValue(roleKey, out string aliased) && !string.IsNullOrEmpty(aliased))
            return aliased;

        if (_stage.roleAliases.TryPeek(roleKey, out aliased) && !string.IsNullOrEmpty(aliased))
            return aliased;

        return roleKey;
    }
}
