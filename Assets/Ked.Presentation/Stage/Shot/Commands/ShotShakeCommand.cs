using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class ShotShakeCommandSpec : CommandSpecBase
{
    [Header("Shake")]
    [Tooltip("motions.json에서 읽은 카메라 리그의 클립(검증된 것). Camera_Shake에만 키가 있고 end는 restore다.")]
    public MotionClip clip;

    [Tooltip("키 값(중립에서 벗어난 만큼)에 곱한다. 1이면 클립 그대로.")]
    public float intensity = 1f;
}

// shot_shake — 화면 흔들림 클립을 한 번 튼다. 재생은 샷 시스템의 CameraShakePlayer가 소유한다.
//
// 의도를 쓰지 않으므로 샷 커맨드의 틀(ShotIntentCommandBase)을 쓰지 않는다. 수명은 화면 플래시 · restore 모션과 같다:
//   - 압축(스킵 · 시크) 중에는 틀지 않는다 — 끝이 0이라 남길 상태가 없다. 돌던 흔들림은 끊는다.
//   - 스텝 경계에서 바로 0으로 끊는다(가속 없음 — 흔들림에는 "남은 거리"가 없다).
public sealed class ShotShakeCommand : CommandBase
{
    private readonly PresentationShotResponseSystem _rig;
    private readonly ShotShakeCommandSpec _spec;

    public override bool WaitForCompletion => _spec.wait;

    public ShotShakeCommand(PresentationShotResponseSystem rig, ShotShakeCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _rig.Shake.Play(_spec.clip, _spec.intensity);

        if (!_spec.wait)
            yield break;

        while (_rig.Shake.IsPlaying)
            yield return null;
    }

    protected override void OnSkip()
    {
        _rig.Shake.Stop();
    }

    protected override void OnStepLifetimeFinished()
    {
        _rig.Shake.Stop();
    }
}
