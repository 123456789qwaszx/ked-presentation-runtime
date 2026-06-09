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
public sealed class SlantedShutterCommandSpec : CommandSpecBase
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
    public bool disableWhenOpen = true;
    public bool blockRaycastWhileClosed = false;
}

public sealed class SlantedShutterCommand : CommandBase
{
    private readonly SlantedShutterCommandSpec _spec;

    private SlantedShutterGraphic _graphic;
    private float _startProgress;
    private float _finalProgress;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

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

        ClaimTarget();

        if (scope.IsRollbackSeeking || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _graphic.gameObject.SetActive(true);
        _graphic.Progress01 = _startProgress;
        _graphic.RaycastBlocking = _spec.blockRaycastWhileClosed && _startProgress >= 1f;

        Tween tween = DOTween
            .To(
                () => _startProgress,
                value =>
                {
                    _graphic.Progress01 = value;
                    _graphic.RaycastBlocking =
                        _spec.blockRaycastWhileClosed && value >= 0.98f;
                },
                _finalProgress,
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

        RectTransform rect = transitionSlotProvider.SlantedShutter;
        _graphic = rect.GetComponent<SlantedShutterGraphic>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_graphic, true);

        ApplyConfig();

        _startProgress = _spec.mode == SlantedShutterMode.Close ? 0f : 1f;
        _finalProgress = _spec.mode == SlantedShutterMode.Close ? 1f : 0f;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        ApplyConfig();

        _graphic.Progress01 = _finalProgress;

        bool isClosed = _finalProgress >= 1f;
        bool isOpen = _finalProgress <= 0f;

        _graphic.RaycastBlocking = _spec.blockRaycastWhileClosed && isClosed;

        if (_spec.disableWhenOpen && isOpen)
            _graphic.gameObject.SetActive(false);

        HasClaimedTarget = false;
    }

    private void ApplyConfig()
    {
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