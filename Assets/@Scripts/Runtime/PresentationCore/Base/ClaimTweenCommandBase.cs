using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// "클레임-트윈" 커맨드의 공통 수명 —
/// 57곳에 흩어져 있던 미니 상태머신(ResolveRefs → ClaimTarget → 트윈 → Commit /
/// OnSkip / OnStepLifetimeFinished 가속)을 한 곳으로 모은 것.
///
/// ShotIntentCommandBase가 shot 계열에 했던 통합을 일반화했다 — 그 덕에 shot 이관이
/// 커맨드당 몇 줄로 끝났던 것과 같은 효과를 노린다.
///
/// 파생이 채우는 것 (변형점):
///   TryResolveTargets — 유니티 참조 해석 (한 번만 불린다)
///   ClaimTarget       — DOKill → (from-override) → 목표 계산(코어 리덕션) → (게시)
///   CreateTween       — 목표를 향한 트윈 (ease·target 포함. SetUpdate/OnComplete는 여기서 건다)
///   OnCommitFinalState— 최종 쓰기 + 장부 Clear
///   MeasureRemainingRatio — 가속 잔여시간 계산용 남은 비율 (계량은 커맨드마다 다르다)
///
/// 적용 범위: 목표값이 있는 클레임-트윈 부류만이다. 절차적 연기(Tremble·Sway 등)는
/// 목표가 정의되지 않으므로 이 기반에 맞지 않는다 (reduction-boundary.md).
///
/// 검증: 이 리팩터의 오라클은 등가성 하네스다 — 이관 배치마다 랩드스킵 재판정으로
/// 리포트가 리팩터 전과 같은지 확인한다.
/// </summary>
public abstract class ClaimTweenCommandBase : CommandBase
{
    private Tween _tween;
    private bool _resolveAttempted;
    private bool _resolved;

    protected bool HasClaimedTarget { get; private set; }

    /// <summary>스펙의 트윈 시간. ≤ 0이면 즉시 확정(스냅)이다. 가속 잔여시간의 기준이기도 하다.</summary>
    protected abstract float TweenDuration { get; }

    /// <summary>
    /// 실제로 재생할 시간. 기본은 스펙 그대로다.
    /// 배속 재생에서 시간을 줄이는 커맨드(오버레이 계열)는 이걸 덮는다 —
    /// 가속 잔여시간의 기준은 종전대로 줄이지 않은 스펙 시간(TweenDuration)이다.
    /// </summary>
    protected virtual float ResolvePlaybackDuration(CommandRunScope scope) => TweenDuration;

    /// <summary>
    /// 유니티 참조 해석. 실패하면 커맨드를 건너뛴다 — 종전에는 이 경우 NRE로 터졌는데,
    /// 조용히 죽는 대신 경고를 남기고 빠지는 쪽으로 통일한다(실경로에서는 발생하지 않는다).
    /// </summary>
    protected abstract bool TryResolveTargets(CommandRunScope scope);

    /// <summary>
    /// DOKill → (from-override) → 목표 계산(코어 리덕션) → (장부 게시).
    /// 이 메서드가 끝나면 목표값이 확정돼 있어야 한다. HasClaimedTarget은 기반이 올린다.
    /// </summary>
    protected abstract void ClaimTarget(CommandRunScope scope);

    /// <summary>목표를 향한 트윈. ease·SetTarget은 파생이, SetUpdate·OnComplete는 기반이 건다.</summary>
    protected abstract Tween CreateTween(float duration);

    /// <summary>최종 쓰기 + 장부 Clear. 수명 플래그 정리는 기반이 한다.</summary>
    protected abstract void OnCommitFinalState();

    /// <summary>
    /// 목표까지 남은 진행 비율 [0,1]. 0이면 가속할 것이 없다는 뜻이라 즉시 확정한다.
    /// 계량은 커맨드마다 다르다(px·배율·각) — 한 축이면 RemainingRatio 하나로 끝나고,
    /// 여러 축이면 축별 비율의 최댓값을 쓴다(가장 늦게 도착하는 축이 기준이다).
    /// </summary>
    protected abstract float MeasureRemainingRatio();

    /// <summary>
    /// 스텝 경계에서 가속 재트윈을 만들 것인가. 진행률(0→1) 트윈 커맨드는 재시작이
    /// 처음부터 다시 도는 것이라 무의미하므로 false — 즉시 확정한다 (RotateBy 계열).
    /// </summary>
    protected virtual bool AccelerateOnStepFinish => true;

    /// <summary>
    /// 스텝 경계 가속 배율 — 남은 시간을 이만큼 나눠 붙인다. 기본은 사실상 즉시(30배)다.
    /// 화면 전체가 흔들려 보이는 시각효과는 그렇게 끊으면 튀어서 훨씬 완만하게 쓴다.
    /// </summary>
    protected virtual float StepFinishSpeedUpMultiplier => 30f;

    /// <summary>
    /// 스텝 경계 가속용 트윈. 기본은 본 트윈을 잔여시간으로 다시 만드는 것이다.
    /// 다단 연출(예열·오버슈트)처럼 "현재 위치에서 목표까지"만 남기고 마무리해야 하는
    /// 커맨드는 이걸 덮어 별도 트윈을 낸다 (PivotRotateTo).
    /// </summary>
    protected virtual Tween CreateAcceleratedTween(float duration) => CreateTween(duration);

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!ResolveOnce(scope))
            yield break;

        Claim(scope);

        float duration = ResolvePlaybackDuration(scope);

        if (duration <= 0f)
        {
            Commit();
            yield break;
        }

        _tween = CreateTween(duration)
            .SetUpdate(true)
            .OnComplete(Commit);

        if (WaitForCompletion)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!ResolveOnce(scope))
            return;

        if (!HasClaimedTarget)
            Claim(scope);

        Commit();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween?.Kill(false);

        if (!AccelerateOnStepFinish)
        {
            Commit();
            return;
        }

        float duration = CalculateAcceleratedRemainingDuration();

        if (duration <= 0f)
        {
            Commit();
            return;
        }

        _tween = CreateAcceleratedTween(duration)
            .SetUpdate(true)
            .OnComplete(Commit);
    }

    private bool ResolveOnce(CommandRunScope scope)
    {
        if (!_resolveAttempted)
        {
            _resolveAttempted = true;
            _resolved = TryResolveTargets(scope);

            if (!_resolved)
                Debug.LogWarning($"[{GetType().Name}] 대상 해석 실패 — 커맨드를 건너뛴다.");
        }

        return _resolved;
    }

    private void Claim(CommandRunScope scope)
    {
        ClaimTarget(scope);
        HasClaimedTarget = true;
    }

    private void Commit()
    {
        OnCommitFinalState();

        HasClaimedTarget = false;
        _tween = null;
    }

    private float CalculateAcceleratedRemainingDuration()
    {
        float remainingRatio = Mathf.Clamp01(MeasureRemainingRatio());

        if (remainingRatio <= 0f)
            return 0f;

        float remainingDuration = TweenDuration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }

    /// <summary>한 축의 남은 비율. 출발했던 거리 대비 남은 거리이며, 양끝은 0으로 접는다.</summary>
    protected static float RemainingRatio(float originalDistance, float remainingDistance)
    {
        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }
}
