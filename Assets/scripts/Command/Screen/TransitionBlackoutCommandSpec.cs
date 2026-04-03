using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Yarn;

[Serializable]
[CommandMenuHint("Transition", "Blackout (Cover+Uncover)", Order = -949)]
public sealed class TransitionBlackoutCommandSpec : CommandSpecBase
{
    [Header("Durations")]
    public float coverDuration = 0.20f;
    public float uncoverDuration = 0.20f;

    public Ease coverEase = Ease.InOutSine;
    public Ease uncoverEase = Ease.InOutSine;

    [Header("Swap Signal")]
    [Tooltip("Swap 지점에서 Raise할 시그널 키(비우면 Raise 안 함)")]
    public string swapSignalKey = "transition.swap";

    [Tooltip("커버 끝나고 언커버 시작 전 홀드(초)")]
    public float holdSeconds = 0f;

    [Header("Wait")]
    [Tooltip("전체 전환이 끝날 때까지 Step을 막을지")]
    public bool wait = true;

    [Header("Options")]
    public bool killTween = true;
    public bool blockRaycastsWhileFading = true;
    public bool autoBlockByAlpha = true;

    [Header("Debug")]
    public bool warnIfMissing = true;
}

public sealed class TransitionBlackoutCommand : CommandBase, IStepScopedCommand
{
    private readonly TransitionBlackoutCommandSpec _spec;
    private readonly UnitySignalBus _unitySignalBus;

    private CanvasGroup _cg;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public TransitionBlackoutCommand(UnitySignalBus unitySignalBus, TransitionBlackoutCommandSpec spec)
    {
        _unitySignalBus = unitySignalBus;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_cg == null)
            yield break;

        if (_spec.killTween)
            _cg.DOKill(false);

        if (_spec.blockRaycastsWhileFading)
        {
            _cg.blocksRaycasts = true;
            _cg.interactable = false;
        }

        // Skip이면 한 번에 처리: 커버 상태에서 swap 신호 -> 언커버까지 즉시
        if (scope != null && scope.IsSkipping)
        {
            SetAlpha(1f, true);
            RaiseSwapSignal(scope);
            SetAlpha(0f, false);
            yield break;
        }

        // 1) Cover
        if (_spec.coverDuration <= 0f) SetAlpha(1f, true);
        else
        {
            Tween tCover = _cg.DOFade(1f, _spec.coverDuration).SetEase(_spec.coverEase).SetUpdate(true);
            if (_spec.wait) yield return tCover.WaitForCompletion();
        }

        // 2) Swap point
        RaiseSwapSignal(scope);

        // 3) Hold
        if (_spec.holdSeconds > 0f && _spec.wait)
        {
            float t = 0f;
            while (t < _spec.holdSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 4) Uncover
        if (_spec.uncoverDuration <= 0f) SetAlpha(0f, false);
        else
        {
            Tween tUncover = _cg.DOFade(0f, _spec.uncoverDuration).SetEase(_spec.uncoverEase).SetUpdate(true);
            if (_spec.wait) yield return tUncover.WaitForCompletion();
        }

        ApplyBlockByAlphaIfNeeded();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (_cg == null) return;

        _cg.DOKill(false);

        // 완료는 안전하게 언커버 상태로 (보통 “전환 끝”이므로)
        SetAlpha(0f, false);
        ApplyBlockByAlphaIfNeeded();

        _cg = null;
    }

    private void RaiseSwapSignal(CommandRunScope scope)
    {
        if (string.IsNullOrWhiteSpace(_spec.swapSignalKey))
            return;

        // 예: scope.Signals.Raise(_spec.swapSignalKey);
        // 또는 scope.Session.RaiseSignal(...)
        _unitySignalBus.Raise(_spec.swapSignalKey);
    }

    private void SetAlpha(float a, bool block)
    {
        _cg.alpha = Mathf.Clamp01(a);
        _cg.blocksRaycasts = block;
        _cg.interactable = false;
    }

    private void ApplyBlockByAlphaIfNeeded()
    {
        if (!_spec.autoBlockByAlpha) return;
        bool block = _cg.alpha > 0.0001f;
        _cg.blocksRaycasts = block;
        _cg.interactable = false;
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        var blackout = TransitionOverlay.GetOrCreate();
        if (blackout == null)
        {
            if (_spec.warnIfMissing)
                Debug.LogWarning("[TransitionBlackoutCommand] ScreenBlackout not found.");
            return;
        }

        _cg = blackout.CanvasGroup;
    }
}
