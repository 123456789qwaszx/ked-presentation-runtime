using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum SlantedShutterMode
{
    Close = 0,
    Open = 1
}

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Slanted Shutter",
    Order = -849)]
public sealed class SlantedShutterCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Mode")]
    public SlantedShutterMode mode = SlantedShutterMode.Close;

    [Header("Shape")]
    public float slantPixels = 140f;
    public float openGapHeight = 420f;
    public float finalGapHeight = 0f;

    [Header("Center Exposure")]
    public float centerBandHeight = 260f;

    [Range(0f, 1f)]
    public float centerStartAlpha = 0.25f;

    [Range(0f, 1f)]
    public float centerEndAlpha = 1f;

    [Header("Visual")]
    public Color color = Color.black;

    [Header("Tween")]
    public float duration = 0.38f;
    public Ease ease = Ease.OutCubic;

    [Header("Options")]
    public bool killTween = true;
    public bool disableWhenOpen = true;
    public bool blockRaycastWhileClosed = false;
}

public sealed class SlantedShutterCommand : CommandBase, IStepScopedCommand
{
    private readonly SlantedShutterCommandSpec _spec;

    private SlantedShutterGraphic _graphic;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private float _finalProgress;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlantedShutterCommand(SlantedShutterCommandSpec spec)
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

        float startProgress = _spec.mode == SlantedShutterMode.Close ? 0f : 1f;
        _finalProgress = _spec.mode == SlantedShutterMode.Close ? 1f : 0f;

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
        _graphic.RaycastBlocking = _spec.blockRaycastWhileClosed && startProgress >= 1f;

        _tween = DOTween
            .To(
                () => startProgress,
                value =>
                {
                    if (!_canCommitFinalState || _graphic == null)
                        return;

                    _graphic.Progress01 = value;
                    _graphic.RaycastBlocking =
                        _spec.blockRaycastWhileClosed && value >= 0.98f;
                },
                _finalProgress,
                _spec.duration)
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

            bool isClosed = _finalProgress >= 1f;
            bool isOpen = _finalProgress <= 0f;

            _graphic.RaycastBlocking = _spec.blockRaycastWhileClosed && isClosed;

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

        _graphic = rect.GetComponent<SlantedShutterGraphic>();

        if (_graphic == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[SlantedShutterCommand] Target '{_spec.target}' does not have SlantedShutterGraphic.");
        }
    }

    private void ApplyConfig()
    {
        if (_graphic == null)
            return;

        _graphic.color = _spec.color;

        _graphic.Configure(
            _spec.slantPixels,
            _spec.openGapHeight,
            _spec.finalGapHeight,
            _spec.centerBandHeight,
            _spec.centerStartAlpha,
            _spec.centerEndAlpha);
    }
}