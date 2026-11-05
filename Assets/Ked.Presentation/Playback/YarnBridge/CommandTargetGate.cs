using System;
using System.Collections.Generic;

public sealed class CommandTargetGate
{
    private readonly PresentationStage _stage;

    private readonly Dictionary<string, string> _pendingAliases = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingCharacters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pendingSlots = new(StringComparer.Ordinal);

    public CommandTargetGate(PresentationStage stage)
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
            if (TryGetTargetKey(specs[i], out string targetKey) && !CanResolve(targetKey))
            {
                reason = string.IsNullOrEmpty(targetKey)
                    ? "대상 인자가 없다"
                    : $"대상 '{targetKey}'를 슬롯으로 풀 수 없다 (slot/cast/actor 선행 필요)";

                return false;
            }

            Declare(specs[i]);
        }

        reason = null;
        return true;
    }

    private static bool TryGetTargetKey(CommandSpecBase spec, out string targetKey)
    {
        switch (spec)
        {
            case DepthFocusCommandSpecCharR _:
                break;

            case CharacterRigCommandSpecBase charRig:
                targetKey = charRig.slotKey;
                return true;

            case CastCharacterCommandSpec cast:
                targetKey = cast.slotKey;
                return true;

            case ShotZoomFocusCommandSpec shotFocus:
                targetKey = shotFocus.focusRoleKey;
                return true;
        }

        targetKey = null;
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
                _pendingAliases[actor.aliasSymbol ?? ""] = actor.targetKey;
                break;
        }
    }

    private bool CanResolve(string targetKey)
    {
        if (string.IsNullOrEmpty(targetKey))
            return false;

        string key = ResolveAlias(targetKey);

        return _pendingCharacters.Contains(PresentationKeyNormalizer.NormalizeCharacterKey(key))
               || _pendingSlots.Contains(key)
               || _stage.castRegistry.TryGetSlotKey(key, out _)
               || _stage.characterRigs.TryPeekRig(key, out _);
    }

    private string ResolveAlias(string targetKey)
    {
        if (_pendingAliases.TryGetValue(targetKey, out string aliased) && !string.IsNullOrEmpty(aliased))
            return aliased;

        if (_stage.characterTargetAliases.TryPeekTarget(targetKey, out aliased) && !string.IsNullOrEmpty(aliased))
            return aliased;

        return targetKey;
    }
}
