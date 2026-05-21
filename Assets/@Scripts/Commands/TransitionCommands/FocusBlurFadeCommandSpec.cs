using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public enum FocusBlurFadeMode
{
    FadeOut = 0,
    FadeIn = 1
}

[Serializable]
[CommandMenuHint(
    "Presentation Transition",
    "Focus Blur Fade",
    Order = -848)]
public sealed class FocusBlurFadeCommandSpec : PresentationTargetCommandSpecBase
{
    [Header("Mode")]
    public FocusBlurFadeMode mode = FocusBlurFadeMode.FadeOut;

    [Header("Visual")]
    public Color color = Color.black;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("Fake Focus Blur")]
    [Tooltip("실제 blur shader 대신, 화면을 살짝 확대해서 초점이 무너지는 느낌을 만듭니다.")]
    public float zoomAmount = 0.035f;

    [Header("Tween")]
    public float duration = 0.45f;
    public Ease ease = Ease.InOutCubic;

    [Header("Options")]
    public bool killTween = true;
    public bool disableWhenClear = true;
    public bool blockRaycastWhenVisible = false;
}

public sealed class FocusBlurFadeCommand : CommandBase, IStepScopedCommand
{
    private readonly FocusBlurFadeCommandSpec _spec;

    private FocusBlurFadeOverlay _overlay;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private float _finalAlpha;
    private float _finalZoomAmount;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public FocusBlurFadeCommand(FocusBlurFadeCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_overlay == null)
            yield break;

        if (_spec.killTween)
            DOTween.Kill(_overlay, true);

        ApplyConfig();

        float startAlpha = _spec.mode == FocusBlurFadeMode.FadeOut
            ? 0f
            : _spec.maxAlpha;

        _finalAlpha = _spec.mode == FocusBlurFadeMode.FadeOut
            ? _spec.maxAlpha
            : 0f;

        float startZoomAmount = _spec.mode == FocusBlurFadeMode.FadeOut
            ? 0f
            : _spec.zoomAmount;

        _finalZoomAmount = _spec.mode == FocusBlurFadeMode.FadeOut
            ? _spec.zoomAmount
            : 0f;

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

        _overlay.gameObject.SetActive(true);
        _overlay.SetAlpha(startAlpha);
        _overlay.SetZoomAmount(startZoomAmount);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _overlay == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    float alpha = Mathf.Lerp(startAlpha, _finalAlpha, e);
                    float zoom = Mathf.Lerp(startZoomAmount, _finalZoomAmount, e);

                    _overlay.SetAlpha(alpha);
                    _overlay.SetZoomAmount(zoom);
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_overlay)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _overlay == null)
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

        if (_overlay == null)
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

        if (!_canCommitFinalState || _overlay == null)
            return;

        _tween?.Kill(false);
        DOTween.Kill(_overlay, false);

        CommitFinalState();
    }

    private void CommitFinalState()
    {
        if (_overlay != null)
        {
            ApplyConfig();

            _overlay.SetAlpha(_finalAlpha);
            _overlay.SetZoomAmount(_finalZoomAmount);

            if (_spec.disableWhenClear && _finalAlpha <= 0f)
            {
                _overlay.ResetZoom();
                _overlay.gameObject.SetActive(false);
            }
        }

        _canCommitFinalState = false;
        _overlay = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider transitionSlotProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        RectTransform rect = transitionSlotProvider.FocusBlurFade;

        _overlay = rect.GetComponent<FocusBlurFadeOverlay>();

        if (_overlay == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[FocusBlurFadeCommand] Target '{_spec.target}' does not have FocusBlurFadeOverlay.");
        }
    }

    private void ApplyConfig()
    {
        if (_overlay == null)
            return;

        _overlay.SetColor(_spec.color);
        _overlay.BlockRaycastWhenVisible = _spec.blockRaycastWhenVisible;
    }
}