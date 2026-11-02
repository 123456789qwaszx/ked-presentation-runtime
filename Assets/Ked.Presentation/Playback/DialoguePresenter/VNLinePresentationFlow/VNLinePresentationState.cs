public sealed class VNLinePresentationState : ISeekStateQuery
{
    private readonly VNSeekState _seekState = new ();
    public VNSeekKind SeekKind => _seekState.Kind;
    public bool IsSeekingActive => _seekState.IsSeeking;
    
    public bool IsLineFullyShown { get; private set; } = true;
    
    // 지금 줄이 #auto이고 아직 넘기지 않았다(VNLineAdvanceTag). 다 보이면 재생 입력의 프레임이 넘기고 내린다.
    // 줄에 들어갈 때 · 무너질 때 내린다 — 다음 줄로 새지 않는다.
    public bool IsLineAutoAdvancePending { get; private set; }

    public void BeginRollbackSeek(string lineId, int occurrence)
        => BeginSeek(VNSeekKind.Rollback, lineId, occurrence);
    
    // 로드 - 장면 루트에서 표적까지. 롤백과 달리 백로그를 다시 적는다(VNLineRecordPolicy).
    public void BeginLoadSeek(string lineId, int occurrence)
        => BeginSeek(VNSeekKind.Load, lineId, occurrence);

    // 줄 표시 상태는 건드리지 않는다. 시크 중 넘김은 AdvanceGate가 막고(seek_active),
    // 리플레이 첫 줄의 MarkLineEntered가 다시 내린다.
    private void BeginSeek(VNSeekKind kind, string lineId, int occurrence)
        => _seekState.Begin(new VNSeekTarget(kind, lineId, occurrence));
    
    // 시크 중 지나가는 줄을 하나 관측한다. 줄마다 정확히 한 번 부른다(VNSeekState 참고).
    public void ObserveSeekLine(YarnLineMeta meta) => _seekState.ObserveLine(meta);

    // 마지막으로 관측한 줄이 시크 목표였는가.
    public bool IsAtSeekTarget => _seekState.LastObservedIsTarget;

    // 시크를 끝낸다 — 사유만 보고한다(VNSeekState.End). 시크 중이 아니면 무시된다.
    public void EndSeek(VNSeekEndReason reason) => _seekState.End(reason);
    
    public void MarkLineEntered()
    {
        IsLineFullyShown = false;
        IsLineAutoAdvancePending = false;
    }
    
    // 줄의 넘김 태그를 정한 뒤(시크 판정 다음) 한 번. 시크로 지나가는 줄에는 부르지 않는다.
    public void SetLineAutoAdvance(bool autoAdvance)
    {
        IsLineAutoAdvancePending = autoAdvance;
    }

    // 넘김 요청이 받아들여졌다 — 같은 줄에서 두 번 넘기지 않는다.
    public void ConsumeLineAutoAdvance()
    {
        IsLineAutoAdvancePending = false;
    }

    // 라인이 안정된 경계까지 갔다(= Ready).
    public void MarkLineDisplayCompleted(string reason)
    {
        IsLineFullyShown = true;
    }

    // 라인이 정상 경계 전에 무너짐.(= Released). 롤백 · 정지 · 선점으로 잘린 경우.
    public void MarkLineTornDown(string reason)
    {
        IsLineFullyShown = false;
        IsLineAutoAdvancePending = false;
    }
}