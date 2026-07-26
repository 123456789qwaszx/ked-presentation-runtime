/// <summary>
/// 게시판에 올라온 예약 문의 한 건.
/// 게시판 단계에서는 종족과 겉모습만, 통화 확정 이후에 대응 타입이 공개된다.
///
/// 슬롯이 끝나면 반드시 Settlement 또는 SkipReason 중 하나가 채워진다.
/// 둘 다 비어 있으면 아직 처리되지 않은 예약이다.
/// </summary>
public sealed class ServiceBookingState
{
    public MonsterProfile Monster { get; }
    public int SlotIndex { get; }

    public bool IsConfirmed { get; private set; }

    public bool IsCodexRevealed { get; private set; }

    public string AssignedMaidId { get; private set; }

    public ServiceSettlementResult Settlement { get; private set; }

    public ServiceBookingState(MonsterProfile monster, int slotIndex)
    {
        Monster = monster;
        SlotIndex = slotIndex;
    }

    public bool IsSuccessful => Settlement != null && Settlement.IsSatisfactionMet;

    public void ConfirmByPhone()
    {
        IsConfirmed = true;
        IsCodexRevealed = true;
    }

    public void AssignMaid(string maidId)
    {
        AssignedMaidId = maidId;
    }

    public void MarkServed(ServiceSettlementResult result) => Settlement = result;
}