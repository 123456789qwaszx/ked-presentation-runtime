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
public sealed class FocusBlurFadeCommandSpec : CommandSpecBase
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
    public bool disableWhenClear = true;
    public bool blockRaycastWhenVisible = false;
}

public sealed class FocusBlurFadeCommand : CommandBase
{
    private readonly FocusBlurFadeCommandSpec _spec;

    private FocusBlurFadeOverlay _overlay;

    private float _startAlpha;
    private float _finalAlpha;
    private float _startZoomAmount;
    private float _finalZoomAmount;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

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

        ClaimTarget();

        if (scope.IsSeekPassThrough || _spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _overlay.gameObject.SetActive(true);
        _overlay.SetAlpha(_startAlpha);
        _overlay.SetZoomAmount(_startZoomAmount);

        Tween tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    float alpha = Mathf.Lerp(_startAlpha, _finalAlpha, e);
                    float zoom = Mathf.Lerp(_startZoomAmount, _finalZoomAmount, e);

                    _overlay.SetAlpha(alpha);
                    _overlay.SetZoomAmount(zoom);
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_overlay)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_overlay == null)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        IPresentationTransitionSlotProvider transitionSlotProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        RectTransform rect = transitionSlotProvider.FocusBlurFade;
        _overlay = rect.GetComponent<FocusBlurFadeOverlay>();
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_overlay, true);

        ApplyConfig();

        _startAlpha = _spec.mode == FocusBlurFadeMode.FadeOut
            ? 0f
            : _spec.maxAlpha;

        _finalAlpha = _spec.mode == FocusBlurFadeMode.FadeOut
            ? _spec.maxAlpha
            : 0f;

        _startZoomAmount = _spec.mode == FocusBlurFadeMode.FadeOut
            ? 0f
            : _spec.zoomAmount;

        _finalZoomAmount = _spec.mode == FocusBlurFadeMode.FadeOut
            ? _spec.zoomAmount
            : 0f;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        ApplyConfig();

        _overlay.SetAlpha(_finalAlpha);
        _overlay.SetZoomAmount(_finalZoomAmount);

        if (_spec.disableWhenClear && _finalAlpha <= 0f)
        {
            _overlay.ResetZoom();
            _overlay.gameObject.SetActive(false);
        }

        HasClaimedTarget = false;
    }

    private void ApplyConfig()
    {
        _overlay.SetColor(_spec.color);
        _overlay.BlockRaycastWhenVisible = _spec.blockRaycastWhenVisible;
    }
}