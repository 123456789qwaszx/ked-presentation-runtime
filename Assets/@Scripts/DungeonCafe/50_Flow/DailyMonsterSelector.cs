using System;
using System.Collections.Generic;

// 오늘 게시판에 올라올 몬스터를 고른다.
// 신규 개체는 등장일 첫 슬롯에 보장하고,
// 나머지는 날짜와 슬롯으로 결정론 회전한다.
public sealed class DailyMonsterSelector
{
    private readonly DungeonCafeContentDB _content;

    public DailyMonsterSelector(
        DungeonCafeContentDB content)
    {
        _content = content;
    }

    public IReadOnlyList<MonsterProfile> CreateDailyBookings(
        int dayNumber)
    {
        CampaignDayPlan plan =
            _content.GetDayPlan(dayNumber);

        int serviceCount =
            plan.ServiceSlots;

        List<MonsterProfile> pool =
            _content.GetMonsterPool(dayNumber);

        if (pool.Count == 0 || serviceCount <= 0)
            return Array.Empty<MonsterProfile>();

        var selected =
            new List<MonsterProfile>(
                serviceCount);

        MonsterProfile debutant =
            FindDebutant(
                pool,
                dayNumber);

        for (int slot = 0;
             slot < serviceCount;
             slot++)
        {
            MonsterProfile monster =
                SelectSlot(
                    pool,
                    selected,
                    debutant,
                    dayNumber,
                    slot,
                    serviceCount);

            selected.Add(monster);
        }

        return selected;
    }

    private static MonsterProfile FindDebutant(
        IReadOnlyList<MonsterProfile> pool,
        int dayNumber)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].AppearDay == dayNumber)
                return pool[i];
        }

        return null;
    }

    private static MonsterProfile SelectSlot(
        IReadOnlyList<MonsterProfile> pool,
        List<MonsterProfile> selected,
        MonsterProfile debutant,
        int dayNumber,
        int slot,
        int serviceCount)
    {
        if (slot == 0 && debutant != null)
            return debutant;

        int index =
            (dayNumber * 3 + slot * 5)
            % pool.Count;

        MonsterProfile pick =
            pool[index];

        if (!selected.Contains(pick)
            || pool.Count <= serviceCount)
        {
            return pick;
        }

        return pool[
            (index + 1) % pool.Count];
    }
}