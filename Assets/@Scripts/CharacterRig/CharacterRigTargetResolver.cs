using System;
using UnityEngine;

public static class CharacterRigTargetResolver
{
    public static CharacterRigRefs ResolveCharRigFromTargetKey(CommandRunScope scope, string targetKey)
    {
        string resolvedRigKey = ResolveRigKeyByPolicy(scope, targetKey);
        
        if (!TryGetRigRefsByKey(scope, resolvedRigKey, out CharacterRigRefs rig))
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
        if (scope.CastRegistry.TryGetSlotKey(targetKey, out string characterSlotKey))
            return characterSlotKey;

        return targetKey;
    }
    
    
    public static bool TryGetRigRefsByKey(CommandRunScope scope, string rigKey, out CharacterRigRefs rigRefs)
    {
        if (!scope.Refs.TryGetValue(rigKey, out object obj))
        {
            Debug.LogWarning(
                $"[CharacterRigTargetResolver] CharacterRigRefs not found. " +
                $"rigKey='{rigKey}'.");
        
            rigRefs = null;
            return false;
        }

        if (obj is not CharacterRigRefs refs)
        {
            string actualType = obj != null ? obj.GetType().Name : "null";

            Debug.LogWarning(
                $"[CharacterRigTargetResolver] Invalid refs type. " +
                $"Expected CharacterRigRefs. " +
                $"rigKey='{rigKey}', actualType='{actualType}'.");

            rigRefs = null;
            return false;
        }

        rigRefs = refs;
        return true;
    }
}