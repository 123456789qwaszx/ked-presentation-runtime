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

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;
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
    }

    private readonly DuoShotCommandSpecCharR _spec;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private readonly SideRuntime _left = new();
    private readonly SideRuntime _right = new();

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

        Sequence leftSequence = CreateSideSequence(_left);
        Sequence rightSequence = CreateSideSequence(_right);

        if (_spec.wait)
        {
            while (leftSequence.IsPlaying() || rightSequence.IsPlaying())
                yield return null;
        }
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
        _resolveAttempted = true;

        _left.RoleKey = _spec.leftRoleKey;
        _left.PoseKey = _spec.leftPoseKey;

        _right.RoleKey = _spec.rightRoleKey;
        _right.PoseKey = _spec.rightPoseKey;

        ResolveSide(scope, _left);
        ResolveSide(scope, _right);
    }

    private void ResolveSide(CommandRunScope scope, SideRuntime side)
    {
        if (string.IsNullOrWhiteSpace(side.RoleKey))
            return;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, side.RoleKey);
        side.MoveRect = rig.GetRect(_spec.moveTarget);
        side.ScaleRect = rig.GetRect(_spec.scaleTarget);
    }

    private void ClaimTargets(CommandRunScope scope)
    {
        _left.MoveRect.DOKill(true);
        _right.ScaleRect.DOKill(true);

        ComputeDestinations(scope);

        HasClaimedTargets = true;
    }

    private bool ComputeDestinations(CommandRunScope scope)
    {
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

        CharacterPlacementScalePreview scalePreview = new CharacterPlacementScalePreview(side.ScaleRect, layout.scale);

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
                out Vector2 destPos))
        {
            Debug.LogWarning(
                $"[DuoShotCommandCharR] Failed to calculate side placement. " +
                $"roleKey='{side.RoleKey}', focus='{layout.focusPreset}', screenPoint='{layout.screenPoint}'.");

            return false;
        }

        side.Destination = destPos;
        return true;
    }

    private Sequence CreateSideSequence(SideRuntime side)
    {
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(side.MoveRect);

        sequence.Join(
            side.MoveRect
                .DOAnchorPos(side.Destination, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(side.MoveRect));

        Vector3 targetScale = side.ScaleRect.localScale;
        targetScale.x = side.TargetScale.x;
        targetScale.y = side.TargetScale.y;
        
        sequence.Join(
            side.ScaleRect
            .DOScale(targetScale, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(side.ScaleRect));

        sequence.OnComplete(() => CommitSide(side));

        return sequence;
    }

    private void CommitFinalState()
    {
        CommitSide(_left);
        CommitSide(_right);

        HasClaimedTargets = false;
    }

    private void CommitSide(SideRuntime side)
    {
        side.MoveRect.anchoredPosition = side.Destination;

        Vector3 scale = side.ScaleRect.localScale;
        scale.x = side.TargetScale.x;
        scale.y = side.TargetScale.y;
        side.ScaleRect.localScale = scale;
    }
}