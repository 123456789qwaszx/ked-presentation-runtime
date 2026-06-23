// using UnityEngine;
//
// public static class BackgroundRigRootMaskParser
// {
//     public static BackgroundRigRootMask Parse(
//         string value,
//         BackgroundRigRootMask fallback = BackgroundRigRootMask.All)
//     {
//         if (string.IsNullOrWhiteSpace(value))
//             return fallback;
//
//         string normalized = value.Trim().ToLowerInvariant();
//
//         if (TryParseSingle(normalized, out BackgroundRigRootMask singleMask))
//             return singleMask;
//
//         BackgroundRigRootMask mask = ParseComposite(value);
//
//         if (mask != BackgroundRigRootMask.None)
//             return mask;
//
//         Debug.LogWarning(
//             $"[BackgroundRigRootMaskParser] Unknown background root mask '{value}'. Fallback to {fallback}.");
//
//         return fallback;
//     }
//
//     private static BackgroundRigRootMask ParseComposite(string value)
//     {
//         BackgroundRigRootMask mask = BackgroundRigRootMask.None;
//         string[] tokens = value.Split('|', ',', '+');
//
//         for (int i = 0; i < tokens.Length; i++)
//         {
//             string token = tokens[i].Trim().ToLowerInvariant();
//
//             if (TryParseSingle(token, out BackgroundRigRootMask tokenMask))
//                 mask |= tokenMask;
//         }
//
//         return mask;
//     }
//
//     private static bool TryParseSingle(string value, out BackgroundRigRootMask mask)
//     {
//         switch (value)
//         {
//             case "visual":
//             case "visual_layers":
//             case "layers":
//                 mask = BackgroundRigRootMask.All;
//                 return true;
//
//             case "all":
//                 mask = BackgroundRigRootMask.All;
//                 return true;
//
//             case "root":
//                 mask = BackgroundRigRootMask.Background_Root;
//                 return true;
//
//             case "sprite":
//             case "sprite_root":
//                 mask = BackgroundRigRootMask.BackgroundSprite_Root;
//                 return true;
//
//             case "object":
//             case "objects":
//             case "object_slots":
//                 mask = BackgroundRigRootMask.Background_ObjectSlotRoot;
//                 return true;
//
//         }
//
//         mask = BackgroundRigRootMask.None;
//         return false;
//     }
// }