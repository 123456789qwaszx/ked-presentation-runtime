using System;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRigFromTargetKey(CommandRunScope scope, string targetKey)
    {
        // targetKey policy:
        // 1. If targetKey is a cast characterKey, use its bound slotKey.
        // 2. Otherwise, use targetKey itself as slotKey.
        string resolvedSlotKey = targetKey;

        if (scope.CastRegistry.TryGetSlotKey(targetKey, out string boundSlotKey))
            resolvedSlotKey = boundSlotKey;

        if (!scope.Refs.TryGetCharRigRefs(resolvedSlotKey, out CharacterRigRefs rig) || rig == null)
        {
            throw new InvalidOperationException(
                $"[CharacterRigTargetResolver] CharacterRigRefs not found. targetKey='{targetKey}', resolvedRoleKey='{resolvedSlotKey}'.");
        }

        return rig;
    }
    
    public static string ResolveSlotKeyFromTargetKey(CommandRunScope scope, string targetKey)
    {
        if (scope.CastRegistry.TryGetSlotKey(targetKey, out string boundRoleKey))
            return boundRoleKey;

        return targetKey;
    }
}