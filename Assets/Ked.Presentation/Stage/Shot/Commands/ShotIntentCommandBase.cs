using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
public abstract class ShotIntentCommandSpecBase : CommandSpecBase
{
    [Header("Tween")]
    [Tooltip("0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;
}

// 샷 커맨드는 다음 의도를 계산해 넘기기만 한다 — 전환은 PresentationShotResponseSystem이 소유한다.
// 출발점이 화면에 보이는 값이 아니라 의도라서, 앞 전환이 얼마나 갔는지와 무관하게 결과가 같다.
//
// 커맨드는 제가 조준하는 축(Axes)을 밝힌다. 시스템은 다음 의도에서 그 축의 값만 읽고, 나머지 축은 건드리지 않는다 —
// 기다림 · 압축 · 스텝 경계도 제 축에만 건다.
//
// 줄을 넘는지는 커맨드가 아니라 축이 정한다. 렌즈 · 롤은 그 줄의 연기라 다음 줄이 열리면 서둘러 끝낸다(×20).
// 몸(트랙 · 달리)은 장면의 구도라 서두르지 않는다 — 여러 줄에 걸친 느린 이동이 제 길이로 간다(화면 효과와 같은 부류).
public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    private const float StepFinishSpeedUpMultiplier = 20f;

    // 스텝 경계에서 서둘러 끝내는 축 — 몸은 빠진다.
    private const ShotAxes HurriedAtStepEnd = ShotAxes.Lens | ShotAxes.Roll;

    protected readonly PresentationShotResponseSystem rig;
    protected readonly TSpec spec;

    public override bool WaitForCompletion => spec.wait;

    protected ShotIntentCommandBase(PresentationShotResponseSystem rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    /// <summary>이 커맨드가 다시 조준하는 축. BuildTargetState가 바꾸는 값과 같아야 한다.</summary>
    protected abstract ShotAxes Axes { get; }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        rig.Follow(BuildTargetState(rig.Intent), Axes, spec.duration, spec.ease);

        if (!spec.wait)
            yield break;

        while (rig.IsTransitioning(Axes))
            yield return null;
    }

    protected override void OnSkip()
    {
        rig.Snap(BuildTargetState(rig.Intent), Axes);
    }

    protected abstract PresentationIntentState BuildTargetState(in PresentationIntentState from);

    protected override void OnStepLifetimeFinished()
    {
        rig.AccelerateTransition(Axes & HurriedAtStepEnd, StepFinishSpeedUpMultiplier);
    }
}