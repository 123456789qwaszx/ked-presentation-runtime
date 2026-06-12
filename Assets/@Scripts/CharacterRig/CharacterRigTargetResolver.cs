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
        targetKey = ResolveAlias(scope, targetKey);

        if (scope.CastRegistry.TryGetSlotKey(targetKey, out string characterSlotKey))
            return characterSlotKey;

        return targetKey;
    }
    
    public static string ResolveCharacterKeyFromTargetKey(CommandRunScope scope, string targetKey)
    {
        string resolvedTargetKey = ResolveAlias(scope, targetKey);
        string resolvedRigKey = ResolveRigKeyByPolicy(scope, resolvedTargetKey);

        if (!scope.CastRegistry.TryGetCharacter(resolvedRigKey, out string characterKey))
        {
            Debug.LogWarning(
                $"[CharacterRigTargetResolver] Failed to get characterKey. return resolved targetKey as fallback. " +
                $"targetKey='{targetKey}', resolvedTargetKey='{resolvedTargetKey}', resolvedRigKey='{resolvedRigKey}'.");

            return resolvedTargetKey;
        }
        
        return characterKey;
    }

    private static string ResolveAlias(CommandRunScope scope, string targetKey)
    {
        return scope.CharacterTargetAliases.Resolve(targetKey);
    }
}