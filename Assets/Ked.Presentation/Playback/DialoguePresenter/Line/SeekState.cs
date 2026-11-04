using System;

public enum SeekKind
{
    None = 0,
    Rollback = 1,
    Load = 2,
}

/// <summary>시크가 끝난 사유. 끝내는 쪽은 사유만 보고하고, 정리는 SeekState.End가 한다.</summary>
public enum SeekEndReason
{
    None = 0,                 // 진행 중이거나 아직 끝난 적 없다
    Arrived = 1,              // 목표 줄에 도착했다
    // ⛔ 2 = ReplayChoiceMismatch(리플레이할 선택지를 기록에서 찾지 못했다)는 대사 안 선택지와 함께 2026-11-05에 걷혔다 — 번호는 비워 둔다.
    SceneReset = 3,           // 새 장면이 열렸다 — 앞 단위의 시크는 이 단위의 것이 아니다
    Abandoned = 4,            // 호스트의 기록이 바닥났는데 목표에 닿지 못했다
}

/// <summary>
/// 시크가 찾아갈 줄. 줄 ID와 장면 내 등장 순번으로 정해진다.
/// </summary>
public readonly struct SeekTarget
{
    public readonly SeekKind Kind;
    public readonly string LineId;

    // 장면 내 등장 순번(1부터). RollbackPoint.occurrence와 같은 좌표계.
    public readonly int Occurrence;

    public SeekTarget(SeekKind kind, string lineId, int occurrence)
    {
        Kind = kind;
        LineId = lineId;
        Occurrence = occurrence < 1 ? 1 : occurrence;
    }
}

/// <summary>
/// 시크 하나의 수명과 목표 판정. 시작은 Begin, 끝은 End(사유) 하나뿐이다.
/// - 목표와 같은 줄 ID가 지나갈 때마다 세고, Occurrence번째 일치가 목표다.
///   순번을 만드는 RollbackHistory·BacklogForkPoints도 줄 ID 하나로 센다 — 세 곳의 규칙이 같아야 한다.
/// 전제: 리플레이는 장면 시작에서 같은 경로를 탄다. 줄 ID는 프로젝트에서 고유하다.
///       시크 중 줄마다 ObserveLine을 정확히 한 번 부른다.
/// </summary>
public sealed class SeekState : ISeekStateQuery
{
    // 시크가 살아 있는 동안만 의미가 있다. 시작·종료 때 통째로 비운다.
    private struct InFlight
    {
        public int MatchedCount;
        public bool LastObservedIsTarget;
    }

    private SeekTarget _target;
    private InFlight _inFlight;

    public SeekKind Kind => _target.Kind;
    public bool IsSeeking => Kind != SeekKind.None;
    bool ISeekStateQuery.IsSeekingActive => IsSeeking;

    // 마지막으로 관측한 줄이 목표였는가.
    public bool LastObservedIsTarget => _inFlight.LastObservedIsTarget;

    // 지금 시크가 끝난 사유. 진행 중이면 None.
    public SeekEndReason LastEndReason { get; private set; }

    public void Begin(SeekTarget target)
    {
        if (target.Kind == SeekKind.None)
            throw new ArgumentException("[시크] None으로 시크를 시작할 수 없다.", nameof(target));

        _target = target;
        _inFlight = default;
        LastEndReason = SeekEndReason.None;
    }

    // 시크 중 지나가는 줄을 하나 관측한다. 목표와 같은 줄이면 일치 횟수를 센다.
    public void ObserveLine(string lineId)
    {
        _inFlight.LastObservedIsTarget = false;

        if (!IsSeeking)
            return;

        if (string.IsNullOrWhiteSpace(_target.LineId))
            return;

        if (!string.Equals(lineId, _target.LineId, StringComparison.Ordinal))
            return;

        _inFlight.MatchedCount++;
        _inFlight.LastObservedIsTarget = _inFlight.MatchedCount == _target.Occurrence;
    }

    // 시크를 끝낸다. 시크 중이 아니면 무시한다.
    public void End(SeekEndReason reason)
    {
        if (reason == SeekEndReason.None)
            throw new ArgumentException("[시크] 종료 사유가 없다.", nameof(reason));

        if (!IsSeeking)
            return;

        _target = default;
        _inFlight = default;
        LastEndReason = reason;
    }
}
