using System.Text;
using Yarn.Unity;

/// <summary>
/// §17 회귀 지표 집계기. 에디터 밸런스 창과 테스트가 함께 쓴다.
/// 헤드리스 응답이 전부 동기 완료되므로 RunAsync 는 호출 즉시 끝난다.
/// </summary>
public static class GuesthouseV3HeadlessValidator
{
    public sealed class Report
    {
        public int Seeds;
        public int Completions;
        public int Services;
        public int Landings;
        public int DepthEntries;
        public int DepthBeats;
        public int TotalCollapses;
        public double AvgLifetime;
        public double AvgShopLevel;
        public int SafeFirstMissDayMin = int.MaxValue;
        public int SafeFirstMissDayMax = -1;
        public int Stage4Count;

        public double CompletionRate => Seeds == 0 ? 0 : (double)Completions / Seeds;
        public double LandingRate => Services == 0 ? 0 : (double)Landings / Services;
        public double DepthEntryRate => Services == 0 ? 0 : (double)DepthEntries / Services;
        public double AvgDepthStay => DepthEntries == 0 ? 0 : (double)DepthBeats / DepthEntries;
        public double CollapseAfterDepth => DepthEntries == 0 ? 0 : (double)TotalCollapses / DepthEntries;

        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"시드 {Seeds}개 — §17 지표");
            sb.AppendLine($"  완주율            {CompletionRate:P0}   (목표 ≥90%)");
            sb.AppendLine($"  80~99 착지율      {LandingRate:P0}   (목표 35~55%)");
            sb.AppendLine($"  심층 진입률       {DepthEntryRate:P0}   (목표 5~12%)");
            sb.AppendLine($"  심층 평균 체류    {AvgDepthStay:0.00}비트 (목표 2~4, 즉시 탈출 정책 기준)");
            sb.AppendLine($"  심층 후 완전붕괴  {CollapseAfterDepth:P0}   (목표 20~40%)");
            sb.AppendLine($"  최종 누적 욕구    {AvgLifetime:0}   (목표 6000~7500)");
            sb.AppendLine($"  평균 가게 레벨    {AvgShopLevel:0.0}");
            sb.AppendLine($"  관계 4단계 도달   {Stage4Count}/{Seeds}");
            if (SafeFirstMissDayMax >= 0)
                sb.AppendLine($"  [안전 정책] 첫 할당 미달일 {SafeFirstMissDayMin}~{SafeFirstMissDayMax} (목표 3~5 — 안전 운행은 빠르게 실패해야 한다)");
            return sb.ToString();
        }
    }

    public static Report Run(HeadlessPolicyV3 policy, int seedCount, ulong seedBase = 1000UL)
    {
        var report = new Report { Seeds = seedCount };
        long lifetimeSum = 0;
        long shopSum = 0;

        for (int s = 0; s < seedCount; s++)
        {
            GuesthouseV3ContentDB content = GuesthouseV3Content.Build();
            GuesthouseTuningV3 tuning = GuesthouseTuningV3.CreateStandard();
            var campaign = new CampaignStateV3(content, tuning, seedBase + (ulong)s);
            var screens = new HeadlessV3Screens(policy);
            var nodes = new HeadlessNodePlayerV3();
            var flow = new CampaignFlowV3(campaign, screens, nodes);

            YarnTask<EndingKindV3> task = flow.RunAsync();   // 동기 완료

            report.Services += screens.ServiceCount;
            report.Landings += screens.LandingCount;
            report.DepthEntries += screens.DepthEntryCount;
            report.DepthBeats += screens.DepthBeatTotal;
            report.TotalCollapses += screens.TotalCollapseCount;
            lifetimeSum += campaign.Ledger.Lifetime;
            shopSum += campaign.ShopLevel;

            if (campaign.Ending is EndingKindV3.FullHouseMorning
                or EndingKindV3.NormalBusiness or EndingKindV3.ClosingTime)
                report.Completions++;

            if (EndingResolverV3.HasStage4(campaign)) report.Stage4Count++;

            if (policy == HeadlessPolicyV3.Safe && screens.FirstQuotaMissDay > 0)
            {
                if (screens.FirstQuotaMissDay < report.SafeFirstMissDayMin)
                    report.SafeFirstMissDayMin = screens.FirstQuotaMissDay;
                if (screens.FirstQuotaMissDay > report.SafeFirstMissDayMax)
                    report.SafeFirstMissDayMax = screens.FirstQuotaMissDay;
            }

            campaign.ReleaseCounters();
        }

        report.AvgLifetime = seedCount == 0 ? 0 : (double)lifetimeSum / seedCount;
        report.AvgShopLevel = seedCount == 0 ? 0 : (double)shopSum / seedCount;
        return report;
    }
}
