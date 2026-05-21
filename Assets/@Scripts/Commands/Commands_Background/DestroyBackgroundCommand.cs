// using System;
// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
// using Object = UnityEngine.Object;
//
// [Serializable]
// [CommandMenuHint("Presentation Background", "Destroy Background", Order = -880)]
// public sealed class DestroyBackgroundCommandSpec : CommandSpecBase
// {
//     [Header("Identity")]
//     [Tooltip("파괴할 대상 배경 RectTransformResponseTarget을 찾을 bgKey")]
//     public string bgKey = "current";
//
//     [Header("Options")]
//     [Tooltip("체크하면 기존 트윈을 끝내고 committed state에서 파괴합니다.")]
//     public bool killTween = true;
//
//     [Tooltip("scope.Refs에서 해당 bgKey 엔트리를 제거합니다.")]
//     public bool removeRefEntry = true;
//
//     [Tooltip("필수 계약이 없으면 예외를 던질지")]
//     public bool strict = true;
// }
//
// public sealed class DestroyBackgroundCommand : CommandBase
// {
//     private readonly DestroyBackgroundCommandSpec _spec;
//     private readonly IBGRuntimeRegistry _runtimeRegistry;
//     private readonly PresentationResponseRig _responseRig;
//
//     private RectTransformResponseTarget _background;
//     private bool _resolveAttempted;
//
//     public override bool WaitForCompletion => true;
//
//     public DestroyBackgroundCommand(
//         DestroyBackgroundCommandSpec spec,
//         IBGRuntimeRegistry runtimeRegistry = null,
//         PresentationResponseRig responseRig = null)
//     {
//         _spec = spec;
//         _runtimeRegistry = runtimeRegistry;
//         _responseRig = responseRig;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         DestroyBackground(scope);
//         yield break;
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         DestroyBackground(scope);
//     }
//
//     protected override void OnRollbackSeek(CommandRunScope scope)
//     {
//         DestroyBackground(scope);
//     }
//
//     private void DestroyBackground(CommandRunScope scope)
//     {
//         if (!_resolveAttempted)
//             ResolveRefs(scope);
//
//         if (_background == null)
//             return;
//
//         if (_spec.killTween)
//             KillTweens(_background);
//
//         _responseRig?.RemoveBinding(_spec.bgKey);
//
//         if (_runtimeRegistry != null)
//             _runtimeRegistry.DestroyRuntimeBackground(_spec.bgKey);
//         else
//             DestroyGameObject(_background);
//
//         if (_spec.removeRefEntry && scope != null && scope.Refs != null)
//             scope.Refs.Remove(_spec.bgKey);
//
//         _background = null;
//     }
//
//     private void ResolveRefs(CommandRunScope scope)
//     {
//         _resolveAttempted = true;
//
//         if (scope == null || scope.Refs == null)
//         {
//             if (_spec.strict)
//                 throw new InvalidOperationException($"[DestroyBackgroundCommand] Refs is null. bgKey={_spec.bgKey}");
//
//             return;
//         }
//
//         if (!scope.Refs.TryGetValue(_spec.bgKey, out object obj) ||
//             obj is not RectTransformResponseTarget background)
//         {
//             if (_spec.strict)
//                 throw new InvalidOperationException(
//                     $"[DestroyBackgroundCommand] Background target not found. bgKey={_spec.bgKey}");
//
//             return;
//         }
//
//         _background = background;
//
//         if (_background == null && _spec.strict)
//         {
//             throw new InvalidOperationException(
//                 $"[DestroyBackgroundCommand] Background target is null. bgKey={_spec.bgKey}");
//         }
//     }
//
//     private static void KillTweens(RectTransformResponseTarget background)
//     {
//         if (background == null)
//             return;
//
//         RectTransform rect = background.transform as RectTransform;
//         if (rect != null)
//             rect.DOKill(true);
//
//         CanvasGroup canvasGroup = background.GetComponent<CanvasGroup>();
//         if (canvasGroup != null)
//             canvasGroup.DOKill(true);
//     }
//
//     private static void DestroyGameObject(RectTransformResponseTarget background)
//     {
//         if (background == null)
//             return;
//
//         Object.Destroy(background.gameObject);
//     }
// }