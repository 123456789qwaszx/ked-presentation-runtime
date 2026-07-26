// Settlement가 비어 있으면 아직 처리되지 않은 예약이다.
public sealed class ServiceBookingState
{
    public MonsterProfile Monster { get; }

    public bool IsConfirmed { get; private set; }

    public ServiceSettlementResult Settlement { get; private set; }

    public ServiceBookingState(MonsterProfile monster)
    {
        Monster = monster;
    }

    public bool IsSuccessful => Settlement != null && Settlement.IsSatisfactionMet;

    public void ConfirmByPhone()
    {
        IsConfirmed = true;
    }

    public void MarkServed(ServiceSettlementResult result) => Settlement = result;
}