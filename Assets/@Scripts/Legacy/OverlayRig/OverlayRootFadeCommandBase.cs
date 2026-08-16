// using DG.Tweening;
// using UnityEngine;
//
// /// <summary>
// /// 오버레이 루트 CanvasGroup 페이드의 공통 수명 — Show/Hide가 이것 하나다.
// /// 다른 건 목표 alpha 하나뿐이다.
// ///
// /// 캐릭터·배경의 CanvasFadeCommandBase와 달리 CanvasGroup을 직접 만지지 않고
// /// 리그의 Kill/SetImmediate를 거친다 (오버레이는 리그가 트윈을 소유한다).
// /// </summary>
// public abstract class OverlayRootFadeCommandBase : ClaimTweenCommandBase
// {
//     private OverlayRigRefs _refs;
//     private float _startAlpha;
//
//     protected abstract string RigKey { get; }
//     protected abstract Ease FadeEase { get; }
//     protected abstract float TargetAlpha { get; }
//
//     // 스텝 경계에서는 가속하지 않고 곧장 확정한다 — 페이드는 종전부터 그랬다.
//     protected override bool AccelerateOnStepFinish => false;
//
//     protected override bool TryResolveTargets(CommandRunScope scope)
//     {
//         scope.OverlayRigs.TryGet(RigKey, out _refs);
//
//         return _refs?.Overlay_RootCanvasGroup != null;
//     }
//
//     protected override void ClaimTarget(CommandRunScope scope)
//     {
//         _refs.KillRootCanvasTween(true);
//
//         _startAlpha = _refs.Overlay_RootCanvasGroup.alpha;
//     }
//
//     protected override Tween CreateTween(float duration)
//         => _refs.Overlay_RootCanvasGroup
//             .DOFade(TargetAlpha, duration)
//             .SetEase(FadeEase)
//             .SetTarget(_refs.Overlay_RootCanvasGroup);
//
//     protected override void OnCommitFinalState()
//     {
//         _refs.SetRootAlphaImmediate(TargetAlpha);
//     }
//
//     // AccelerateOnStepFinish = false라 불리지 않지만, 계약은 정직하게 채운다.
//     protected override float MeasureRemainingRatio()
//         => RemainingRatio(
//             Mathf.Abs(TargetAlpha - _startAlpha),
//             Mathf.Abs(TargetAlpha - _refs.Overlay_RootCanvasGroup.alpha));
// }
