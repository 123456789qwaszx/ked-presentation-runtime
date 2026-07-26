using System.Collections.Generic;

/// <summary>
/// 하루 단위 상태. 예약 3건과 그 결산을 보관한다.
///
/// 예약은 생성 시점에 확정된다. '예약 없는 하루'는 존재하지 않는다.
/// 접객 순서는 플레이어가 정하므로 슬롯은 순차가 아니라 지목으로 소비된다.
/// 소비 경로는 CompleteSlot 하나뿐이고, 남은 슬롯 수는 결산 건수로만 판단한다.
/// </summary>
public sealed class DayCycleState
{
    private readonly List<ServiceBookingState> _bookings;
    private readonly List<ServiceSettlementResult> _settlements = new();

    public int DayNumber { get; }

    /// <summary>처리를 마친 슬롯 수. 진행 눈금에 쓴다.</summary>
    public int ResolvedSlotCount => _settlements.Count;

    /// <summary>아직 접객하지 않은 예약이 남아 있는가.</summary>
    public bool HasPendingBooking => _settlements.Count < _bookings.Count;

    public DayPhaseKind Phase { get; private set; } = DayPhaseKind.None;

    public int EnergyEarned { get; private set; }

    public IReadOnlyList<ServiceBookingState> Bookings => _bookings;
    public IReadOnlyList<ServiceSettlementResult> Settlements => _settlements;

    public DayCycleState(int dayNumber, IReadOnlyList<ServiceBookingState> bookings)
    {
        DayNumber = dayNumber;
        _bookings = new List<ServiceBookingState>(bookings);
    }

    public ServiceBookingState GetBooking(int index) => _bookings[index];

    // 접객을 결산까지 마쳤다. 결과 기록과 슬롯 소비가 함께 일어난다.
    public void CompleteSlot(ServiceBookingState booking, ServiceSettlementResult result)
    {
        booking.MarkServed(result);
        _settlements.Add(result);

        EnergyEarned += result.Energy;
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