using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Composition",
    "Set Depth",
    Order = -198,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -928)]
public sealed class SetDepthCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Depth")]
    public CharacterDepthKey preset = CharacterDepthKey.Mid;

    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Bust;
    
    [Tooltip("true이면 preset 대신 level을 사용합니다.")]
    public bool useLevel = false;
    public float level = 5f;

    [Tooltip("preserve focus point에 추가로 더할 offset.")]
    public Vector2 focusOffset = Vector2.zero;

    [Header("Targets")]
    public CharacterRigTarget depthYTarget = CharacterRigTarget.CharSlot_DepthY;
    public CharacterRigTarget depthScaleTarget = CharacterRigTarget.CharSlot_DepthScale;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class SetDepthCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly SetDepthCommandSpecCharR _spec;
    private readonly CharacterDepthTuningSO _globalTuning;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _depthYRect;
    private RectTransform _depthScaleRect;

    private Vector2 _startDepthY;
    private Vector2 _startDepthScale;

    private Vector2 _destRawDepthY;
    private Vector2 _destFinalDepthY;
    private Vector2 _destDepthScale;

    private Sequence _sequence;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public SetDepthCommandCharR(
        SetDepthCommandSpecCharR spec,
        CharacterDepthTuningSO globalTuning,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _globalTuning = globalTuning;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget(scope);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_depthYRect);

        _sequence.Join(
            _depthYRect
                .DOAnchorPos(_destFinalDepthY, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthYRect));

        _sequence.Join(
            _depthScaleRect
                .DOScale(new Vector3(_destDepthScale.x, _destDepthScale.y, 1f), _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthScaleRect));

        _sequence.OnComplete(CommitFinalState);

        if (_spec.wait && _sequence != null)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget(scope);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        
        _depthYRect = _rigRefs.GetRect(_spec.depthYTarget);
        _depthScaleRect = _rigRefs.GetRect(_spec.depthScaleTarget);
        
        _resolveAttempted = true;
    }

    private void ClaimTarget(CommandRunScope scope)
    {
        _depthYRect.DOKill(true);
        _depthScaleRect.DOKill(true);

        _startDepthY = _depthYRect.anchoredPosition;
        _startDepthScale = new Vector2(_depthScaleRect.localScale.x, _depthScaleRect.localScale.y);
        
        CharacterDepthResolver.ResolveRawDepth(
            _spec.preset,
            _spec.useLevel,
            _spec.level,
            _globalTuning,
            _spec.focusPreset,
            _spec.focusOffset,
            out CharacterDepthResult rawDepth);

        _destRawDepthY = rawDepth.RawDepthYAnchoredPosition;
        _destDepthScale = rawDepth.DepthScale;
        
        CharacterDepthResolver.CalculateDepthYThatPreservesCurrentFocus(
            scope,
            _spec.slotKey,
            _depthYRect,
            _depthScaleRect,
            _destRawDepthY,
            _destDepthScale,
            rawDepth.PreserveFocusPreset,
            rawDepth.PreserveFocusOffset,
            _focusTuningDb,
            out Vector2 destFinalDepthY);
        
        _destFinalDepthY = destFinalDepthY;
        
        PublishSettledTargets();

        HasClaimedTarget = true;
    }
    
    private void CommitFinalState()
    {
        _depthYRect.anchoredPosition = _destFinalDepthY;
        _depthScaleRect.localScale = new Vector3(_destDepthScale.x, _destDepthScale.y, 1f);

        ClearSettledTargets();

        HasClaimedTarget = false;
    }

    private void PublishSettledTargets()
    {
        _rigRefs.PlacementTargets.PublishAnchoredPosition(_depthYRect, _destFinalDepthY);
        _rigRefs.PlacementTargets.PublishLocalScale(_depthScaleRect, _destDepthScale);
    }

    private void ClearSettledTargets()
    {
        _rigRefs.PlacementTargets.Clear(_depthYRect);
        _rigRefs.PlacementTargets.Clear(_depthScaleRect);
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        if (_sequence.IsActive())
            _sequence.Kill(false);

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_depthYRect);

        _sequence.Join(
            _depthYRect
                .DOAnchorPos(_destFinalDepthY, duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthYRect));

        _sequence.Join(
            _depthScaleRect
                .DOScale(new Vector3(_destDepthScale.x, _destDepthScale.y, 1f), duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_depthScaleRect));

        _sequence.OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float posOriginalDistance = Vector2.Distance(_startDepthY, _destFinalDepthY);
        float posRemainingDistance = Vector2.Distance(
            _depthYRect.anchoredPosition,
            _destFinalDepthY);

        float scaleOriginalDistance = Vector2.Distance(_startDepthScale, _destDepthScale);
        float scaleRemainingDistance = Vector2.Distance(
            new Vector2(_depthScaleRect.localScale.x, _depthScaleRect.localScale.y),
            _destDepthScale);

        float posRatio = CalculateRemainingRatio(
            posRemainingDistance,
            posOriginalDistance);

        float scaleRatio = CalculateRemainingRatio(
            scaleRemainingDistance,
            scaleOriginalDistance);

        float remainingRatio = Mathf.Max(posRatio, scaleRatio);

        if (!float.IsFinite(remainingRatio))
            return 0f;

        float remainingDuration = _spec.duration * remainingRatio;

        if (!float.IsFinite(remainingDuration))
            return 0f;

        return remainingDuration / StepFinishSpeedUpMultiplier;
    }

    private static float CalculateRemainingRatio(
        float remainingDistance,
        float originalDistance)
    {
        if (originalDistance <= Mathf.Epsilon)
            return 0f;

        float ratio = remainingDistance / originalDistance;

        if (!float.IsFinite(ratio))
            return 0f;

        return Mathf.Clamp01(ratio);
    }

    #endregion
}