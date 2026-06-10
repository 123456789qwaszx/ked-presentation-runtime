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

    [Header("Optional Pose Tuning")]
    public string leftPoseKey = "";
    public string rightPoseKey = "";

    [Header("Targets")]
    public CharacterRigTarget moveTarget = CharacterRigTarget.CharSlot_Track;

    [Tooltip("DuoShot scale을 적용할 대상입니다. 보통 CharSlot_Scale입니다.")]
    public CharacterRigTarget scaleTarget = CharacterRigTarget.CharSlot_Scale;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;
}

public sealed class DuoShotCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private sealed class SideRuntime
    {
        public string RoleKey;
        public string PoseKey;

        public RectTransform MoveRect;
        public RectTransform ScaleRect;

        public Vector2 StartPosition;
        public Vector2 Destination;

        public Vector2 StartScale;
        public Vector2 TargetScale;
    }

    private readonly DuoShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private readonly SideRuntime _left = new();
    private readonly SideRuntime _right = new();

    private Sequence _sequence;

    private bool _resolveAttempted;

    private bool HasClaimedTargets { get; set; }

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
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTargets(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(this);

        JoinSideTween(_sequence, _left, _spec.duration);
        JoinSideTween(_sequence, _right, _spec.duration);

        _sequence.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTargets)
            ClaimTargets(scope);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _left.RoleKey = _spec.leftRoleKey;
        _left.PoseKey = _spec.leftPoseKey;

        _right.RoleKey = _spec.rightRoleKey;
        _right.PoseKey = _spec.rightPoseKey;

        ResolveSide(scope, _left);
        ResolveSide(scope, _right);

        _resolveAttempted = true;
    }

    private void ResolveSide(CommandRunScope scope, SideRuntime side)
    {
        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, side.RoleKey);
        side.MoveRect = rig.GetRect(_spec.moveTarget);
        side.ScaleRect = rig.GetRect(_spec.scaleTarget);
    }

    private void ClaimTargets(CommandRunScope scope)
    {
        _left.MoveRect.DOKill(true);
        _left.ScaleRect.DOKill(true);
        _right.MoveRect.DOKill(true);
        _right.ScaleRect.DOKill(true);

        CaptureStartState(_left);
        CaptureStartState(_right);

        ComputeDestinations(scope);

        HasClaimedTargets = true;
    }

    private static void CaptureStartState(SideRuntime side)
    {
        side.StartPosition = side.MoveRect.anchoredPosition;

        Vector3 scale = side.ScaleRect.localScale;
        side.StartScale = new Vector2(scale.x, scale.y);
    }

    private void ComputeDestinations(CommandRunScope scope)
    {
        CharacterDuoShotLayout layout = CharacterDuoShotPresetResolver.Resolve(_spec.preset);

        ComputeSideDestination(scope, _left, layout.left);
        ComputeSideDestination(scope, _right, layout.right);
    }

    private void ComputeSideDestination(CommandRunScope scope, SideRuntime side, CharacterDuoShotSideLayout layout)
    {
        side.TargetScale = layout.scale;

        CharacterFocusPlacementSolver.TryCalculateFocusPlacement(
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
            out Vector2 destPos);

        side.Destination = destPos;
    }

    private void JoinSideTween(Sequence sequence, SideRuntime side, float duration)
    {
        sequence.Join(
            side.MoveRect
                .DOAnchorPos(side.Destination, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(side.MoveRect));

        Vector3 targetScale = side.ScaleRect.localScale;
        targetScale.x = side.TargetScale.x;
        targetScale.y = side.TargetScale.y;

        sequence.Join(
            side.ScaleRect
                .DOScale(targetScale, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(side.ScaleRect));
    }

    private void CommitFinalState()
    {
        CommitSide(_left);
        CommitSide(_right);

        HasClaimedTargets = false;
        _sequence = null;
    }

    private void CommitSide(SideRuntime side)
    {
        side.MoveRect.anchoredPosition = side.Destination;

        Vector3 scale = side.ScaleRect.localScale;
        scale.x = side.TargetScale.x;
        scale.y = side.TargetScale.y;
        side.ScaleRect.localScale = scale;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        _sequence.Kill(false);

        float duration = Mathf.Max(
            CalculateAcceleratedRemainingDuration(_left),
            CalculateAcceleratedRemainingDuration(_right));

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(this);

        JoinSideTween(_sequence, _left, duration);
        JoinSideTween(_sequence, _right, duration);

        _sequence.OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(SideRuntime side)
    {
        float moveRatio = CalculateMoveRemainingRatio(side);
        float scaleRatio = CalculateScaleRemainingRatio(side);

        float remainingRatio = Mathf.Max(moveRatio, scaleRatio);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private static float CalculateMoveRemainingRatio(SideRuntime side)
    {
        float originalDistance = Vector2.Distance(side.StartPosition, side.Destination);
        float remainingDistance = Vector2.Distance(side.MoveRect.anchoredPosition, side.Destination);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    private static float CalculateScaleRemainingRatio(SideRuntime side)
    {
        Vector3 currentScale3 = side.ScaleRect.localScale;
        Vector2 currentScale = new(currentScale3.x, currentScale3.y);

        float originalDistance = Vector2.Distance(side.StartScale, side.TargetScale);
        float remainingDistance = Vector2.Distance(currentScale, side.TargetScale);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }

    #endregion
}