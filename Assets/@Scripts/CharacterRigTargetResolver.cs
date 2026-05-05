using System;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRig(CommandRunScope scope, string targetKey)
    {
        // targetKey policy:
        // 1. If targetKey is a cast characterKey, use its bound roleKey.
        // 2. Otherwise, use targetKey itself as roleKey/slotKey.
        string resolvedRoleKey = targetKey;

        if (scope.CastRegistry.TryGetRole(targetKey, out string boundRoleKey))
            resolvedRoleKey = boundRoleKey;

        if (!scope.Refs.TryGetCharRigRefs(resolvedRoleKey, out CharacterRigRefs rig) || rig == null)
        {
            throw new InvalidOperationException(
                $"[CharacterRigTargetResolver] CharacterRigRefs not found. targetKey='{targetKey}', resolvedRoleKey='{resolvedRoleKey}'.");
        }

        return rig;
    }
}