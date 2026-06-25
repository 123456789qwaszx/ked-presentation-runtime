// using System;
// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
//
// [Serializable]
// [CommandMenuHint(
//     "Presentation Motion",
//     "Slanted Mask Slide Out",
//     Order = -898)]
// public sealed class SlantedMaskSlideOutCommandSpec : CommandSpecBase
// {
//     [Header("Shape")]
//     public Vector2 fromOffset = new Vector2(-770f, 0f);
//     public Vector2 toOffset = new Vector2(-2200f, 0f);
//
//     [Header("Mask Shape Fixed Options")]
//     public bool slantToRight = false;
//     public bool flipVertical = true;
//
//     [Header("Tween")]
//     public float duration = 0.45f;
//     public Ease ease = Ease.InCubic;
//
//     [Header("Rubber Start")]
//     [Tooltip("시작할 때 반대 방향으로 살짝 당겼다가 빠져나가는 거리입니다.")]
//     public float pullPixels = 24f;
//
//     [Tooltip("당김이 사라지는 진행률입니다. 0.25면 초반 25% 구간에서만 당김이 적용됩니다.")]
//     [Range(0.01f, 0.99f)]
//     public float pullEnd = 0.28f;
// }
//
// public sealed class SlantedMaskSlideOutCommand : CommandBase
// {
//     private readonly SlantedMaskSlideOutCommandSpec _spec;
//
//     private SlantedMaskGraphic _maskGraphic;
//
//     private bool _resolveAttempted;
//
//     private bool HasClaimedTarget { get; set; }
//
//     public override bool WaitForCompletion => _spec.wait;
//
//     public SlantedMaskSlideOutCommand(SlantedMaskSlideOutCommandSpec spec)
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
//             : Vector2.left;
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
//                     float pull = RubberPullStart(e, _spec.pullEnd);
//
//                     _maskGraphic.ShapeOffsetPixels =
//                         baseOffset - moveDir * (_spec.pullPixels * pull);
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
//
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
//     private static float RubberPullStart(float e, float pullEnd)
//     {
//         e = Mathf.Clamp01(e);
//         pullEnd = Mathf.Clamp(pullEnd, 0.01f, 0.99f);
//
//         if (e >= pullEnd)
//             return 0f;
//
//         float t = Mathf.InverseLerp(0f, pullEnd, e);
//         return 1f - Mathf.SmoothStep(0f, 1f, t);
//     }
// }