using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>컷인을 여는 모양 — cutin_open의 끝 인자(docs/PLAN_cutin.md §5.1).</summary>
public enum CutInOpenMotion
{
    /// <summary>cutin_pop_in — 작은 점에서 튀어나온다(창의 배율 · 회전 클립). 기본값.</summary>
    PopIn = 0,

    /// <summary>cutin_flash_in — 흰 선이 긋고 흰 판이 된 뒤 흰빛이 걷히며 드러난다. 창은 움직이지 않는다.</summary>
    FlashIn = 1,
}

[Serializable]
public sealed class CutInOpenCommandSpec : CommandSpecBase
{
    [Header("Target")]
    [Tooltip("덮는 무대(stage01 · stage02).")]
    public string stage = "stage01";

    [Header("Panel")]
    [Tooltip("판 중심. 무대 중심 기준 px, y 위. 판 그림은 파서가 같은 자리 · 배율로 세운 리그다.")]
    public Vector2 center;

    [Tooltip("판 기본 크기(프리셋)에 곱한다.")]
    public float scale = 1f;

    [Header("Motion")]
    [Tooltip("여는 모양. 길이 · 키는 프리셋(CutInPresetDBSO)에 있다.")]
    public CutInOpenMotion motion = CutInOpenMotion.PopIn;
}

// cutin_open의 창 — 창 피벗 · 마스크를 세우고 등장 모션을 걸기만 한다. 모션은 슬롯이 소유한다(StageCutInSlot.PlayOpen).
// 판 그림은 이 커맨드가 아니라 파서가 바로 앞에 낸 판 리그 커맨드들이 세운다(docs/PLAN_stage-visibility.md §3.2).
public sealed class CutInOpenCommand : CommandBase
{
    private readonly CutInOpenCommandSpec _spec;
    private readonly CutInPresetDBSO _preset;
    private readonly StageFrameRegistry _frames;

    private StageCutInSlot _slot;
    private MotionClip _clip;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public CutInOpenCommand(
        CutInOpenCommandSpec spec,
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

        if (_spec.motion == CutInOpenMotion.FlashIn)
            _slot.PlayFlashOpen(_preset, _spec.center, _spec.scale);
        else
            _slot.PlayOpen(_preset, _spec.center, _spec.scale, _clip);

        if (!_spec.wait)
            yield break;

        while (_slot.IsPlayingMotion)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.SnapOpen(_preset, _spec.center, _spec.scale, _clip);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _slot = _frames.CutIn(_spec.stage);

        // 흰빛 열림은 창 클립을 쓰지 않는다 — 끝 상태도 창 1 · 0이다(SnapOpen에 클립 없이).
        if (_spec.motion == CutInOpenMotion.FlashIn)
            return;

        _clip = _preset.PopIn;

        if (!MotionRules.TryValidate(_clip, null, out string why))
        {
            Debug.LogWarning($"[CutInOpenCommand] 등장 클립이 유효하지 않다 ({_preset.name}): {why}. 열린 자리만 세운다.");
            _clip = null;
        }
    }
}
