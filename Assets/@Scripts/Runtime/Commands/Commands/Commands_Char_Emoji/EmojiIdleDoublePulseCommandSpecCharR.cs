using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Emoji",
    "Emoji Idle Double Pulse",
    Order = -699)]
public sealed class EmojiIdleDoublePulseCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.EmojiSlot00_Root;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Scale;

    [Header("Timing")]
    [Tooltip("등장 직후 첫 박동까지 기다리는 시간.")]
    public float initialDelay = 1.35f;

    [Tooltip("Double pulse가 반복되는 주기. start-to-start 기준.")]
    public float interval = 2.0f;

    [Header("Pulse Amount")]
    public Vector2 firstPulseScale = new(1.035f, 1.035f);
    public Vector2 secondPulseScale = new(1.022f, 1.022f);

    [Header("Pulse Shape")]
    public float firstUpDuration = 0.075f;
    public float firstDownDuration = 0.105f;
    public float pulseGap = 0.065f;
    public float secondUpDuration = 0.065f;
    public float secondDownDuration = 0.13f;

    public Ease upEase = Ease.OutSine;
    public Ease downEase = Ease.InOutSine;

    [Header("Step Cleanup")]
    [Tooltip("다음 Line/Step으로 넘어갈 때 이모지 Root를 숨긴다.")]
    public bool hideRootOnStepFinished = true;
}

public sealed class EmojiIdleDoublePulseCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 30f;

    private readonly EmojiIdleDoublePulseCommandSpecCharR _spec;

    private RectTransform _root;
    private CanvasGroup _rootCanvasGroup;

    private RectTransform _rect;
    private Vector3 _baseScale;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    // 이 command는 항상 background idle이다.
    // wait=true가 되면 infinite loop 때문에 step 진행이 막힐 수 있으므로 spec.wait를 보지 않는다.
    public override bool WaitForCompletion => false;

    public EmojiIdleDoublePulseCommandCharR(EmojiIdleDoublePulseCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.initialDelay > 0f)
            yield return WaitUnscaled(_spec.initialDelay);

        PlayLoop();

        yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        KillActiveTween();
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        _root = rigRefs.GetRect(_spec.rootTarget);
        _rootCanvasGroup = _root.GetComponent<CanvasGroup>();

        _rect = rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        _baseScale = _rect.localScale;

        HasClaimedTarget = true;
    }

    private void PlayLoop()
    {
        KillActiveTween();

        float pulseDuration = CalculatePulseDuration();
        float restDuration = Mathf.Max(0f, _spec.interval - pulseDuration);

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_rect);

        AppendDoublePulse(sequence, restDuration);

        _tween = sequence.SetLoops(-1, LoopType.Restart);
    }

    private void AppendDoublePulse(Sequence sequence, float restDuration)
    {
        sequence
            .Append(_rect
                .DOScale(MultiplyScale(_baseScale, _spec.firstPulseScale), _spec.firstUpDuration)
                .SetEase(_spec.upEase))
            .Append(_rect
                .DOScale(_baseScale, _spec.firstDownDuration)
                .SetEase(_spec.downEase))
            .AppendInterval(_spec.pulseGap)
            .Append(_rect
                .DOScale(MultiplyScale(_baseScale, _spec.secondPulseScale), _spec.secondUpDuration)
                .SetEase(_spec.upEase))
            .Append(_rect
                .DOScale(_baseScale, _spec.secondDownDuration)
                .SetEase(_spec.downEase));

        if (restDuration > 0f)
            sequence.AppendInterval(restDuration);
    }

    private void CommitFinalState()
    {
        _rect.localScale = _baseScale;
        HideRootOnStepFinished();

        HasClaimedTarget = false;
        _tween = null;
    }

    private void KillActiveTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        KillActiveTween();

        HideRootOnStepFinished();

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _tween = _rect
            .DOScale(_baseScale, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);
    }

    private void HideRootOnStepFinished()
    {
        if (!_spec.hideRootOnStepFinished)
            return;

        DOTween.Kill(_rootCanvasGroup, false);
        _rootCanvasGroup.alpha = 0f;
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float originalDistance = CalculateReferenceScaleDistance();
        float remainingDistance = Vector3.Distance(_rect.localScale, _baseScale);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = CalculatePulseDuration() * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    private float CalculateReferenceScaleDistance()
    {
        Vector3 firstPulseScale = MultiplyScale(_baseScale, _spec.firstPulseScale);
        Vector3 secondPulseScale = MultiplyScale(_baseScale, _spec.secondPulseScale);

        float firstDistance = Vector3.Distance(firstPulseScale, _baseScale);
        float secondDistance = Vector3.Distance(secondPulseScale, _baseScale);

        return Mathf.Max(
            Mathf.Max(firstDistance, secondDistance),
            0.001f);
    }

    #endregion

    private float CalculatePulseDuration()
    {
        return Mathf.Max(
            0.01f,
            _spec.firstUpDuration +
            _spec.firstDownDuration +
            _spec.pulseGap +
            _spec.secondUpDuration +
            _spec.secondDownDuration);
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static Vector3 MultiplyScale(Vector3 baseScale, Vector2 multiplier)
    {
        return new Vector3(
            baseScale.x * multiplier.x,
            baseScale.y * multiplier.y,
            baseScale.z);
    }
}