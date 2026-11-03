using System;

public readonly struct LineRecordDecision
{
    public readonly bool RecordBacklog;
    public readonly bool AddRollbackPoint;

    public LineRecordDecision(bool recordBacklog, bool addRollbackPoint)
    {
        RecordBacklog = recordBacklog;
        AddRollbackPoint = addRollbackPoint;
    }
}

// 줄 진입 시 기록 규칙.
// - 백로그: 들어온 줄당 1회(stale 포함). 롤백 리플레이는 이미 기록된 줄이라 생략.
// - 롤백 포인트: 항상 기록(장면 시작에 비워짐).
// 전제: 롤백은 백로그에서 목표 줄 뒤만 자른다(PlaybackFeatureController.RequestRollbackTo). 시크 종류는 시크 종료(SeekState.End) 전에 읽는다.
public static class LineRecordPolicy
{
    public static LineRecordDecision Resolve(SeekKind entrySeekKind) => entrySeekKind switch
    {
        SeekKind.None => new LineRecordDecision(
            recordBacklog: true,
            addRollbackPoint: true),

        SeekKind.Load => new LineRecordDecision(
            recordBacklog: true,
            addRollbackPoint: true),

        SeekKind.Rollback => new LineRecordDecision(
            recordBacklog: false,
            addRollbackPoint: true),

        _ => throw new ArgumentOutOfRangeException(
            nameof(entrySeekKind),
            entrySeekKind,
            "[기록 정책] 이 시크 종류의 기록 규칙이 정해지지 않았다."),
    };
}