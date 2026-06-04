using System;
using System.Collections;
using DG.Tweening;

[Serializable]
public abstract class ShotIntentCommandSpecBase : CommandSpecBase
{
    public float duration = 0.45f;

    public Ease ease = Ease.OutCubic;

    // killTween 제거:
    // rig가 단일 shot 드라이버를 소유하므로, 새 shot 커맨드는 항상 현재 visual에서
    // 다음 목표로 재타깃된다. 더 이상 커맨드별 kill 플래그가 필요 없다.
}

public abstract class ShotIntentCommandBase<TSpec> : CommandBase
    where TSpec : ShotIntentCommandSpecBase
{
    protected readonly PresentationResponseRig rig;
    protected readonly TSpec spec;

    private PresentationIntentState _toState;

    public override bool WaitForCompletion => spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    protected ShotIntentCommandBase(PresentationResponseRig rig, TSpec spec)
    {
        this.rig = rig;
        this.spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (rig == null)
            yield break;

        // from = 결정론적 logical 상태 (visual의 중간값이 아니다).
        _toState = BuildTargetState(rig.CurrentState, scope);

        if (spec.duration <= 0f)
        {
            rig.SetShotImmediate(_toState);
            yield break;
        }

        // logical은 즉시 _toState로 확정되고(롤백과 동일), visual만 현재값에서 ease한다.
        rig.DriveShotTo(_toState, spec.duration, spec.ease);

        if (!spec.wait)
            yield break;

        // wait면 화면이 목표에 닿을 때까지 시퀀스를 막는다.
        while (rig != null && rig.IsShotDriving)
        {
            if (scope.Token.IsCancellationRequested)
                yield break;

            if (scope.IsSkipping)
            {
                rig.SetShotImmediate(_toState);
                yield break;
            }

            yield return null;
        }
    }

    // logical은 ExecuteInner/OnSkip에서 이미 확정
    // 드라이버를 rig가 소유하므로 스텝 경계에서 커맨드가 최종값을 강제 커밋하지 않는다.
    // 다음 shot 커맨드가 있으면 현재 visual에서 재타깃 → 점프가 발생하지 않는다.
    protected override void OnSkip(CommandRunScope scope)
    {
        if (rig == null)
            return;

        _toState = BuildTargetState(rig.CurrentState, scope);
        rig.SetShotImmediate(_toState);
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope) => OnSkip(scope);

    protected abstract PresentationIntentState BuildTargetState(
        in PresentationIntentState from,
        CommandRunScope scope);
}