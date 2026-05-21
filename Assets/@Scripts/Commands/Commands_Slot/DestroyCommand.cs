// using System;
// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
// using UnityEngine.UI;
// using Object = UnityEngine.Object;
//
// [Serializable]
// [CommandMenuHint(
//     "Other", "Destroy Rig", Order = 900)]
// public sealed class DestroyCommandSpec : CharacterRigCommandSpecBase
// {
//     [Header("Destroy")]
//     [Tooltip("Destroy 전에 대상 Rig 하위 Tween을 정리합니다.")]
//     public bool killTween = true;
//
//     [Tooltip("Destroy 후 scope.Refs에서 roleKey 항목을 제거합니다. 끄면 null로만 세팅합니다.")]
//     public bool removeRefKey = false;
// }
//
// public sealed class DestroyCommand : CommandBase
// {
//     private readonly DestroyCommandSpec _spec;
//
//     public override bool WaitForCompletion => _spec.wait;
//
//     public DestroyCommand(DestroyCommandSpec spec)
//     {
//         _spec = spec;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         Apply(scope);
//         yield break;
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         Apply(scope);
//     }
//
//     protected override void OnRollbackSeek(CommandRunScope scope)
//     {
//         OnSkip(scope);
//     }
//
//     private void Apply(CommandRunScope scope)
//     {
//         if (scope == null || scope.Refs == null)
//             return;
//
//         string resolvedRoleKey =
//             CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, _spec.slotKey);
//
//         if (string.IsNullOrEmpty(resolvedRoleKey))
//             return;
//
//         if (!scope.Refs.TryGetCharRigRefs(resolvedRoleKey, out CharacterRigRefs rigRefs))
//             return;
//
//         RectTransform root = ResolveRoot(rigRefs);
//         if (root == null)
//         {
//             ClearRef(scope, resolvedRoleKey);
//             return;
//         }
//
//         if (_spec.killTween)
//             KillTweenBeforeDestroy(root, resolvedRoleKey);
//
//         Object.Destroy(root.gameObject);
//
//         ClearRef(scope, resolvedRoleKey);
//     }
//
//     private void ClearRef(CommandRunScope scope, string roleKey)
//     {
//         if (scope == null || scope.Refs == null)
//             return;
//
//         if (string.IsNullOrEmpty(roleKey))
//             return;
//
//         if (_spec.removeRefKey)
//             scope.Refs.Remove(roleKey);
//         else
//             scope.Refs[roleKey] = null;
//     }
//
//     private static RectTransform ResolveRoot(CharacterRigRefs rigRefs)
//     {
//         if (rigRefs == null)
//             return null;
//
//         return rigRefs.RigRoot;
//     }
//
//     private static void KillTweenBeforeDestroy(RectTransform root, string roleKey)
//     {
//         if (root == null)
//             return;
//
//         if (!string.IsNullOrEmpty(roleKey))
//             DOTween.Kill($"CharPortraitWipe:{roleKey}", false);
//
//         KillTweenOnHierarchy(root);
//     }
//
//     private static void KillTweenOnHierarchy(Transform root)
//     {
//         if (root == null)
//             return;
//
//         RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
//         for (int i = 0; i < rects.Length; i++)
//         {
//             if (rects[i] != null)
//                 rects[i].DOKill(false);
//         }
//
//         CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
//         for (int i = 0; i < canvasGroups.Length; i++)
//         {
//             if (canvasGroups[i] != null)
//                 canvasGroups[i].DOKill(false);
//         }
//
//         Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
//         for (int i = 0; i < graphics.Length; i++)
//         {
//             if (graphics[i] != null)
//                 graphics[i].DOKill(false);
//         }
//
//         DOTween.Kill(root, false);
//         DOTween.Kill(root.gameObject, false);
//     }
// }