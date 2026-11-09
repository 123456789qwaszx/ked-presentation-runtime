using System.Collections;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public sealed class StageMaskMotionCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";

    [Header("Preset")]
    [Tooltip("StageMaskMotionPresetDBSO entry key. ex) hstrip_open, hstrip_in, iris_in")]
    public string presetKey = StageMaskMotionPresetDBSO.DefaultPresetKey;

    [Header("Tween Override")]
    [Tooltip("음수이면 preset의 duration을 사용합니다. 0이면 즉시 커밋.")]
    public float durationOverride = -1f;
}

// tx — 프리셋을 골라 무대 슬롯에 모션을 걸기만 한다. 모션은 슬롯이 소유한다(StageMaskSlot.PlayMotion).
public sealed class StageMaskMotionCommand : CommandBase
{
    private readonly StageMaskMotionCommandSpec _spec;
    private readonly StageMaskMotionPresetDBSO _presetDb;
    private readonly StageRigRegistry _stages;

    private StageMaskMotionPresetDBSO.Entry _entry;
    private float _duration;
    private StageMaskSlot _slot;

    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;

    public StageMaskMotionCommand(
        StageMaskMotionCommandSpec spec,
        StageMaskMotionPresetDBSO presetDb,
        StageRigRegistry stages)
    {
        _spec = spec;
        _presetDb = presetDb;
        _stages = stages;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.PlayMotion(_entry, _duration);

        if (!_spec.wait)
            yield break;

        while (_slot.IsPlayingMotion)
            yield return null;
    }

    protected override void OnSkip()
    {
        if (!_resolveAttempted)
            ResolveRefs();

        _slot.SnapMotionEnd(_entry);
    }

    private void ResolveRefs()
    {
        _resolveAttempted = true;

        _entry = ResolvePresetEntry();
        _duration = _spec.durationOverride >= 0f
            ? _spec.durationOverride
            : _entry.duration;

        StageRigRefs stage = _stages.Get(_spec.stage);
        _slot = stage.Mask;

        // tx는 무대를 어디까지 보여 줄지를 새로 정한다 — 열려 있던(또는 닫힌 채 남은) 컷인 창 · 판은 중립으로.
        // 마스크 모양은 tx의 것이 이긴다(docs/PLAN_cutin.md §5).
        if (stage.CutIn.IsOpen || stage.CutIn.Window.localScale != Vector3.one)
            Debug.LogWarning($"[StageMaskMotionCommand] {_spec.stage}의 컷인을 tx '{_spec.presetKey}'가 덮는다 — 컷인 창을 중립으로 돌린다.");

        stage.CutIn.Reset();

        // 그림 팝업도 같은 규칙 — 그 무대를 새로 보여 주는 전환이 덮는다(docs/PLAN_popup.md §6).
        if (stage.Popup.IsOpen)
            Debug.LogWarning($"[StageMaskMotionCommand] {_spec.stage}의 그림 팝업을 tx '{_spec.presetKey}'가 덮는다 — 팝업을 닫는다.");

        stage.Popup.Reset();
    }

    private StageMaskMotionPresetDBSO.Entry ResolvePresetEntry()
    {
        if (_presetDb.TryGet(_spec.presetKey, out StageMaskMotionPresetDBSO.Entry entry))
        {
            return entry;
        }

        Debug.LogWarning(
            $"[StageMaskMotionCommand] Stage mask preset not found. " +
            $"presetKey='{_spec.presetKey}'. Using fallback.");

        if (_presetDb.TryGet(StageMaskMotionPresetDBSO.DefaultPresetKey, out entry))
        {
            return entry;
        }

        return BuildHardcodedFallbackEntry();
    }

    private static StageMaskMotionPresetDBSO.Entry BuildHardcodedFallbackEntry()
    {
        return new StageMaskMotionPresetDBSO.Entry
        {
            key = "fallback",
            kind = StageMaskKind.HorizontalStrip,
            fromOffset = Vector2.zero,
            toOffset = Vector2.zero,
            animateStripHeight = true,
            fromStripHeightPixels = 0f,
            stripHeightPixels = 360f,
            horizontalBleedPixels = 96f,
            showEdge = true,
            edgeMode = StageMaskEdgeMode.Both,
            edgeColor = new Color(1f, 1f, 1f, 0.82f),
            edgeThickness = 4f,
            hideEdgeOnComplete = false,
            duration = 0.45f,
            ease = Ease.OutCubic,
            rubberMode = StageMaskRubberMode.None,
            irisAspect = 1f,
            irisSegments = 64,
        };
    }
}
