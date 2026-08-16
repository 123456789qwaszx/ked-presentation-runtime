// using UnityEngine;
//
// public sealed class StageOverlayRigSlotResolver
// {
//     private readonly IStageOverlayRigSlotProvider _slotProvider;
//
//     public StageOverlayRigSlotResolver(IStageOverlayRigSlotProvider slotProvider)
//     {
//         _slotProvider = slotProvider;
//     }
//
//     public bool TryResolve(
//         StageOverlayRigRootKind kind,
//         out RectTransform rect)
//     {
//         if (_slotProvider == null)
//         {
//             rect = null;
//             Debug.LogWarning(
//                 $"[StageOverlayRigSlotResolver] Missing overlay rig slot provider. kind='{kind}'.");
//             return false;
//         }
//
//         rect = _slotProvider.GetStageOverlayRigRoot(kind);
//
//         if (rect == null)
//         {
//             Debug.LogWarning(
//                 $"[StageOverlayRigSlotResolver] Overlay rig root is null. kind='{kind}'.");
//             return false;
//         }
//
//         return true;
//     }
// }