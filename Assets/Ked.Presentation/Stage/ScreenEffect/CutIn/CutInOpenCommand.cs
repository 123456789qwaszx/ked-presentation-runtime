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
    [Tooltip("판 그림 — Resources/CutInPanels/{키}")]
    public string panelKey;

    [Tooltip("판 중심. 무대 중심 기준 px, y 위.")]
    public Vector2 center;

    [Tooltip("판 기본 크기(프리셋)에 곱한다.")]
    public float scale = 1f;
}

// cutin_open — 프리셋과 판을 골라 무대 슬롯에 등장 모션을 걸기만 한다. 모션은 슬롯이 소유한다(StageCutInSlot.PlayOpen).
public sealed class CutInOpenCommand : CommandBase
{
    private readonly CutInOpenCommandSpec _spec;
    private readonly CutInPresetDBSO _preset;
    private readonly StageRigRegistry _stages;

    private StageCutInSlot _slot;
    private Sprite _panel;
    private MotionClip _clip;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public CutInOpenCommand(
        CutInOpenCommandSpec spec,
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

        _slot.PlayOpen(_preset, _panel, _spec.center, _spec.scale, _clip);

        if (!_spec.wait)
            yield break;

        while (_slot.IsPlayingMotion)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.SnapOpen(_preset, _panel, _spec.center, _spec.scale, _clip);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _slot = _stages.Get(_spec.stage).CutIn;

        // 못 찾으면 해석기가 경고한다 — 판 없이 열린다(마스크 · 모션은 그대로).
        _panel = ResourcesSpriteResolver.Resolve(CutInPresetDBSO.PanelResourcesRoot, _spec.panelKey);

        _clip = _preset.PopIn;

        if (!MotionRules.TryValidate(_clip, null, out string why))
        {
            Debug.LogWarning($"[CutInOpenCommand] 등장 클립이 유효하지 않다 ({_preset.name}): {why}. 열린 자리만 세운다.");
            _clip = null;
        }
    }
}
