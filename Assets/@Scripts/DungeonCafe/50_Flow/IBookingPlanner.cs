using System;
using System.Collections.Generic;

public sealed class BookingPlanner
{
    private readonly GuesthouseContentDB _content;

    public BookingPlanner(GuesthouseContentDB content)
    {
        _content = content;
    }

    public IReadOnlyList<ServiceBookingState> CreateDailyBookings(int dayNumber)
    {
        IReadOnlyList<MonsterProfile> monsters = _content.Monsters;
        int count = _content.Tuning.ServicesPerDay;

        if (monsters.Count == 0 || count <= 0)
            return Array.Empty<ServiceBookingState>();

        ServiceBookingState[] bookings = new ServiceBookingState[count];

        // 오늘이 전체 몬스터 순서에서 몇 번째부터 시작하는지
        int offset = (dayNumber - 1) * count;

        // 목록 끝을 넘으면 처음으로 돌아간다
        for (int slot = 0; slot < count; slot++)
            bookings[slot] = new ServiceBookingState(monsters[(offset + slot) % monsters.Count]);

        return bookings;
    }
}