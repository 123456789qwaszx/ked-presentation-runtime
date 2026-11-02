using System;

public enum VNSeekKind
{
    None = 0,
    Rollback = 1,
    Load = 2,
}

/// <summary>
/// 시크가 찾아갈 줄. 줄 ID와 장면 내 등장 순번으로 정해진다.
/// </summary>
public readonly struct VNSeekTarget
{
    public readonly VNSeekKind Kind;
    public readonly string LineId;

    // 장면 내 등장 순번(1부터). RollbackPoint.occurrence와 같은 좌표계.
    public readonly int Occurrence;

    public VNSeekTarget(VNSeekKind kind, string lineId, int occurrence)
    {
        Kind = kind;
        LineId = lineId;
        Occurrence = occurrence < 1 ? 1 : occurrence;
    }
}

/// <summary>
/// 시크 하나의 수명과 목표 판정.
/// - 목표와 같은 줄 ID가 지나갈 때마다 세고, Occurrence번째 일치가 목표다.
/// 전제: 리플레이는 장면 시작에서 같은 경로를 탄다. 줄 ID는 프로젝트에서 고유하다.
///       시크 중 줄마다 ObserveLine을 정확히 한 번 부른다.
/// </summary>
public sealed class VNSeekState
{
    // 시크가 살아 있는 동안만 의미가 있다. 시작·종료 때 통째로 비운다.
    private struct InFlight
    {
        public int MatchedCount;
        public bool LastObservedIsTarget;
    }

    private VNSeekTarget _target;
    private InFlight _inFlight;

    public VNSeekKind Kind => _target.Kind;
    public bool IsSeeking => Kind != VNSeekKind.None;

    // 마지막으로 관측한 줄이 목표였는가.
    public bool LastObservedIsTarget => _inFlight.LastObservedIsTarget;

    public void Begin(VNSeekTarget target)
    {
        if (target.Kind == VNSeekKind.None)
            throw new ArgumentException("[시크] None으로 시크를 시작할 수 없다.", nameof(target));

        _target = target;
        _inFlight = default;
    }

    // 시크 중 지나가는 줄을 하나 관측한다. 목표와 같은 줄이면 일치 횟수를 센다.
    public void ObserveLine(YarnLineMeta meta)
    {
        _inFlight.LastObservedIsTarget = false;

        if (!IsSeeking)
            return;

        if (string.IsNullOrWhiteSpace(_target.LineId))
            return;

        if (!string.Equals(meta.lineId, _target.LineId, StringComparison.Ordinal))
            return;

        _inFlight.MatchedCount++;
        _inFlight.LastObservedIsTarget = _inFlight.MatchedCount == _target.Occurrence;
    }

    public void Clear()
    {
        _target = default;
        _inFlight = default;
    }
}
