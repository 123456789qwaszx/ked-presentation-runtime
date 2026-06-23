using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Place To",
    Order = -199)]
public sealed class PlaceToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Anchor only)")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Anchor;

    [Header("Preset")]
    public CharAnchorPreset preset = CharAnchorPreset.Center;

    [Tooltip("StageSlot 폭 대비 상대 위치. 0.33이면 좌/우가 화면폭의 약 1/3 지점.")]
    [Range(0f, 0.5f)]
    public float baseRatioX = 0.33f;

    [Header("Offset (after tuning)")]
    public Vector2 offset = Vector2.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class PlaceToCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly PlaceToCommandSpecCharR _spec;
    private readonly CharStageTuningSO _globalTuning;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private Vector2 _startPos;
    private Vector2 _destPos;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public PlaceToCommandCharR(
        PlaceToCommandSpecCharR spec,
        CharStageTuningSO globalTuning,
        RoleAnchorTuningDBSO roleTuningDb)
    {
        _spec = spec;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            yield break;

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _rect
            .DOAnchorPos(_destPos, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            return;

        if (!HasClaimedTarget)
            ClaimTarget(scope);

        CommitFinalState();
    }

    private bool TryResolveRefs(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _rigRefs != null && _rect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            scope,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _rect = _rigRefs.GetRect(_spec.target);

        return _rect != null;
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        // 기존 PlaceTo/placement tween이 있으면 최종상태로 커밋하고 이어받는다.
        // 이때 이전 커맨드의 OnComplete가 ledger clear까지 처리할 수 있다.
        _rect.DOKill(true);

        _startPos = _rect.anchoredPosition;
        _destPos = ResolveDestination(scope);

        // 중요:
        // 이 커맨드는 duration 동안 CharSlot_Anchor를 움직이는 placement writer다.
        // FocusPoint solver가 라이브 위치가 아니라 "정착 목표"를 알 수 있도록 게시한다.
        PublishSettledTarget();

        HasClaimedTarget = true;
    }

    private Vector2 ResolveDestination(CommandRunScope scope)
    {
        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(
                scope,
                _spec.slotKey);

        return CharAnchorPlacementResolver.ResolveAnchoredPosition(
            _rect,
            _spec.preset,
            _spec.baseRatioX,
            _globalTuning,
            _roleTuningDb,
            tuningKey,
            _spec.offset);
    }

    private void PublishSettledTarget()
    {
        if (_rigRefs == null || _rect == null)
            return;

        _rigRefs.PlacementTargets.PublishAnchoredPosition(_rect, _destPos);
    }

    private void ClearSettledTarget()
    {
        if (_rigRefs == null || _rect == null)
            return;

        _rigRefs.PlacementTargets.Clear(_rect);
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _destPos;

        ClearSettledTarget();

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _tween = _rect
            .DOAnchorPos(_destPos, duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = Vector2.Distance(_startPos, _destPos);
        float remainingDistance = Vector2.Distance(_rect.anchoredPosition, _destPos);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(
            0.01f,
            remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion
}