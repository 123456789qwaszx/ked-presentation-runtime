using System.Collections.Generic;

/// <summary>
/// 하루 단위 상태. 예약 3건과 그 결산을 보관한다.
/// </summary>
public sealed class DayCycleState
{
    private readonly List<ServiceBookingState> _bookings = new();
    private readonly List<ServiceSettlementResult> _settlements = new();

    public int DayNumber { get; }
    public DayPhaseKind Phase { get; private set; } = DayPhaseKind.None;

    /// <summary>현재 처리 중인 예약 슬롯. 0부터 시작한다.</summary>
    public int SlotCursor { get; private set; }

    public int EnergyEarned { get; private set; }

    public IReadOnlyList<ServiceBookingState> Bookings => _bookings;
    public IReadOnlyList<ServiceSettlementResult> Settlements => _settlements;

    public DayCycleState(int dayNumber)
    {
        DayNumber = dayNumber;
    }

    public bool HasRemainingSlot => SlotCursor < _bookings.Count;

    public ServiceBookingState CurrentBooking
        => HasRemainingSlot ? _bookings[SlotCursor] : null;

    public void SetPhase(DayPhaseKind phase)
    {
        Phase = phase;
    }

    public void PostBookings(IReadOnlyList<MonsterProfile> monsters)
    {
        _bookings.Clear();
        SlotCursor = 0;

        for (int i = 0; i < monsters.Count; i++)
            _bookings.Add(new ServiceBookingState(monsters[i], i));
    }

    public void CommitSettlement(ServiceSettlementResult result)
    {
        _settlements.Add(result);
        EnergyEarned += result.Energy;
    }

    public void AdvanceSlot()
    {
        SlotCursor++;
    }

    public int CountFailedBookings()
    {
        int count = 0;

        for (int i = 0; i < _settlements.Count; i++)
        {
            if (!_settlements[i].IsSatisfactionMet)
                count++;
        }

        return count;
    }

    public int CountIncidents()
    {
        int count = 0;

        for (int i = 0; i < _settlements.Count; i++)
        {
            if (_settlements[i].IsIncident)
                count++;
        }

        return count;
    }
}
