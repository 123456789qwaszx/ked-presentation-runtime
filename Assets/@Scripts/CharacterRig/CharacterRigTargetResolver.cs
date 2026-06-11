using UnityEngine;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRigFromTargetKey(CommandRunScope scope, string targetKey)
    {
        string resolvedRigKey = ResolveRigKeyByPolicy(scope, targetKey);
        
        if (!scope.CharacterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rig))
        {
            Debug.LogWarning(
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
        if (scope.CastRegistry.TryGetSlotKey(targetKey, out string characterSlotKey))
            return characterSlotKey;

        return targetKey;
    }
    
    public static string ResolveCharacterKeyFromTargetKey(CommandRunScope scope, string targetKey)
    {
        string resolvedRigKey = ResolveRigKeyByPolicy(scope, targetKey);

        if (!scope.CastRegistry.TryGetCharacter(resolvedRigKey, out string characterKey))
        {
            Debug.LogWarning(
                $"[CharacterRigTargetResolver] Failed to get characterKey. return slotKey as fallback. " +
                $"targetKey='{targetKey}', resolvedRigKey='{resolvedRigKey}'.");
            return targetKey;
        }
        
        return characterKey;
    }
}