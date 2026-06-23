// using System;
// using System.Collections;
// using System.Collections.Generic;
// using DG.Tweening;
// using UnityEngine;
//
// [Serializable]
// [CommandMenuHint(
//     "Background Rig",
//     "#Show Root Layers",
//     Order = -939
// )]
// public sealed class ShowRootLayersCommandSpecBgR : BackgroundRigCommandSpecBase
// {
//     public BackgroundRigRootMask targetMask = BackgroundRigRootMask.VisualLayers;
// }
//
// public sealed class ShowRootLayersCommandBgR : CommandBase
// {
//     private readonly ShowRootLayersCommandSpecBgR _spec;
//
//     private readonly List<RectTransform> _targets = new();
//     private bool _resolveAttempted;
//
//     public ShowRootLayersCommandBgR(ShowRootLayersCommandSpecBgR spec)
//     {
//         _spec = spec;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         if (!_resolveAttempted)
//             ResolveRefs(scope);
//
//         Apply();
//         yield break;
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         if (!_resolveAttempted)
//             ResolveRefs(scope);
//
//         Apply();
//     }
//
//     private void ResolveRefs(CommandRunScope scope)
//     {
//         _resolveAttempted = true;
//         
//         BackgroundRigRefs rig = BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);
//         BackgroundRigRootSelector.CollectRects(rig, _spec.targetMask, _targets);
//     }
//
//     private void Apply()
//     {
//         for (int i = 0; i < _targets.Count; i++)
//         {
//             CanvasGroup canvasGroup = _targets[i].GetComponent<CanvasGroup>();
//
//             canvasGroup.DOKill(true);
//             
//             canvasGroup.alpha = 1f;
//             canvasGroup.interactable = true;
//             canvasGroup.blocksRaycasts = true;
//         }
//     }
// }