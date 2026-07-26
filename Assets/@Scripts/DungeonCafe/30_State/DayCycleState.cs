using System.Collections.Generic;

/// <summary>
/// 하루 단위 상태. 예약 3건과 그 결산을 보관한다.
///
/// 슬롯 진행은 이 객체가 소유한다. 커서를 밖에서 밀 수 없고,
/// 전진 경로는 CompleteSlot 과 SkipSlot 둘뿐이며 둘 다 결과를 남긴다.
/// 따라서 '기록 없이 넘어간 슬롯'이 존재할 수 없다.
/// </summary>
public sealed class DayCycleState
{
    private readonly List<ServiceBookingState> _bookings = new();
    private readonly List<ServiceSettlementResult> _settlements = new();

    private int _slotCursor;

    public int DayNumber { get; }
    
    public int ResolvedSlotCount => _slotCursor;
    public DayPhaseKind Phase { get; private set; } = DayPhaseKind.None;

    public int EnergyEarned { get; private set; }

    public IReadOnlyList<ServiceBookingState> Bookings => _bookings;
    public IReadOnlyList<ServiceSettlementResult> Settlements => _settlements;

    public DayCycleState(int dayNumber)
    {
        DayNumber = dayNumber;
    }

    public void SetPhase(DayPhaseKind phase)
    {
        Phase = phase;
    }

    /// <summary>오늘 게시판에 문의를 올린다. 하루에 한 번만 부른다.</summary>
    public void PostBookings(IReadOnlyList<MonsterProfile> monsters)
    {
        _bookings.Clear();
        _settlements.Clear();

        _slotCursor = 0;
        EnergyEarned = 0;

        for (int i = 0; i < monsters.Count; i++)
            _bookings.Add(new ServiceBookingState(monsters[i], i));
    }

    /// <summary>아직 처리하지 않은 예약을 꺼낸다. 상태는 바꾸지 않는다.</summary>
    public bool TryGetPendingSlot(out ServiceBookingState booking)
    {
        booking = _slotCursor < _bookings.Count ? _bookings[_slotCursor] : null;
        return booking != null;
    }

    /// <summary>담당을 확정한다. 예약과 메이드가 함께 바뀐다.</summary>
    public void AssignMaid(ServiceBookingState booking, MaidRuntimeState maid)
    {
        booking.AssignMaid(maid.MaidId);
        maid.MarkAssigned(DayNumber);
    }

    /// <summary>접객을 결산까지 마쳤다.</summary>
    public void CompleteSlot(ServiceSettlementResult result)
    {
        _bookings[_slotCursor].MarkServed(result);
        _settlements.Add(result);

        EnergyEarned += result.Energy;
        _slotCursor++;
    }

    /// <summary>성사되지 않은 예약 수. 접객조차 못 한 건도 포함한다.</summary>
    public int CountFailedBookings()
    {
        int count = 0;

        for (int i = 0; i < _bookings.Count; i++)
        {
            if (!_bookings[i].IsSuccessful)
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