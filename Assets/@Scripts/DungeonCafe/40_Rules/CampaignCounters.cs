using System;
using System.Collections.Generic;

/// <summary>
/// 타입별 접객/목격 카운트. 능력 게이트(§11)가 조회한다.
/// 상태에 필드를 늘리는 대신 캠페인이 보관하는 사전을 확장 메서드로 감싼다.
/// </summary>
public static class CampaignCounterExtensions
{
    private static readonly Dictionary<CampaignState, int[]> ServiceCounts = new();
    private static readonly Dictionary<CampaignState, int[]> WitnessCounts = new();

    private static int[] Book(Dictionary<CampaignState, int[]> map, CampaignState c)
    {
        if (!map.TryGetValue(c, out int[] arr)) { arr = new int[3]; map[c] = arr; }
        return arr;
    }

    public static void CountService(this CampaignState c, ResearchType type)
        => Book(ServiceCounts, c)[(int)type]++;

    public static void CountWitness(this CampaignState c, ResearchType type)
        => Book(WitnessCounts, c)[(int)type]++;

    public static int ServiceCountByType(this CampaignState c, ResearchType type)
        => Book(ServiceCounts, c)[(int)type];

    public static int WitnessCountByType(this CampaignState c, ResearchType type)
        => Book(WitnessCounts, c)[(int)type];

    public static void RestoreCounters(this CampaignState c, int[] service, int[] witness)
    {
        int[] s = Book(ServiceCounts, c);
        int[] w = Book(WitnessCounts, c);
        for (int i = 0; i < 3; i++) { s[i] = service?[i] ?? 0; w[i] = witness?[i] ?? 0; }
    }

    public static (int[] service, int[] witness) SnapshotCounters(this CampaignState c)
        => ((int[])Book(ServiceCounts, c).Clone(), (int[])Book(WitnessCounts, c).Clone());

    /// <summary>캠페인 폐기 시 누수 방지.</summary>
    public static void ReleaseCounters(this CampaignState c)
    {
        ServiceCounts.Remove(c);
        WitnessCounts.Remove(c);
    }
}
