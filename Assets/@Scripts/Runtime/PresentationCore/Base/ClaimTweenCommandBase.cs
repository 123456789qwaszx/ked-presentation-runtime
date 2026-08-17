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
///   ResolveTargets    — 유니티 참조 해석 (한 번만 불린다)
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
    private bool _resolved;

    protected bool HasClaimedTarget { get; private set; }

    // ≤ 0이면 즉시 확정(스냅)
    protected abstract float TweenDuration { get; }

    /// <summary>유니티 참조 해석. 한 번만 불린다.</summary>
    protected abstract void ResolveTargets(CommandRunScope scope);

    /// <summary>
    /// DOKill → (from-override) → 목표 계산(코어 리덕션) → (장부 게시).
    /// 이 메서드가 끝나면 목표값이 확정돼 있어야 한다. HasClaimedTarget은 기반이 올린다.
    /// </summary>
    protected abstract void ClaimTarget(CommandRunScope scope);

    /// <summary>목표를 향한 트윈. ease·SetTarget은 파생이, SetUpdate·OnComplete는 기반이 건다.</summary>
    protected abstract Tween CreateTween(float duration);

    /// <summary>최종 쓰기 + 장부 Clear. 수명 플래그 정리는 기반이 함.</summary>
    protected abstract void OnCommitFinalState();

    /// <summary>
    /// 목표까지 남은 진행 비율 [0,1]. 0이면 가속할 것이 없다는 뜻이라 즉시 확정.
    /// 여러 축이면 축별 비율의 최댓값을 사용(가장 늦게 도착하는 축이 기준).
    /// </summary>
    protected abstract float MeasureRemainingRatio();

    // 스텝 경계에서 가속 재트윈을 만들 것인가. 진행률(0→1) 트윈 커맨드는 재시작이
    // 처음부터 다시 도는 것이라 무의미하므로 false — 즉시 확정한다 (RotateBy 계열).
    protected virtual bool AccelerateOnStepFinish => true;

    protected virtual float StepFinishSpeedUpMultiplier => 30f;

    /// <summary>
    /// 스텝 경계 가속용 트윈. 기본은 본 트윈을 잔여시간으로 다시 만드는 것이다.
    /// 다단 연출(예열·오버슈트)처럼 "현재 위치에서 목표까지"만 남기고 마무리해야 하는
    /// 커맨드는 이걸 덮어 별도 트윈을 낸다 (PivotRotateTo).
    /// </summary>
    protected virtual Tween CreateAcceleratedTween(float duration) => CreateTween(duration);

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ResolveOnce(scope);
        Claim(scope);

        float duration = TweenDuration;

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
        ResolveOnce(scope);

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

    private void ResolveOnce(CommandRunScope scope)
    {
        if (_resolved)
            return;

        _resolved = true;
        ResolveTargets(scope);
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
