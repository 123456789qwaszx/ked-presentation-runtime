using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Transition Out - Strip",
    Order = -836)]
public sealed class TransitionOutStripCommandSpec : CommandSpecBase
{
    [Header("Wipe")]
    public VerticalStripWipeOrder order = VerticalStripWipeOrder.RightToLeft;

    [Header("Strips")]
    public int stripCount = 20;
    public float stripDelay = 0.02f;
    public float stripFillDuration = 0.08f;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.Linear;

    [Header("Options")]
    public bool killTween = true;
    public bool clearOthersBeforeOut = true;
    public bool clearAllAfterOut = true;
}

public sealed class TransitionOutStripCommand : CommandBase
{
    private readonly TransitionOutStripCommandSpec _spec;

    private VerticalStripWipeGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionOutStripCommand(TransitionOutStripCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_graphic == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_graphic, false);

        PrepareCoveredState();

        if (_spec.clearOthersBeforeOut)
            PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.VerticalStripWipe);

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        float duration = _spec.duration > 0f
            ? _spec.duration
            : _graphic.TotalDuration;

        _tween = DOTween
            .To(
                () => 1f,
                value =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    _graphic.Progress01 = value;
                },
                0f,
                duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider provider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (provider == null || provider.VerticalStripWipe == null)
            return;

        _graphic = provider.VerticalStripWipe.GetComponent<VerticalStripWipeGraphic>();
    }

    private void PrepareCoveredState()
    {
        if (_graphic == null)
            return;

        _graphic.gameObject.SetActive(true);
        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.stripCount,
            _spec.stripDelay,
            _spec.stripFillDuration,
            _spec.order);

        _graphic.Progress01 = 1f;
    }

    private void CommitFinalState()
    {
        if (_tween != null)
        {
            _tween.Kill(false);
            _tween = null;
        }

        if (_graphic != null)
        {
            DOTween.Kill(_graphic, false);
            _graphic.Progress01 = 0f;
            _graphic.gameObject.SetActive(false);
        }

        if (_spec.clearAllAfterOut)
            PresentationTransitionClearUtility.ClearAll();

        _canCommitFinalState = false;
        _graphic = null;
    }
}