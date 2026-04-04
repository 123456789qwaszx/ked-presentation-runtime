using System;
using System.Collections;
using UnityEngine;

public enum TransitionPlayMode
{
    CoverOnly,
    UncoverOnly,
    CoverThenUncover,
}

[Serializable]
[CommandMenuHint("Transition", "Play Transition", Order = -950)]
public sealed class TransitionCommandSpec : CommandSpecBase
{
    [Header("Mode")]
    public TransitionPlayMode playMode = TransitionPlayMode.CoverThenUncover;

    [Header("Target")]
    public TransitionTargetKind targetKind = TransitionTargetKind.Blackout;
    public string customTargetKey = "";

    [Header("Opacity")]
    [Range(0f, 1f)]
    public float coveredAlpha = 1f;

    [Range(0f, 1f)]
    public float uncoveredAlpha = 0f;

    [Header("Durations")]
    [Min(0f)]
    public float coverDuration = 0.20f;

    [Min(0f)]
    public float uncoverDuration = 0.20f;

    [Tooltip("CoverThenUncover 모드에서, 화면이 완전히 닫힌 뒤 유지할 시간.")]
    [Min(0f)]
    public float holdCoveredSeconds = 0f;

    [Header("Ease")]
    public AnimationCurve coverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve uncoverEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Playback")]
    [Tooltip("체크하면 이 커맨드의 재생이 끝날 때까지 Step 진행을 멈춥니다.")]
    public bool wait = true;

    [Tooltip("시작 시 uncoveredAlpha 상태로 초기화합니다.")]
    public bool resetToOpenAtStart = true;
}

public sealed class TransitionCommand : CommandBase
{
    private readonly TransitionCommandSpec _spec;
    private readonly TransitionTargetRouter _targetRouter;
    private readonly ITransitionTargetPlayer _transitionPlayer;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionCommand(
        TransitionTargetRouter targetRouter,
        ITransitionTargetPlayer transitionPlayer,
        TransitionCommandSpec spec)
    {
        _targetRouter = targetRouter;
        _transitionPlayer = transitionPlayer;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _targetRouter.TryResolve(_spec.targetKind, _spec.customTargetKey,
            out TransitionTargetHandle target);

        if (_spec.resetToOpenAtStart && (_spec.playMode == TransitionPlayMode.CoverOnly || _spec.playMode == TransitionPlayMode.CoverThenUncover))
            _transitionPlayer.SetInstant(target, _spec.uncoveredAlpha, false);

        if (scope.IsSkipping)
        {
            ApplySkipInstant(target, _spec.playMode);
            yield break;
        }

        switch (_spec.playMode)
        {
            case TransitionPlayMode.CoverOnly:
                yield return Cover(target);
                break;

            case TransitionPlayMode.UncoverOnly:
                yield return Uncover(target);
                break;

            case TransitionPlayMode.CoverThenUncover:
                yield return Cover(target);

                if (_spec.holdCoveredSeconds > 0f)
                    yield return WaitUnscaled(_spec.holdCoveredSeconds);

                yield return Uncover(target);
                break;
        }
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        _targetRouter.TryResolve(_spec.targetKind, _spec.customTargetKey, 
            out TransitionTargetHandle target);

        ApplySkipInstant(target, _spec.playMode);
    }

    private IEnumerator Cover(TransitionTargetHandle target)
    {
        if (_spec.coverDuration <= 0f)
        {
            _transitionPlayer.SetInstant(target, _spec.coveredAlpha, false);
            yield break;
        }

        yield return _transitionPlayer.FadeTo(target, _spec.coveredAlpha, _spec.coverDuration, false, _spec.coverEase);
    }

    private IEnumerator Uncover(TransitionTargetHandle target)
    {
        if (_spec.uncoverDuration <= 0f)
        {
            _transitionPlayer.SetInstant(target, _spec.uncoveredAlpha, false);
            yield break;
        }

        yield return _transitionPlayer.FadeTo(target, _spec.uncoveredAlpha, _spec.uncoverDuration, false, _spec.uncoverEase);
    }

    private void ApplySkipInstant(TransitionTargetHandle target, TransitionPlayMode mode)
    {
        switch (mode)
        {
            case TransitionPlayMode.CoverOnly:
                _transitionPlayer.SetInstant(target, _spec.coveredAlpha, false);
                break;

            case TransitionPlayMode.UncoverOnly:
                _transitionPlayer.SetInstant(target, _spec.uncoveredAlpha, false);
                break;

            case TransitionPlayMode.CoverThenUncover:
                _transitionPlayer.SetInstant(target, _spec.coveredAlpha, false);
                _transitionPlayer.SetInstant(target, _spec.uncoveredAlpha, false);
                break;
        }
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}