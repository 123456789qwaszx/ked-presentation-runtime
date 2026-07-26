using System.Collections.Generic;

public sealed class MaidAssignmentRequest
{
    public ServiceBookingState Booking { get; }
    public IReadOnlyList<MaidRuntimeState> Candidates { get; }

    public MaidAssignmentRequest(
        ServiceBookingState booking,
        IReadOnlyList<MaidRuntimeState> candidates)
    {
        Booking = booking;
        Candidates = candidates;
    }

    public MonsterProfile Monster => Booking.Monster;

    // 예약 확정 전에는 대응 타입이 수첩에 기재되지 않는다.
    public bool IsDemandAxisKnown => Booking.IsConfirmed;
}
