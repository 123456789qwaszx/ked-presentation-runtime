// using System;
// using DG.Tweening;
// using UnityEngine;
//
// [Serializable]
// [CommandMenuHint(
//     "Stage Overlay",
//     "Overlay Size",
//     Order = -939)]
// public sealed class OverlaySizeCommandSpec : CommandSpecBase
// {
//     [Header("Overlay")]
//     public string rigKey;
//
//     [Header("Target")]
//     public OverlayRigTarget target = OverlayRigTarget.Overlay_Size;
//
//     [Header("Size")]
//     public bool relativeToCurrent = false;
//     public Vector2 sizeDelta = Vector2.zero;
//
//     [Header("Tween")]
//     public float duration = 0f;
//     public Ease ease = Ease.OutCubic;
// }
//
// public sealed class OverlaySizeCommand : ClaimTweenCommandBase
// {
//     private readonly OverlaySizeCommandSpec _spec;
//
//     private OverlayRigRefs _refs;
//     private RectTransform _rect;
//
//     private Vector2 _startSize;
//     private Vector2 _destSize;
//
//     public override bool WaitForCompletion => _spec.wait;
//
//     protected override float TweenDuration => _spec.duration;
//
//     public OverlaySizeCommand(OverlaySizeCommandSpec spec)
//     {
//         _spec = spec;
//     }
//
//     protected override float ResolvePlaybackDuration(CommandRunScope scope)
//         => scope.ScalePresentationDuration(_spec.duration);
//
//     protected override bool TryResolveTargets(CommandRunScope scope)
//     {
//         if (!scope.OverlayRigs.TryGet(_spec.rigKey, out _refs))
//             return false;
//
//         _rect = _refs.GetRect(_spec.target);
//
//         return _rect != null;
//     }
//
//     protected override void ClaimTarget(CommandRunScope scope)
//     {
//         _refs.KillTween(_spec.target, true);
//
//         _startSize = _rect.sizeDelta;
//         _destSize = _spec.relativeToCurrent
//             ? _startSize + _spec.sizeDelta
//             : _spec.sizeDelta;
//     }
//
//     protected override Tween CreateTween(float duration)
//         => _rect
//             .DOSizeDelta(_destSize, duration)
//             .SetEase(_spec.ease)
//             .SetTarget(_rect);
//
//     protected override void OnCommitFinalState()
//     {
//         _refs.SetSizeDeltaImmediate(_spec.target, _destSize);
//     }
//
//     protected override float MeasureRemainingRatio()
//         => RemainingRatio(
//             Vector2.Distance(_startSize, _destSize),
//             Vector2.Distance(_rect.sizeDelta, _destSize));
// }
