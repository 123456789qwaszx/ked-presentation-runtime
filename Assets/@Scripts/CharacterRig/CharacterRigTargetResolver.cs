using System;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRigFromTargetKey(CommandRunScope scope, string targetKey)
    {
        string resolvedRigKey = ResolveRigKeyByPolicy(scope, targetKey);
        
        if (!scope.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rig))
        {
            throw new InvalidOperationException(
                $"[CharacterRigTargetResolver] Failed to resolve CharacterRigRefs. " +
                $"targetKey='{targetKey}', resolvedRigKey='{resolvedRigKey}'.");
        }

        return rig;
    }
    
    public static string ResolveRigKeyByPolicy(CommandRunScope scope, string targetKey)
    {
        // targetKey policy:
        // 1. If targetKey is a characterKey, resolve it to that character's current slotKey.
        // 2. Otherwise, use targetKey itself as a direct slotKey.
        if (scope.castRegistry.TryGetSlotKey(targetKey, out string characterSlotKey))
            return characterSlotKey;

        return targetKey;
    }
}