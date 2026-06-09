using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public interface IPresentationTransitionSlotProvider
{
    RectTransform VerticalStripWipe { get; }
    RectTransform SlantedShutter { get; }
    RectTransform FocusBlurFade { get; }
    RectTransform FocusBlurCurtain { get; }
    RectTransform SlantedMaskEdgeGraphic { get; }
}

public sealed partial class PresentationUIRoot : IPresentationTransitionSlotProvider
{
    public RectTransform VerticalStripWipe => View.Rect(Refs.VerticalStripWipe);
    public RectTransform SlantedShutter => View.Rect(Refs.SlantedShutter);
    public RectTransform FocusBlurFade => View.Rect(Refs.FocusBlurFade);
    public RectTransform FocusBlurCurtain => View.Rect(Refs.FocusBlurCurtain);
    public RectTransform SlantedMaskEdgeGraphic => View.Rect(Refs.Stage01_Root);
}

public enum VerticalStripWipeMode
{
    Cover = 0,
    Clear = 1
}

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Vertical Strip Wipe",
    Order = -850)]
public sealed class VerticalStripWipeCommandSpec : CommandSpecBase
{
    [Header("Wipe")]
    public VerticalStripWipeMode mode = VerticalStripWipeMode.Cover;
    public VerticalStripWipeOrder order = VerticalStripWipeOrder.LeftToRight;

    [Header("Strips")]
    public int stripCount = 20;
    public float stripDelay = 0.02f;
    public float stripFillDuration = 0.08f;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    [Tooltip("0 이하이면 stripDelay와 stripFillDuration으로 계산된 전체 시간을 사용합니다.")]
    public float duration = 0f;

    public Ease ease = Ease.Linear;

    [Header("Options")]
    public bool disableWhenClear = true;
}

public sealed class VerticalStripWipeCommand : CommandBase
{
    private readonly VerticalStripWipeCommandSpec _spec;

    private VerticalStripWipeGraphic _graphic;
    private float _startProgress;
    private float _finalProgress;
    private float _duration;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public VerticalStripWipeCommand(VerticalStripWipeCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_graphic == null)
            yield break;

        ClaimTarget();

        if (scope.IsRollbackSeeking || _duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _graphic.gameObject.SetActive(true);
        _graphic.Progress01 = _startProgress;

        Tween tween = DOTween
            .To(
                () => _startProgress,
                value =>
                {
                    _graphic.Progress01 = value;
                },
                _finalProgress,
                _duration)
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
            ResolveRefs(scope);

        if (_graphic == null)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider transitionSlotProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        RectTransform rect = transitionSlotProvider.VerticalStripWipe;

        _graphic = rect.GetComponent<VerticalStripWipeGraphic>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        ApplyConfig();

        _startProgress = _spec.mode == VerticalStripWipeMode.Cover ? 0f : 1f;
        _finalProgress = _spec.mode == VerticalStripWipeMode.Cover ? 1f : 0f;

        _duration = _spec.duration > 0f
            ? _spec.duration
            : _graphic.TotalDuration;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        ApplyConfig();

        _graphic.Progress01 = _finalProgress;

        if (_spec.disableWhenClear && _finalProgress <= 0f)
            _graphic.gameObject.SetActive(false);

        HasClaimedTarget = false;
    }

    private void ApplyConfig()
    {
        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.stripCount,
            _spec.stripDelay,
            _spec.stripFillDuration,
            _spec.order);
    }
}