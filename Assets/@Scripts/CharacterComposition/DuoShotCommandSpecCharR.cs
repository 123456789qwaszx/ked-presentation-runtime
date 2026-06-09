using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Duo Shot",
    Order = -150)]
public sealed class DuoShotCommandSpecCharR : CommandSpecBase
{
    [Header("Characters")]
    public string leftRoleKey = "";
    public string rightRoleKey = "";

    [Header("Preset")]
    public CharacterDuoShotPreset preset = CharacterDuoShotPreset.Balanced;

    [Tooltip("true이면 preset 대신 아래 layout을 사용합니다.")]
    public bool overrideLayout = false;

    public CharacterDuoShotLayout layout = new CharacterDuoShotLayout();

    [Header("Optional Pose Tuning")]
    public string leftPoseKey = "";
    public string rightPoseKey = "";

    [Header("Targets")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track;

    [Tooltip("DuoShot scale을 적용할 대상입니다. 보통 CharSlot_Scale입니다.")]
    public CharacterRigTarget scaleTarget = CharacterRigTarget.CharSlot_Scale;

    public bool applyScale = true;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
}

public sealed class DuoShotCommandCharR : CommandBase
{
    private sealed class SideRuntime
    {
        public string RoleKey;
        public string PoseKey;

        public RectTransform MoveRect;
        public RectTransform ScaleRect;

        public Vector2 Destination;
        public Vector2 TargetScale;

        public Tween MoveTween;
        public Tween ScaleTween;
    }

    private readonly DuoShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private readonly SideRuntime _left = new SideRuntime();
    private readonly SideRuntime _right = new SideRuntime();

    private bool _resolved;
    private bool _computed;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public DuoShotCommandCharR(
        DuoShotCommandSpecCharR spec,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!ResolveRuntime(scope))
            yield break;

        if (_spec.killTween)
        {
            KillSideTweens(_left, complete: true);
            KillSideTweens(_right, complete: true);
        }

        _computed = false;

        if (!ComputeDestinations(scope))
            yield break;

        _canCommitFinalState = true;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            ClearRuntimeState();
            yield break;
        }

        StartTweens();

        if (_spec.wait)
            yield return WaitUntilTweensComplete();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!ResolveRuntime(scope))
            return;

        KillSideTweens(_left, complete: false);
        KillSideTweens(_right, complete: false);

        _computed = false;

        if (!ComputeDestinations(scope))
            return;

        CommitFinalState();
        ClearRuntimeState();
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        if (!ResolveRuntime(scope))
            return;

        KillSideTweens(_left, complete: false);
        KillSideTweens(_right, complete: false);

        if (!ComputeDestinations(scope))
            return;

        CommitFinalState();
        ClearRuntimeState();
    }

    private bool ResolveRuntime(CommandRunScope scope)
    {
        if (_resolved)
            return _left.MoveRect != null && _right.MoveRect != null;

        _resolved = true;

        _left.RoleKey = _spec.leftRoleKey;
        _left.PoseKey = _spec.leftPoseKey;

        _right.RoleKey = _spec.rightRoleKey;
        _right.PoseKey = _spec.rightPoseKey;

        if (!ResolveSide(scope, _left))
            return false;

        if (!ResolveSide(scope, _right))
            return false;

        return true;
    }

    private bool ResolveSide(CommandRunScope scope, SideRuntime side)
    {
        if (string.IsNullOrWhiteSpace(side.RoleKey))
            return false;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, side.RoleKey);

        if (rigRefs == null)
            return false;

        side.MoveRect = rigRefs.GetRect(_spec.moveTarget);

        if (side.MoveRect == null)
        {
            Debug.LogWarning(
                $"[DuoShotCommandCharR] Missing move target. " +
                $"roleKey='{side.RoleKey}', target='{_spec.moveTarget}'.");
            return false;
        }

        if (_spec.applyScale)
        {
            side.ScaleRect = rigRefs.GetRect(_spec.scaleTarget);

            if (side.ScaleRect == null)
            {
                Debug.LogWarning(
                    $"[DuoShotCommandCharR] Missing scale target. " +
                    $"roleKey='{side.RoleKey}', target='{_spec.scaleTarget}'.");
                return false;
            }
        }

        return true;
    }

    private bool ComputeDestinations(CommandRunScope scope)
    {
        if (_computed)
            return true;

        _computed = true;

        CharacterDuoShotLayout layout =
            _spec.overrideLayout && _spec.layout != null
                ? _spec.layout
                : CharacterDuoShotPresetResolver.Resolve(_spec.preset);

        if (layout == null || layout.left == null || layout.right == null)
            return false;

        if (!ComputeSideDestination(scope, _left, layout.left))
            return false;

        if (!ComputeSideDestination(scope, _right, layout.right))
            return false;

        return true;
    }

    private bool ComputeSideDestination(
        CommandRunScope scope,
        SideRuntime side,
        CharacterDuoShotSideLayout layout)
    {
        side.TargetScale = layout.scale;

        CharacterPlacementScalePreview scalePreview =
            _spec.applyScale
                ? new CharacterPlacementScalePreview(side.ScaleRect, layout.scale)
                : CharacterPlacementScalePreview.None;

        if (!CharacterPlacementSolver.TryCalculateFocusPlacement(
                scope,
                side.RoleKey,
                side.MoveRect,
                layout.focusPreset,
                side.PoseKey,
                layout.customFocusKey,
                layout.focusOffset,
                _focusTuningDb,
                layout.screenPoint,
                layout.screenOffset,
                scalePreview,
                out CharacterPlacementResult placement))
        {
            Debug.LogWarning(
                $"[DuoShotCommandCharR] Failed to calculate side placement. " +
                $"roleKey='{side.RoleKey}', focus='{layout.focusPreset}', screenPoint='{layout.screenPoint}'.");
            return false;
        }

        side.Destination = placement.DestinationAnchoredPosition;
        return true;
    }

    private void StartTweens()
    {
        StartMoveTween(_left);
        StartMoveTween(_right);

        if (_spec.applyScale)
        {
            StartScaleTween(_left);
            StartScaleTween(_right);
        }
    }

    private void StartMoveTween(SideRuntime side)
    {
        side.MoveTween = side.MoveRect
            .DOAnchorPos(side.Destination, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(side.MoveRect);
    }

    private void StartScaleTween(SideRuntime side)
    {
        if (side.ScaleRect == null)
            return;

        Vector3 target = side.ScaleRect.localScale;
        target.x = side.TargetScale.x;
        target.y = side.TargetScale.y;

        side.ScaleTween = side.ScaleRect
            .DOScale(target, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(side.ScaleRect);
    }

    private IEnumerator WaitUntilTweensComplete()
    {
        while (IsTweenActive(_left.MoveTween) ||
               IsTweenActive(_right.MoveTween) ||
               IsTweenActive(_left.ScaleTween) ||
               IsTweenActive(_right.ScaleTween))
        {
            yield return null;
        }
    }

    private static bool IsTweenActive(Tween tween)
    {
        return tween != null && tween.IsActive() && tween.IsPlaying();
    }

    private void CommitFinalState()
    {
        CommitSide(_left);
        CommitSide(_right);
    }

    private void CommitSide(SideRuntime side)
    {
        if (side.MoveRect != null)
            side.MoveRect.anchoredPosition = side.Destination;

        if (_spec.applyScale && side.ScaleRect != null)
        {
            Vector3 s = side.ScaleRect.localScale;
            s.x = side.TargetScale.x;
            s.y = side.TargetScale.y;
            side.ScaleRect.localScale = s;
        }
    }

    private void KillSideTweens(SideRuntime side, bool complete)
    {
        side.MoveTween?.Kill(complete);
        side.ScaleTween?.Kill(complete);

        if (side.MoveRect != null)
            side.MoveRect.DOKill(complete);

        if (side.ScaleRect != null)
            side.ScaleRect.DOKill(complete);

        side.MoveTween = null;
        side.ScaleTween = null;
    }

    private void ClearRuntimeState()
    {
        _canCommitFinalState = false;

        _left.MoveTween = null;
        _left.ScaleTween = null;
        _right.MoveTween = null;
        _right.ScaleTween = null;
    }
}