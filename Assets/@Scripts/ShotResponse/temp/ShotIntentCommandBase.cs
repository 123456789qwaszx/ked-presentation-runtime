// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
//
// public abstract class ShotIntentCommandBase<TSpec> : CommandBase, IStepScopedCommand
//     where TSpec : CommandSpecBase
// {
//     protected readonly PresentationResponseRig Rig;
//     protected readonly TSpec Spec;
//
//     private PresentationIntentState _fromState;
//     private PresentationIntentState _toState;
//     private Tween _tween;
//     private bool _canCommitFinalState;
//
//     protected abstract float Duration { get; }
//     protected abstract Ease Ease { get; }
//     protected abstract bool KillTween { get; }
//
//     public override bool WaitForCompletion => Spec.wait;
//     protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;
//
//     protected ShotIntentCommandBase(PresentationResponseRig rig, TSpec spec)
//     {
//         Rig = rig;
//         Spec = spec;
//     }
//
//     protected override IEnumerator ExecuteInner(CommandRunScope scope)
//     {
//         if (Rig == null)
//             yield break;
//
//         if (KillTween)
//             _tween?.Kill(true);
//
//         _fromState = Rig.CurrentState;
//         _toState = BuildTargetState(_fromState, scope);
//
//         _canCommitFinalState = true;
//
//         if (Duration <= 0f)
//         {
//             Rig.ApplyToAllBindings(_toState);
//             ClearRuntimeState();
//             yield break;
//         }
//
//         _tween = DOTween
//             .To(
//                 () => 0f,
//                 t =>
//                 {
//                     if (!_canCommitFinalState || Rig == null)
//                         return;
//
//                     PresentationIntentState state = InterpolateState(_fromState, _toState, Mathf.Clamp01(t));
//                     Rig.ApplyToAllBindings(state);
//                 },
//                 1f,
//                 Duration)
//             .SetEase(Ease)
//             .SetUpdate(true)
//             .SetTarget(Rig)
//             .OnComplete(() =>
//             {
//                 if (!_canCommitFinalState || Rig == null)
//                     return;
//
//                 Rig.ApplyToAllBindings(_toState);
//                 ClearRuntimeState();
//             });
//
//         if (Spec.wait)
//             yield return _tween.WaitForCompletion();
//     }
//
//     protected override void OnSkip(CommandRunScope scope)
//     {
//         if (Rig == null)
//             return;
//
//         _fromState = Rig.CurrentState;
//         _toState = BuildTargetState(_fromState, scope);
//
//         Rig.ApplyToAllBindings(_toState);
//         ClearRuntimeState();
//     }
//
//     protected override void OnRollbackSeek(CommandRunScope scope)
//     {
//         OnSkip(scope);
//     }
//
//     protected override void OnCommandCompleted(CommandRunScope scope)
//     {
//         if (!_canCommitFinalState || Rig == null)
//             return;
//
//         _tween?.Kill(false);
//         Rig.ApplyToAllBindings(_toState);
//         ClearRuntimeState();
//     }
//
//     protected abstract PresentationIntentState BuildTargetState(
//         in PresentationIntentState from,
//         CommandRunScope scope);
//
//     private void ClearRuntimeState()
//     {
//         _canCommitFinalState = false;
//         _tween = null;
//     }
//
//     protected static PresentationIntentState InterpolateState(
//         in PresentationIntentState from,
//         in PresentationIntentState to,
//         float t)
//     {
//         return new PresentationIntentState
//         {
//             zoom = Mathf.Lerp(from.zoom, to.zoom, t),
//             pan = Vector2.Lerp(from.pan, to.pan, t),
//             focusPoint = Vector2.Lerp(from.focusPoint, to.focusPoint, t),
//         };
//     }
// }