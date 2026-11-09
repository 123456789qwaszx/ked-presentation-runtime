using System;
using System.Collections.Generic;

/// <summary>
/// 무대 키 → StageFrameRefs. 조립이 무대 리그를 세운 뒤 무대마다 틀을 세워 등록하고, 컷인 · 팝업 커맨드와
/// 틀을 중립으로 돌리는 쪽(tx · 마스크 지우기 · 장면 경계)이 여기서 꺼낸다. 없는 무대를 꺼내면 예외다.
/// </summary>
public sealed class StageFrameRegistry
{
    private readonly StageFrameBuilder _builder;
    private readonly Dictionary<string, StageFrameRefs> _frames = new(StringComparer.Ordinal);
    private readonly List<StageFrameRefs> _ordered = new();

    public StageFrameRegistry(StageFrameBuilder builder)
    {
        _builder = builder;
    }

    public IReadOnlyList<StageFrameRefs> All => _ordered;

    /// <summary>무대 리그 하나에 틀을 세우고 등록한다. 같은 키가 이미 있으면 예외 — 틀은 한 번만 선다.</summary>
    public StageFrameRefs Build(StageRigRefs stage)
    {
        if (_frames.ContainsKey(stage.StageKey))
            throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stage.StageKey}'의 틀은 이미 서 있다.");

        StageFrameRefs frame = _builder.Build(stage);

        _frames[stage.StageKey] = frame;
        _ordered.Add(frame);

        return frame;
    }

    public StageFrameRefs Get(string stageKey)
        => _frames.TryGetValue(stageKey, out StageFrameRefs frame)
            ? frame
            : throw new InvalidOperationException($"[StageFrameRegistry] 무대 '{stageKey}'의 틀이 없다. 조립이 세운 틀: {string.Join(", ", _frames.Keys)}");

    /// <summary>장면 경계 — 모든 무대의 틀을 중립으로.</summary>
    public void ResetAll()
    {
        for (int i = 0; i < _ordered.Count; i++)
            _ordered[i].Reset();
    }
}
