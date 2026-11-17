using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class StageFrameResetCommandSpec : CommandSpecBase
{
    [Header("Target")]
    public string stage = "stage01";

    [Tooltip("true면 모든 무대의 틀. stage는 무시.")]
    public bool allStages;

    [Tooltip("열려 있던 컷인을 닫을 때 경고에 적는 까닭(예: tx 'daze_close'). 비면 조용히 닫는다.")]
    public string cause;
}

// 틀 되돌리기 — 그 무대(또는 전부)의 컷인 창을 중립으로(stage-frame.md §4). 즉시라 압축과 상관없다.
// 대본 어휘가 아니다. 무대를 어디까지 보여 줄지 새로 정하는 명령(tx · tx_stage_mask_clear)을 파서가 이 커맨드와 마스크 커맨드 둘로 낸다 —
// 틀이 마스크의 결정을 따르는 규칙을 마스크 코드 안에 숨기지 않고 문법에 드러낸다(docs/PLAN_stage-code-homes.md §3).
public sealed class StageFrameResetCommand : CommandBase
{
    private readonly StageFrameResetCommandSpec _spec;
    private readonly StageFrameRegistry _frames;

    // 실험 A(docs/PLAN_stage-code-homes.md §8) — 열린 컷인은 닫지 않고 경고만, 닫힌 컷인은 중립으로.
    // Unity에서 지금 규칙과 견주어 보려고 둔다. 소유자가 정하면 지운다.
    private readonly bool _keepOpenFrames;

    public override bool WaitForCompletion => false;

    public StageFrameResetCommand(StageFrameResetCommandSpec spec, StageFrameRegistry frames, bool keepOpenFrames = false)
    {
        _spec = spec;
        _frames = frames;
        _keepOpenFrames = keepOpenFrames;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip()
    {
        Apply();
    }

    private void Apply()
    {
        if (_keepOpenFrames)
        {
            ApplyKeepingOpenFrames();
            return;
        }

        if (_spec.allStages)
        {
            _frames.ResetAll();
            return;
        }

        WarnIfOpen();
        _frames.Reset(_spec.stage);
    }

    private void ApplyKeepingOpenFrames()
    {
        if (!_spec.allStages)
        {
            ResetClosedOnly(_spec.stage);
            return;
        }

        foreach (string stage in _frames.StageKeys)
            ResetClosedOnly(stage);
    }

    // 열리지 않은 컷인은 중립으로 둔다(닫힌 컷인의 창은 이미 항등이다). 열린(닫히는 중인) 컷인은 그대로 둔다.
    private void ResetClosedOnly(string stage)
    {
        string cause = string.IsNullOrEmpty(_spec.cause) ? "tx_stage_mask_clear" : _spec.cause;

        StageCutInSlot cutIn = _frames.CutIn(stage);

        if (cutIn.IsOpen)
            Debug.LogWarning($"[StageFrameResetCommand] {stage}의 열린 컷인 위에 {cause} — 컷인은 그대로 둔다(실험 A).");
        else
            cutIn.Reset();
    }

    // 열려 있던 컷인을 덮는 것은 대본의 실수일 수 있다 — 까닭을 적어 알린다(docs/PLAN_cutin.md §5).
    private void WarnIfOpen()
    {
        if (string.IsNullOrEmpty(_spec.cause))
            return;

        StageCutInSlot cutIn = _frames.CutIn(_spec.stage);

        if (cutIn.IsOpen || cutIn.Window.localScale != Vector3.one)
            Debug.LogWarning($"[StageFrameResetCommand] {_spec.stage}의 컷인을 {_spec.cause}가 덮는다 — 컷인 창을 중립으로 돌린다.");
    }
}
