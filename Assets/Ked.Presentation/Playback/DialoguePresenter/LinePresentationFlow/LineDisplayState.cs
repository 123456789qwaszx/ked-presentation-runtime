// 지금 줄의 표시 상태 — 다 보였나 · #auto 넘김 대기. 시크는 SeekState가 가진다.
public sealed class LineDisplayState
{
    public bool IsLineFullyShown { get; private set; } = true;
    
    // 지금 줄이 #auto이고 아직 넘기지 않았다(LineAdvanceTag). 다 보이면 재생 입력의 프레임이 넘기고 내린다.
    // 줄에 들어갈 때 · 무너질 때 내린다 — 다음 줄로 새지 않는다.
    public bool IsLineAutoAdvancePending { get; private set; }

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