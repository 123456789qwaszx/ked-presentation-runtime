// using System;
// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
//
// [Serializable]
// [CommandMenuHint(
//     "Presentation Motion",
//     "Slanted Mask Slide In",
//     Order = -899)]
// public sealed class SlantedMaskSlideInCommandSpec : CommandSpecBase
// {
//     [Header("Shape")]
//     public Vector2 fromOffset = new Vector2(-2200f, 0f);
//     public Vector2 toOffset = new Vector2(-770f, 0f);
//
//     [Header("Mask Shape Fixed Options")]
//     public bool slantToRight = false;
//     public bool flipVertical = true;
//
//     [Header("Tween")]
//     public float duration = 0.65f;
//     public Ease ease = Ease.OutCubic;
//
//     [Header("Rubber End")]
//     [Tooltip("끝부분에서 진행 방향으로 살짝 지나쳤다가 목적지로 돌아오는 거리입니다.")]
//     public float overshootPixels = 72f;
//
//     [Tooltip("오버슛이 시작되는 진행률입니다. 0.75면 마지막 25% 구간에서 고무줄처럼 처리됩니다.")]
//     [Range(0.01f, 0.99f)]
//     public float overshootStart = 0.72f;
// }
//
// public sealed class SlantedMaskSlideInCommand : CommandBase
// {
//     private readonly SlantedMaskSlideInCommandSpec _spec;
//
//     private SlantedMaskGraphic _maskGraphic;
//
//     private bool _resolveAttempted;
//
//     private bool HasClaimedTarget { get; set; }
//
//     public override bool WaitForCompletion => _spec.wait;
//
//     public SlantedMaskSlideInCommand(SlantedMaskSlideInCommandSpec spec)
//     {
//         _spec = spec;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         if (!_resolveAttempted)
//             ResolveRefs(scope);
//
//         if (_maskGraphic == null)
//             yield break;
//
//         ClaimTarget();
//
//         if (scope.IsSeekPassThrough || _spec.duration <= 0f)
//         {
//             CommitFinalState();
//             yield break;
//         }
//
//         Vector2 start = _spec.fromOffset;
//         Vector2 dest = _spec.toOffset;
//
//         Vector2 moveDir = dest - start;
//         moveDir = moveDir.sqrMagnitude > 0f
//             ? moveDir.normalized
//             : Vector2.right;
//
//         _maskGraphic.ShapeOffsetPixels = start;
//
//         Tween tween = DOTween
//             .To(
//                 () => 0f,
//                 t =>
//                 {
//                     float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
//
//                     Vector2 baseOffset = Vector2.LerpUnclamped(start, dest, e);
//                     float rubber = RubberOvershootEnd(e, _spec.overshootStart);
//
//                     _maskGraphic.ShapeOffsetPixels =
//                         baseOffset + moveDir * (_spec.overshootPixels * rubber);
//                 },
//                 1f,
//                 _spec.duration)
//             .SetEase(Ease.Linear)
//             .SetUpdate(true)
//             .SetTarget(_maskGraphic)
//             .OnComplete(CommitFinalState);
//
//         if (_spec.wait)
//             yield return tween.WaitForCompletion();
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         if (!_resolveAttempted)
//             ResolveRefs(scope);
//
//         if (_maskGraphic == null)
//             return;
//
//         if (!HasClaimedTarget)
//             ClaimTarget();
//
//         CommitFinalState();
//     }
//
//     private void ResolveRefs(CommandRunScope scope)
//     {
//         _resolveAttempted = true;
//
//         IPresentationTransitionSlotProvider transitionSlotProvider =
//             UIManager.Instance.GetUI<PresentationUIRoot>();
//
//         RectTransform rect = transitionSlotProvider.SlantedMaskEdgeGraphic;
//         _maskGraphic = rect.GetComponent<SlantedMaskGraphic>();
//     }
//
//     private void ClaimTarget()
//     {
//         DOTween.Kill(_maskGraphic, true);
//         ApplyFixedMaskOptions();
//
//         HasClaimedTarget = true;
//     }
//
//     private void CommitFinalState()
//     {
//         ApplyFixedMaskOptions();
//         _maskGraphic.ShapeOffsetPixels = _spec.toOffset;
//
//         HasClaimedTarget = false;
//     }
//
//     private void ApplyFixedMaskOptions()
//     {
//         _maskGraphic.SlantToRight = _spec.slantToRight;
//         _maskGraphic.FlipVertical = _spec.flipVertical;
//     }
//
//     private static float RubberOvershootEnd(float e, float overshootStart)
//     {
//         e = Mathf.Clamp01(e);
//         overshootStart = Mathf.Clamp(overshootStart, 0.01f, 0.99f);
//
//         if (e < overshootStart)
//             return 0f;
//
//         float t = Mathf.InverseLerp(overshootStart, 1f, e);
//         return Mathf.Sin(t * Mathf.PI);
//     }
// }