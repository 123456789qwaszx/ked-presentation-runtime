using System;
using System.Collections.Generic;

/// <summary>
/// 무대 키 → 그 무대의 틀(컷인 슬롯 · 그림 팝업 슬롯). 무대 리그가 선 무대마다 하나씩, 만들 때 한 번에 세운다.
/// </summary>
public sealed class StageFrameRegistry
{
    private readonly Dictionary<string, (StageCutInSlot CutIn, StagePopupSlot Popup)> _frames = new(StringComparer.Ordinal);

    public StageFrameRegistry(StageRigRegistry stages)
    {
        foreach (StageRigRefs stage in stages.All)
            _frames[stage.StageKey] = StageFrameBuilder.Build(stage);
    }

    public StageCutInSlot CutIn(string stageKey) => Get(stageKey).CutIn;

    public StagePopupSlot Popup(string stageKey) => Get(stageKey).Popup;

    /// <summary>그 무대의 틀을 중립으로. 도는 모션 · 페이드는 끝값 없이 끊는다.</summary>
    public void Reset(string stageKey) => Reset(Get(stageKey));

    /// <summary>장면 경계 — 모든 무대의 틀을 중립으로.</summary>
    public void ResetAll()
    {
        foreach ((StageCutInSlot CutIn, StagePopupSlot Popup) frame in _frames.Values)
            Reset(frame);
    }

    private static void Reset((StageCutInSlot CutIn, StagePopupSlot Popup) frame)
    {
        frame.CutIn.Reset();
        frame.Popup.Reset();
    }

    private (StageCutInSlot CutIn, StagePopupSlot Popup) Get(string stageKey)
        => _frames.TryGetValue(stageKey, out var frame)
            ? frame
            : throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stageKey}'의 틀이 없다.");
}