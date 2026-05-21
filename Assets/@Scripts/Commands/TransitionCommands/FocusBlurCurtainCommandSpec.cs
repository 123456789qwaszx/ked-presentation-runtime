using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum FocusBlurCurtainMode
{
    Close = 0,
    Open = 1
}

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Focus Blur Curtain",
    Order = -848)]
public sealed class FocusBlurCurtainCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Mode")]
    public FocusBlurCurtainMode mode = FocusBlurCurtainMode.Close;

    [Header("Curtain Shape")]
    public float openGapHeight = 520f;
    public float finalGapHeight = 0f;
    public float slantPixels = 90f;

    [Header("Soft Edge")]
    public float edgeFeatherHeight = 140f;

    [Range(0f, 1f)]
    public float edgeFeatherAlpha = 0.55f;

    [Header("Center Blur Fake")]
    public float centerBlurHeight = 320f;

    [Range(0f, 1f)]
    public float centerStartAlpha = 0.12f;

    [Range(0f, 1f)]
    public float centerEndAlpha = 0.82f;

    public int centerBlurSlices = 18;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.InOutCubic;

    [Header("Options")]
    public bool killTween = true;
    public bool disableWhenOpen = true;
    public bool blockRaycastWhenClosed = false;
}

public sealed class FocusBlurCurtainCommand : CommandBase, IStepScopedCommand
{
    private readonly FocusBlurCurtainCommandSpec _spec;

    private FocusBlurCurtainGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private float _finalProgress;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FocusBlurCurtainCommand(FocusBlurCurtainCommandSpec spec)
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

        float startProgress = _spec.mode == FocusBlurCurtainMode.Close ? 0f : 1f;
        _finalProgress = _spec.mode == FocusBlurCurtainMode.Close ? 1f : 0f;

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking)
        {
            CommitFinalState();
            yield break;
        }

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _graphic.gameObject.SetActive(true);
        _graphic.Progress01 = startProgress;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    float value = Mathf.Lerp(startProgress, _finalProgress, e);

                    _graphic.Progress01 = value;
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
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

            bool isClosed = _finalProgress >= 1f;
            bool isOpen = _finalProgress <= 0f;

            _graphic.RaycastBlocking = _spec.blockRaycastWhenClosed && isClosed;

            if (_spec.disableWhenOpen && isOpen)
                _graphic.gameObject.SetActive(false);
        }

        _canCommitFinalState = false;
        _graphic = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        //***
        // RectTransform rect = PresentationTargetResolver.ResolveRect(
        //     scope,
        //     _spec.target,
        //     _spec.strict,
        //     nameof(FocusBlurCurtainCommand));

        RectTransform rect =   UIManager.Instance.GetUI<PresentationUIRoot>().Stage00BackgroundSlot;
        if (rect == null)
            return;

        _graphic = rect.GetComponent<FocusBlurCurtainGraphic>();

        if (_graphic == null && _spec.strict)
        { }
    }

    private void ApplyConfig()
    {
        if (_graphic == null)
            return;

        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.openGapHeight,
            _spec.finalGapHeight,
            _spec.slantPixels,
            _spec.edgeFeatherHeight,
            _spec.edgeFeatherAlpha,
            _spec.centerBlurHeight,
            _spec.centerStartAlpha,
            _spec.centerEndAlpha,
            _spec.centerBlurSlices);

        _graphic.RaycastBlocking = false;
    }
}