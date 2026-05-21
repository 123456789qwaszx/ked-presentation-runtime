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
public sealed class VerticalStripWipeCommandSpec : PresentationTargetCommandSpecBase
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
    public bool killTween = true;
    public bool disableWhenClear = true;
}

public sealed class VerticalStripWipeCommand : CommandBase, IStepScopedCommand
{
    private readonly VerticalStripWipeCommandSpec _spec;

    private VerticalStripWipeGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private float _finalProgress;

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

        if (_spec.killTween)
            DOTween.Kill(_graphic, true);

        ApplyConfig();

        float startProgress = _spec.mode == VerticalStripWipeMode.Cover ? 0f : 1f;
        _finalProgress = _spec.mode == VerticalStripWipeMode.Cover ? 1f : 0f;

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking)
        {
            CommitFinalState();
            yield break;
        }

        float duration = _spec.duration > 0f
            ? _spec.duration
            : _graphic.TotalDuration;

        if (duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _graphic.gameObject.SetActive(true);
        _graphic.Progress01 = startProgress;

        _tween = DOTween
            .To(
                () => startProgress,
                value =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    _graphic.Progress01 = value;
                },
                _finalProgress,
                duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_graphic)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _graphic == null)
                    return;

                CommitFinalState();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_graphic == null)
            return;

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _graphic == null)
            return;

        _tween?.Kill(false);
        DOTween.Kill(_graphic, false);

        CommitFinalState();
    }

    private void CommitFinalState()
    {
        if (_graphic != null)
        {
            ApplyConfig();
            _graphic.Progress01 = _finalProgress;

            if (_spec.disableWhenClear && _finalProgress <= 0f)
                _graphic.gameObject.SetActive(false);
        }

        _canCommitFinalState = false;
        _graphic = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider transitionSlotProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        RectTransform rect = transitionSlotProvider.VerticalStripWipe;
        if (rect == null)
            return;

        _graphic = rect.GetComponent<VerticalStripWipeGraphic>();

        if (_graphic == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[VerticalStripWipeCommand] Target '{_spec.target}' does not have VerticalStripWipeGraphic.");
        }
    }

    private void ApplyConfig()
    {
        if (_graphic == null)
            return;

        _graphic.color = _spec.color;
        _graphic.Configure(
            _spec.stripCount,
            _spec.stripDelay,
            _spec.stripFillDuration,
            _spec.order);
    }
}