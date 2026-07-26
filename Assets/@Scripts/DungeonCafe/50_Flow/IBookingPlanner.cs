using System.Collections.Generic;

/// <summary>
/// 그날 게시판에 올라올 예약 문의를 결정한다.
/// 버티컬 슬라이스에서는 고정 순환으로 충분하지만, 이후 평판/난이도 곡선으로 교체할 수 있게 분리한다.
/// </summary>
public interface IBookingPlanner
{
    IReadOnlyList<MonsterProfile> PlanBookings(CampaignState campaign, int dayNumber, int count);
}

/// <summary>
/// 콘텐츠 정의 순서를 그대로 순환시키는 기본 구현.
/// 무작위성을 쓰지 않으므로 같은 세이브에서 항상 같은 예약이 올라온다.
/// </summary>
public sealed class RotatingBookingPlanner : IBookingPlanner
{
    private readonly GuesthouseContentDB _content;
    private readonly List<MonsterProfile> _buffer = new();

    public RotatingBookingPlanner(GuesthouseContentDB content)
    {
        _content = content;
    }

    public IReadOnlyList<MonsterProfile> PlanBookings(CampaignState campaign, int dayNumber, int count)
    {
        _buffer.Clear();

        IReadOnlyList<MonsterProfile> monsters = _content.Monsters;

        if (monsters.Count == 0)
            return _buffer;

        // 오늘이 전체 몬스터 순서에서 몇 번째부터 시작하는지 계산
        int offset = (dayNumber - 1) * count;

        // 목록 끝을 넘으면 처음으로 돌아가면서 몬스터 추가
        for (int i = 0; i < count; i++)
            _buffer.Add(monsters[(offset + i) % monsters.Count]);

        return _buffer;
    }
}
