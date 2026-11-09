using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

[Serializable]
public sealed class CutInCloseCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";
}

// cutin_close — 무대 슬롯에 퇴장 모션을 걸기만 한다. 끝에서 슬롯이 닫힘(창 배율 0)으로 둔다(StageCutInSlot.PlayClose).
public sealed class CutInCloseCommand : CommandBase
{
    private readonly CutInCloseCommandSpec _spec;
    private readonly CutInPresetDBSO _preset;
    private readonly StageRigRegistry _stages;

    private StageCutInSlot _slot;
    private MotionClip _clip;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public CutInCloseCommand(
        CutInCloseCommandSpec spec,
        CutInPresetDBSO preset,
        StageRigRegistry stages)
    {
        _spec = spec;
        _preset = preset;
        _stages = stages;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasCutInToClose())
            yield break;

        _slot.PlayClose(_clip);

        if (!_spec.wait)
            yield break;

        while (_slot.IsPlayingMotion)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasCutInToClose())
            return;

        _slot.SnapClose();
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _slot = _stages.Get(_spec.stage).CutIn;
        _clip = _preset.PopOut;

        if (!MotionRules.TryValidate(_clip, null, out string why))
        {
            Debug.LogWarning($"[CutInCloseCommand] 퇴장 클립이 유효하지 않다 ({_preset.name}): {why}. 바로 닫는다.");
            _clip = null;
        }
    }

    // 열린 컷인은 실행 시점에 본다 — 앞 줄의 cutin_open이 아직 모션 중이어도 열림이다.
    private bool HasCutInToClose()
    {
        if (_slot.IsOpen)
            return true;

        Debug.LogWarning($"[CutInCloseCommand] {_spec.stage}에 열린 컷인이 없다 — 건너뛴다.");
        return false;
    }
}
