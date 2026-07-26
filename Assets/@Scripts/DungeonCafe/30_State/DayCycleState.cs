using System.Collections.Generic;

/// <summary>
/// 하루 단위 상태. 예약 3건과 그 결산을 보관한다.
///
/// 예약은 생성 시점에 확정된다. '예약 없는 하루'는 존재하지 않는다.
/// 슬롯 진행도 이 객체가 소유한다. 커서를 밖에서 밀 수 없고, 전진 경로는 CompleteSlot 하나뿐이다.
/// </summary>
public sealed class DayCycleState
{
    private readonly List<ServiceBookingState> _bookings;
    private readonly List<ServiceSettlementResult> _settlements = new();

    private int _slotCursor;

    public int DayNumber { get; }

    /// <summary>처리를 마친 슬롯 수. 진행 눈금에 쓴다.</summary>
    public int ResolvedSlotCount => _slotCursor;

    public DayPhaseKind Phase { get; private set; } = DayPhaseKind.None;

    public int EnergyEarned { get; private set; }

    public IReadOnlyList<ServiceBookingState> Bookings => _bookings;
    public IReadOnlyList<ServiceSettlementResult> Settlements => _settlements;

    public DayCycleState(int dayNumber, IReadOnlyList<ServiceBookingState> bookings)
    {
        DayNumber = dayNumber;
        _bookings = new List<ServiceBookingState>(bookings);
    }

    public void SetPhase(DayPhaseKind phase)
    {
        Phase = phase;
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

    /// <summary>접객을 결산까지 마쳤다. 결과 기록과 슬롯 전진이 함께 일어난다.</summary>
    public void CompleteSlot(ServiceSettlementResult result)
    {
        _bookings[_slotCursor].MarkServed(result);
        _settlements.Add(result);

        EnergyEarned += result.Energy;
        _slotCursor++;
    }

    /// <summary>요구 만족도를 채우지 못한 예약 수.</summary>
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