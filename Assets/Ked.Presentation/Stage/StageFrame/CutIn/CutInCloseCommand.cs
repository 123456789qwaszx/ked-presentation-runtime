using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>컷인을 닫는 모양 — cutin_close의 끝 인자(docs/PLAN_cutin.md §5.1). 끝 상태는 같다.</summary>
public enum CutInCloseMotion
{
    /// <summary>cutin_pop_out — 살짝 커졌다가 튀어 닫힌다(창의 배율 · 회전 클립). 기본값.</summary>
    PopOut = 0,

    /// <summary>cutin_fade_out — 판과 그 무대의 내용이 함께 투명해진다. 창은 움직이지 않는다.</summary>
    FadeOut = 1,
}

[Serializable]
public sealed class CutInCloseCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";

    [Header("Motion")]
    [Tooltip("닫는 모양. 길이 · 키는 프리셋(CutInPresetDBSO)에 있다.")]
    public CutInCloseMotion motion = CutInCloseMotion.PopOut;
}

// cutin_close — 무대 슬롯에 퇴장 모션을 걸기만 한다. 끝에서 슬롯이 닫힘(창 항등 · 마스크 숨김)으로 둔다(StageCutInSlot.PlayClose).
public sealed class CutInCloseCommand : CommandBase
{
    private readonly CutInCloseCommandSpec _spec;
    private readonly CutInPresetDBSO _preset;
    private readonly StageFrameRegistry _frames;

    private StageCutInSlot _slot;
    private MotionClip _clip;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public CutInCloseCommand(
        CutInCloseCommandSpec spec,
        CutInPresetDBSO preset,
        StageFrameRegistry frames)
    {
        _spec = spec;
        _preset = preset;
        _frames = frames;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        if (!HasCutInToClose())
            yield break;

        if (_spec.motion == CutInCloseMotion.FadeOut)
            _slot.PlayFadeClose(_preset.fadeOutSeconds);
        else
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

        _slot = _frames.CutIn(_spec.stage);

        // 흐려져 닫힘은 창 클립을 쓰지 않는다.
        if (_spec.motion == CutInCloseMotion.FadeOut)
            return;

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
