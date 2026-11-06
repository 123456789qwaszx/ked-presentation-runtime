using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// "클레임-트윈" 커맨드의 공통 수명 —
/// 57곳에 흩어져 있던 미니 상태머신(ResolveRefs → ClaimTarget → 트윈 → Commit /
/// OnSkip / OnStepLifetimeFinished 가속)을 한 곳으로 모은 것.
/// </summary>
public abstract class ClaimTweenCommandBase : CommandBase
{
    private Tween _tween;
    private bool _resolved;

    private bool _hasClaimedTarget;

    // 확정할 때 함께 풀 예약. ClaimTarget에서 예약을 거는 쪽(장부)이 스스로 등록한다.
    protected CommitReleases Releases { get; } = new();

    // 0이하일 시 즉시 스냅
    protected abstract float TweenDuration { get; }

    // 대상 해석(한 번만 불린다). 이 훅과 ClaimTarget은 스코프를 받지 않는다 — 무대는 커맨드가 생성자로 받고,
    // 스코프의 실행 상태(취소 · 압축 · 스텝 경계)는 이 기반이 다룬다.
    protected abstract void ResolveTargets();

    // DOKill -> (from-override) -> 목표 계산 및 확정(코어 리덕션). 목표는 언제나 나온다.
    protected abstract void ClaimTarget();

    // ease, SetTarget은 구현 커맨드가 직접. (SetUpdate,OnComplete는 Base측 정책)
    protected abstract Tween CreateTween(float duration);

    // 최종 쓰기. (수명 플래그 정리는 Base측 정책)
    protected abstract void OnCommitFinalState();

    // 목표까지 남은 진행 비율 [0,1]. 0이면 가속할 것이 없다는 뜻이라 즉시 확정.
    // 여러 축이면 축별 비율의 최댓값을 사용(가장 늦게 도착하는 축이 기준).
    protected abstract float MeasureRemainingRatio();

    protected virtual float StepFinishSpeedUpMultiplier => 30f;

    // 스텝 경계 가속용 트윈. 기존 트윈을 다시 만드는 것.(잔여시간 기반 재계산)
    // 다단 연출 커맨드(PivotRotateTo)는 이걸로 덮음.
    // "현재 위치에서 목표까지"만 남기고 마무리해야 하기 때문.
    protected virtual Tween CreateAcceleratedTween(float duration) => CreateTween(duration);

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolved)
        {
            _resolved = true;
            ResolveTargets();
        }
        
        ClaimTarget();
        _hasClaimedTarget = true;

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
        if (!_resolved)
        {
            _resolved = true;
            ResolveTargets();
        }

        if (!_hasClaimedTarget)
        {
            ClaimTarget();
            _hasClaimedTarget = true;
        }

        Commit();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!_hasClaimedTarget)
            return;

        _tween?.Kill(false);

        // 스텝 경계 — 남은 만큼을 빠르게 마저 간다. 남은 것이 없으면(비율 0) 곧장 확정한다.
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

    private void Commit()
    {
        OnCommitFinalState();
        Releases.ReleaseAll();

        _hasClaimedTarget = false;
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

    /// <summary>한 축의 남은 비율. 출발했던 거리 대비 남은 거리. Clamp01</summary>
    protected static float RemainingRatio(float originalDistance, float remainingDistance)
    {
        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        return Mathf.Clamp01(remainingDistance / originalDistance);
    }
}