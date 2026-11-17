using System;
using System.Collections;
using Ked.Presentation.Core;
using UnityEngine;

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

        _clip = _preset.PopIn;

        if (!MotionRules.TryValidate(_clip, null, out string why))
        {
            Debug.LogWarning($"[CutInOpenCommand] 등장 클립이 유효하지 않다 ({_preset.name}): {why}. 열린 자리만 세운다.");
            _clip = null;
        }
    }
}
