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
    [Tooltip("0 이하이면 stripDelay와 stripFillDuration으로 계산된 전체 시간을 사용합니다.")]
    public float duration = 0.4f;

    public Ease ease = Ease.Linear;
}

public sealed class TransitionOutStripCommand : CommandBase
{
    private readonly TransitionOutStripCommandSpec _spec;

    private VerticalStripWipeGraphic _graphic;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => true;

    public TransitionOutStripCommand(TransitionOutStripCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = DOTween
            .To(
                () => 1f,
                value => _graphic.Progress01 = value,
                0f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        _graphic = provider.VerticalStripWipe.GetComponent<VerticalStripWipeGraphic>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        PrepareCoveredState();

        PresentationTransitionClearUtility.ClearAllExcept(PresentationTransitionLayer.VerticalStripWipe);

        HasClaimedTarget = true;
    }

    private void PrepareCoveredState()
    {
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
        _graphic.Progress01 = 0f;
        _graphic.gameObject.SetActive(false);

        PresentationTransitionClearUtility.ClearAll();

        HasClaimedTarget = false;
    }
}