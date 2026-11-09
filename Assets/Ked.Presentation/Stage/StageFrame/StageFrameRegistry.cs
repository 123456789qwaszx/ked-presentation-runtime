using System;
using System.Collections.Generic;

/// <summary>
/// 무대 키 → 컷인 슬롯 · 그림 팝업 슬롯. 조립이 무대 리그를 세운 뒤 무대마다 틀을 세워 등록하고, 컷인 · 팝업 커맨드와
/// 틀을 중립으로 돌리는 쪽(tx · 마스크 지우기 · 장면 경계)이 여기서 꺼낸다. 없는 무대를 꺼내면 예외다.
/// </summary>
public sealed class StageFrameRegistry
{
    private readonly StageFrameBuilder _builder;
    private readonly Dictionary<string, StageCutInSlot> _cutIns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StagePopupSlot> _popups = new(StringComparer.Ordinal);

    public StageFrameRegistry(StageFrameBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>무대 리그 하나에 틀을 세우고 등록한다. 같은 키가 이미 있으면 예외 — 틀은 한 번만 선다.</summary>
    public void Build(StageRigRefs stage)
    {
        if (_cutIns.ContainsKey(stage.StageKey))
            throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stage.StageKey}'의 틀은 이미 서 있다.");

        _builder.Build(stage, out StageCutInSlot cutIn, out StagePopupSlot popup);

        _cutIns[stage.StageKey] = cutIn;
        _popups[stage.StageKey] = popup;
    }

    public StageCutInSlot CutIn(string stageKey) => Find(_cutIns, stageKey);

    public StagePopupSlot Popup(string stageKey) => Find(_popups, stageKey);

    /// <summary>그 무대의 틀을 중립으로 — 컷인 창 · 판과 그림 팝업을 닫는다. 도는 모션 · 페이드는 끝값 없이 끊는다.</summary>
    public void Reset(string stageKey)
    {
        CutIn(stageKey).Reset();
        Popup(stageKey).Reset();
    }

    /// <summary>장면 경계 — 모든 무대의 틀을 중립으로.</summary>
    public void ResetAll()
    {
        foreach (StageCutInSlot cutIn in _cutIns.Values)
            cutIn.Reset();

        foreach (StagePopupSlot popup in _popups.Values)
            popup.Reset();
    }

    private static T Find<T>(Dictionary<string, T> slots, string stageKey)
        => slots.TryGetValue(stageKey, out T slot)
            ? slot
            : throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stageKey}'의 틀이 없다. 조립이 세운 틀: {string.Join(", ", slots.Keys)}");
}
