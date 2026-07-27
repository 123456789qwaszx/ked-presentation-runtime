#pragma warning disable 1998   // 헤드리스는 의도적으로 동기 완료한다 — EditMode 테스트 구동 전제.
using System;
using System.Collections.Generic;
using Yarn.Unity;

/// <summary>헤드리스 정책.</summary>
public enum HeadlessPolicyV3
{
    /// <summary>80~99 착지를 노린다. 심층 회수는 즉시 탈출.</summary>
    Ideal = 0,
    /// <summary>약·중만 승인하는 안전 운행. 할당 미달 검증용. (§17)</summary>
    Safe = 1,
}

/// <summary>노드 재생 no-op. 재생된 노드 이름만 기록한다.</summary>
public sealed class HeadlessNodePlayerV3 : INodePlayerV3
{
    public readonly List<string> PlayedNodes = new();
    public async YarnTask PlayNodeAsync(string nodeName)
    {
        if (!string.IsNullOrEmpty(nodeName)) PlayedNodes.Add(nodeName);
    }
}

/// <summary>
/// UI 없이 캠페인 전체를 굴리는 자동 응답 v3. (§17 회귀 지표의 구동체)
/// 모든 응답이 동기 완료되므로 EditMode 에서 RunAsync 가 즉시 끝난다.
/// 자체 난수를 쓰지 않는다 — 같은 시드·같은 정책이면 항상 같은 결과.
/// </summary>
public sealed class HeadlessV3Screens : IGuesthouseV3Screens
{
    private readonly HeadlessPolicyV3 _policy;

    // §17 지표 수집
    public int ServiceCount;
    public int LandingCount;            // 80~99 착지
    public int DepthEntryCount;
    public int DepthBeatTotal;
    public int TotalCollapseCount;
    public int FirstQuotaMissDay = -1;

    public HeadlessV3Screens(HeadlessPolicyV3 policy) { _policy = policy; }

    public async YarnTask PresentBoardAsync(int day, IReadOnlyList<MonsterProfileV3> bookings, CampaignStateV3 campaign) { }

    public async YarnTask<string> RequestAssignmentAsync(
        MonsterProfileV3 monster, IReadOnlyList<MaidStateV3> candidates, CampaignStateV3 campaign)
    {
        // 요구축 적성 높고, 그 축 여유가 큰 메이드. (기존 헤드리스 정책 계승)
        MaidStateV3 best = candidates[0];
        int bestScore = int.MinValue;
        for (int i = 0; i < candidates.Count; i++)
        {
            MaidStateV3 c = candidates[i];
            int headroom = campaign.Tuning.ControlLossThreshold - c.Gauge.Get(monster.DemandAxis);
            int score = c.Aptitude[monster.DemandAxis] * 10 + headroom;
            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best.MaidId;
    }

    public async YarnTask<ApprovalResponseV3> RequestApprovalAsync(ApprovalRequestV3 request)
    {
        ServiceSessionStateV3 session = request.Session;
        int collapse = session.Maid.Gauge.Get(session.Monster.DemandAxis);

        OptionIntensity? last = session.LastApprovedIntensity;
        MonsterSpecialRule rule = session.Monster.SpecialRule;

        if (_policy == HeadlessPolicyV3.Safe)
        {
            OptionIntensity safePick = collapse >= 60 ? OptionIntensity.Light : OptionIntensity.Medium;
            if (rule == MonsterSpecialRule.RepetitionBoredom && last == safePick)
                safePick = safePick == OptionIntensity.Medium ? OptionIntensity.Light : OptionIntensity.Medium;
            return new ApprovalResponseV3(PickIntensity(request, safePick));
        }

        // Ideal: 만족도 압박(강 필요)과 100 초과 위험 사이. 시뮬 확정 임계 22/10.
        int headroom = 100 - collapse;

        // 감김상·침면: 종료 ≥80 이면 강제 추가 비트 — 80 미만 착지를 예산으로 잡는다. (§13.2 카운터플레이)
        if (rule == MonsterSpecialRule.Overstay || rule == MonsterSpecialRule.OverstayVeil)
            headroom = 80 - collapse;

        OptionIntensity pick =
            headroom > 22 ? OptionIntensity.Heavy :
            headroom > 10 ? OptionIntensity.Medium :
            OptionIntensity.Light;

        // 기록수: 같은 강도 반복은 반응 하향 — 강도를 변주한다. (§13.2 카운터플레이)
        if (rule == MonsterSpecialRule.RepetitionBoredom && last == pick)
        {
            pick = pick == OptionIntensity.Heavy ? OptionIntensity.Medium
                 : pick == OptionIntensity.Medium && headroom > 22 ? OptionIntensity.Heavy
                 : OptionIntensity.Light;
        }

        return new ApprovalResponseV3(PickIntensity(request, pick));
    }

    private static int PickIntensity(ApprovalRequestV3 request, OptionIntensity wanted)
    {
        for (int i = 0; i < request.Options.Count; i++)
            if (request.Options[i].Intensity == wanted) return request.Options[i].Index;
        return 0;
    }

    public async YarnTask<IReadOnlyList<string>> RequestDepthInterventionAsync(DepthInterventionRequestV3 request)
    {
        if (request.DepthBeatIndex == 1) DepthEntryCount++;
        DepthBeatTotal++;
        return Array.Empty<string>();   // 봇은 개입권 미사용 — 무개입 지표. (§17)
    }

    public async YarnTask<DepthRollDecisionV3> PresentDepthRollAsync(
        ServiceSessionStateV3 session, DepthRollResult roll, IReadOnlyList<string> postRollAbilityIds)
        => DepthRollDecisionV3.None;

    public async YarnTask<bool> RequestRecoveryChoiceAsync(ServiceSessionStateV3 session)
        => true;   // 즉시 탈출. (§3.3 초반 정답)

    public async YarnTask PresentSettlementAsync(ServiceSessionStateV3 session, SettlementV3Result result)
    {
        ServiceCount++;
        if (result.Kind == SettlementOutcomeKind.Normal && result.EndCollapse >= 80) LandingCount++;
        if (result.Kind == SettlementOutcomeKind.TotalCollapse) TotalCollapseCount++;
    }

    public async YarnTask<NightPrepResponseV3> RequestNightPrepAsync(NightPrepRequestV3 request)
    {
        // 살 수 있는 가장 싼 능력 1개 구매, 장착은 소유 순서대로 슬롯만큼.
        string cheapest = null;
        int cost = int.MaxValue;
        for (int i = 0; i < request.Purchasable.Count; i++)
        {
            PlayerAbilityDefinition def = request.Purchasable[i];
            if (def.DesireCost < cost && def.DesireCost <= request.HeldDesire)
            { cost = def.DesireCost; cheapest = def.Id; }
        }

        var equips = new List<string>();
        foreach (string id in request.Owned)
        {
            if (equips.Count >= request.SlotLimit) break;
            equips.Add(id);
        }
        if (cheapest != null && equips.Count < request.SlotLimit) equips.Add(cheapest);

        return new NightPrepResponseV3(
            cheapest != null ? new[] { cheapest } : Array.Empty<string>(), equips);
    }

    public async YarnTask<IReadOnlyList<NightChoiceV3>> RequestNightPlanAsync(NightPlanRequestV3 request)
    {
        // 우선순위: ① 80~99 관리 붕괴 ② 후유증 안정 ③ 최고 붕괴 안정. (§5.1)
        var choices = new List<NightChoiceV3>(request.ManageCount);
        var taken = new HashSet<string>(StringComparer.Ordinal);

        for (int pass = 0; pass < 3 && choices.Count < request.ManageCount; pass++)
        {
            MaidStateV3 pick = null;
            int pickValue = -1;

            for (int i = 0; i < request.Maids.Count; i++)
            {
                MaidStateV3 m = request.Maids[i];
                if (taken.Contains(m.MaidId)) continue;
                m.Gauge.HighestAxis(out int v);

                bool match = pass switch
                {
                    0 => request.CanRelease(m),
                    1 => m.HasAftereffect,
                    _ => true,
                };
                if (match && v > pickValue) { pickValue = v; pick = m; }
            }

            if (pick == null) continue;
            taken.Add(pick.MaidId);
            choices.Add(new NightChoiceV3(pick.MaidId,
                pass == 0 ? NightChoiceKind.ManagedRelease : NightChoiceKind.Care));
        }

        return choices;
    }

    public async YarnTask PresentNeglectAsync(MaidStateV3 maid, NeglectJudgment judgment) { }

    public async YarnTask PresentDayReportAsync(CampaignStateV3 campaign, DayStateV3 day, bool quotaMet)
    {
        if (!quotaMet && FirstQuotaMissDay < 0) FirstQuotaMissDay = day.DayNumber;
    }

    public async YarnTask PresentEndingAsync(CampaignStateV3 campaign, EndingKindV3 ending) { }
}
