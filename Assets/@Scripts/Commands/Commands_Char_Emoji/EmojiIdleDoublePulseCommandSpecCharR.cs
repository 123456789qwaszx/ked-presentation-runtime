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
}

public sealed class EmojiIdleDoublePulseCommandCharR : CommandBase
{
    private readonly EmojiIdleDoublePulseCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _baseScale;
    private Sequence _sequence;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    // 이 command는 항상 background idle이다.
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
        PlayLoop();

        // SequencePlayer가 이 coroutine을 background lifetime에 묶을 수 있도록
        // 끝나지 않는 tween completion yield를 반환한다.
        yield return _sequence.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    public override void RegisterStepLifetime(
        CommandRunScope scope,
        MonoBehaviour host,
        IEnumerator routine)
    {
        scope.TrackStep(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                CommitFinalState();
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                OnStepLifetimeFinished(scope);
            });
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

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
        KillSequence();

        float pulseDuration =
            _spec.firstUpDuration +
            _spec.firstDownDuration +
            _spec.pulseGap +
            _spec.secondUpDuration +
            _spec.secondDownDuration;

        float restDuration =
            Mathf.Max(0f, _spec.interval - pulseDuration);

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_rect);

        if (_spec.initialDelay > 0f)
            _sequence.AppendInterval(_spec.initialDelay);

        AppendDoublePulse(_sequence, restDuration);

        _sequence.SetLoops(-1, LoopType.Restart);
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
        KillSequence();

        if (_rect != null)
        {
            _rect.DOKill(false);
            _rect.localScale = _baseScale;
        }

        HasClaimedTarget = false;
    }

    private void KillSequence()
    {
        if (_sequence != null && _sequence.IsActive())
            _sequence.Kill(false);

        _sequence = null;
    }

    private static Vector3 MultiplyScale(Vector3 baseScale, Vector2 multiplier)
    {
        return new Vector3(
            baseScale.x * multiplier.x,
            baseScale.y * multiplier.y,
            baseScale.z);
    }
}