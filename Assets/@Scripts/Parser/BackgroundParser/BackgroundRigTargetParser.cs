// using UnityEngine;
//
// public static class BackgroundRigTargetParser
// {
//     public static BackgroundRigTarget ParseFadeTarget(string value)
//     {
//         string normalized = (value ?? "").Trim().ToLowerInvariant();
//
//         switch (normalized)
//         {
//             case "":
//             case "root":
//             case "all":
//             case "bg":
//             case "background":
//                 return BackgroundRigTarget.Background_Root;
//
//             case "back":
//             case "b":
//             case "backlayer":
//             case "back_layer":
//                 return BackgroundRigTarget.Background_BackLayer_Root;
//
//             case "front":
//             case "f":
//             case "frontlayer":
//             case "front_layer":
//                 return BackgroundRigTarget.Background_FrontLayer_Root;
//
//             case "layer":
//             case "layers":
//             case "layerroot":
//             case "layer_root":
//                 return BackgroundRigTarget.Background_LayerRoot;
//
//             default:
//                 Debug.LogWarning(
//                     $"[BackgroundRigTargetParser] Unknown fade target '{value}'. " +
//                     $"Fallback to '{BackgroundRigTarget.Background_Root}'.");
//                 return BackgroundRigTarget.Background_Root;
//         }
//     }
// }