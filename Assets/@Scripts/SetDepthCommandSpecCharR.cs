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
    public CharacterDepthPreset preset = CharacterDepthPreset.Mid;

    [Tooltip("true이면 preset 대신 level을 사용합니다.")]
    public bool useLevel = false;

    [Range(0f, 10f)]
    public float level = 5f;

    [Header("Command Correction")]
    [Tooltip("최종 DepthY에 추가로 더할 command-time offset입니다.")]
    public Vector2 yOffsetAdd = Vector2.zero;

    [Tooltip("최종 DepthScale에 곱할 command-time multiplier입니다. 1이면 그대로입니다.")]
    public float scaleMultiplier = 1f;

    [Header("Preserve Focus Override")]
    [Tooltip("true이면 depth preset이 가진 preserve focus 대신 아래 값을 사용합니다.")]
    public bool overridePreserveFocus = false;

    public CharacterFocusPreset preserveFocusPreset = CharacterFocusPreset.Bust;

    [Tooltip("preserve focus point에 추가로 더할 command-time offset입니다.")]
    public Vector2 preserveFocusOffset = Vector2.zero;

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
    private readonly RoleDepthTuningDBSO _roleTuningDb;
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
        RoleDepthTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            yield break;

        if (!ClaimTarget(scope))
            yield break;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        StartTween(_spec.duration);

        if (_spec.wait && _sequence != null)
            yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            return;

        if (!HasClaimedTarget)
        {
            if (!ClaimTarget(scope))
                return;
        }

        CommitFinalState();
    }

    private bool TryResolveRefs(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _rigRefs != null && _depthYRect != null && _depthScaleRect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            scope,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _depthYRect = _rigRefs.GetRect(_spec.depthYTarget);
        _depthScaleRect = _rigRefs.GetRect(_spec.depthScaleTarget);

        return _depthYRect != null && _depthScaleRect != null;
    }

    private bool ClaimTarget(CommandRunScope scope)
    {
        // 이전 depth tween이 있으면 final state로 커밋하고 이어받는다.
        _depthYRect.DOKill(true);
        _depthScaleRect.DOKill(true);

        if (_sequence != null && _sequence.IsActive())
            _sequence.Kill(false);

        _startDepthY = _depthYRect.anchoredPosition;
        _startDepthScale = CaptureScaleXY(_depthScaleRect);

        if (!ResolveDestination(scope))
            return false;

        PublishSettledTargets();

        HasClaimedTarget = true;
        return true;
    }

    private bool ResolveDestination(CommandRunScope scope)
    {
        if (!CharacterDepthResolver.TryResolveRawDepth(
                scope,
                _spec.slotKey,
                _spec.preset,
                _spec.useLevel,
                _spec.level,
                _globalTuning,
                _roleTuningDb,
                _spec.overridePreserveFocus,
                _spec.preserveFocusPreset,
                _spec.preserveFocusOffset,
                _spec.yOffsetAdd,
                _spec.scaleMultiplier,
                out CharacterDepthResult rawDepth))
        {
            return false;
        }

        _destRawDepthY = rawDepth.RawDepthYAnchoredPosition;
        _destDepthScale = rawDepth.DepthScale;

        if (!CharacterDepthResolver.CalculateDepthYThatPreservesCurrentFocus(
                scope,
                _spec.slotKey,
                _depthYRect,
                _depthScaleRect,
                _destRawDepthY,
                _destDepthScale,
                rawDepth.PreserveFocusPreset,
                rawDepth.PreserveFocusOffset,
                _focusTuningDb,
                out _destFinalDepthY))
        {
            Debug.LogWarning(
                $"[SetDepthCommandCharR] Failed to preserve focus. " +
                $"Fallback to raw depthY. slotKey='{_spec.slotKey}', " +
                $"focus='{rawDepth.PreserveFocusPreset}', custom='{rawDepth.PreserveCustomFocusKey}'.");

            _destFinalDepthY = _destRawDepthY;
        }

        return true;
    }

    private void StartTween(float duration)
    {
        KillSequenceWithoutCompleting();

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

    private void CommitFinalState()
    {
        if (_depthYRect != null)
            _depthYRect.anchoredPosition = _destFinalDepthY;

        if (_depthScaleRect != null)
            _depthScaleRect.localScale = new Vector3(
                _destDepthScale.x,
                _destDepthScale.y,
                1f);

        ClearSettledTargets();

        HasClaimedTarget = false;
        _sequence = null;
    }

    private void PublishSettledTargets()
    {
        if (_rigRefs == null || _rigRefs.PlacementTargets == null)
            return;

        _rigRefs.PlacementTargets.PublishAnchoredPosition(
            _depthYRect,
            _destFinalDepthY);

        _rigRefs.PlacementTargets.PublishLocalScale(
            _depthScaleRect,
            _destDepthScale);
    }

    private void ClearSettledTargets()
    {
        if (_rigRefs == null || _rigRefs.PlacementTargets == null)
            return;

        _rigRefs.PlacementTargets.Clear(_depthYRect);
        _rigRefs.PlacementTargets.Clear(_depthScaleRect);
    }

    private void KillSequenceWithoutCompleting()
    {
        if (_sequence != null && _sequence.IsActive())
            _sequence.Kill(false);

        _sequence = null;
    }

    private static Vector2 CaptureScaleXY(RectTransform rect)
    {
        if (rect == null)
            return Vector2.one;

        Vector3 s = rect.localScale;
        return new Vector2(s.x, s.y);
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        KillSequenceWithoutCompleting();

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        StartTween(duration);
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float posOriginalDistance =
            Vector2.Distance(_startDepthY, _destFinalDepthY);

        float posRemainingDistance =
            _depthYRect != null
                ? Vector2.Distance(_depthYRect.anchoredPosition, _destFinalDepthY)
                : 0f;

        float scaleOriginalDistance =
            Vector2.Distance(_startDepthScale, _destDepthScale);

        float scaleRemainingDistance =
            _depthScaleRect != null
                ? Vector2.Distance(CaptureScaleXY(_depthScaleRect), _destDepthScale)
                : 0f;

        float posRatio =
            posOriginalDistance <= 0.001f
                ? 0f
                : Mathf.Clamp01(posRemainingDistance / posOriginalDistance);

        float scaleRatio =
            scaleOriginalDistance <= 0.001f
                ? 0f
                : Mathf.Clamp01(scaleRemainingDistance / scaleOriginalDistance);

        float remainingRatio = Mathf.Max(posRatio, scaleRatio);

        if (remainingRatio <= 0.001f)
            return 0f;

        float remainingDuration = _spec.duration * remainingRatio;

        return Mathf.Max(
            0.01f,
            remainingDuration / StepFinishSpeedUpMultiplier);
    }

    #endregion
}