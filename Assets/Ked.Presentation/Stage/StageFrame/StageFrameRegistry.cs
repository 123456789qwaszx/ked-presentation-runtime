using System;
using System.Collections.Generic;

/// <summary>
/// 무대 키 → 그 무대의 틀(컷인 창). 무대 리그가 선 무대마다 하나씩, 만들 때 한 번에 세운다.
/// </summary>
public sealed class StageFrameRegistry
{
    private readonly Dictionary<string, StageCutInSlot> _frames = new(StringComparer.Ordinal);

    public StageFrameRegistry(StageRigRegistry stages)
    {
        foreach (StageRigRefs stage in stages.All)
            _frames[stage.StageKey] = StageFrameBuilder.Build(stage);
    }

    public StageCutInSlot CutIn(string stageKey)
        => _frames.TryGetValue(stageKey, out StageCutInSlot slot)
            ? slot
            : throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stageKey}'의 틀이 없다.");

    /// <summary>장면 경계 — 모든 무대의 틀을 중립으로.</summary>
    public void ResetAll()
    {
        foreach (StageCutInSlot slot in _frames.Values)
            slot.Reset();
    }
}
