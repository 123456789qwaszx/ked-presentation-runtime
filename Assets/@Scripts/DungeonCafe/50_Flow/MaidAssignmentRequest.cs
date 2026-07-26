using System.Collections.Generic;

public sealed class MaidAssignmentRequest
{
    public ServiceBookingState Booking { get; }
    public IReadOnlyList<MaidRuntimeState> Candidates { get; }
    public ProgressionTuning Tuning { get; }

    public MaidAssignmentRequest(
        ServiceBookingState booking,
        IReadOnlyList<MaidRuntimeState> candidates,
        ProgressionTuning tuning)
    {
        Booking = booking;
        Candidates = candidates;
        Tuning = tuning;
    }

    public MonsterProfile Monster => Booking.Monster;

    // 예약 확정 전에는 대응 타입이 수첩에 기재되지 않는다.
    public bool IsDemandAxisKnown => Booking.IsConfirmed;
}
