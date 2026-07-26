using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 접객 1회. 입실 → 비트 반복 → 결산.
// 비트 하나는 상황 재생 → 메이드 제안 → 관리자 승인 → 부담과 반응 반영이다.
// 붕괴 한계를 넘으면 개입이 끊기고 종족 규약이 진행을 가져간다.
public sealed partial class ServiceSessionFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly ServiceOptionSelector _optionSelector;
    private readonly ServiceSettlementCalculator _settlementCalculator;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;

    private readonly List<ServiceActionOption> _offerBuffer = new();

    public ServiceSessionFlow(
        GuesthouseContentDB content,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes,
        ServiceOptionSelector optionSelector,
        ServiceSettlementCalculator settlementCalculator)
    {
        _content = content;
        _screens = screens;
        _nodes = nodes;
        _optionSelector = optionSelector;
        _settlementCalculator = settlementCalculator;
    }

    public async Task<ServiceSettlementResult> RunAsync(
        CampaignState campaign,
        ServiceBookingState booking,
        MaidRuntimeState maid)
    {
        ServiceSessionState session = OpenSession(booking, maid, campaign.CurrentDay.DayNumber);

        // 입실. 격리실이 봉인되고 이후 개입은 승인으로만 가능하다
        await _nodes.PlayNodeAsync(ResolveBriefingNode(session));

        session.SetCurrentBeat(session.Scenario.EntryBeat);

        while (session.CurrentBeat != null)
        {
            ServiceBeat beat = session.CurrentBeat;

            // 상황 재생
            await _nodes.PlayNodeAsync(beat.SituationNodeName);

            // 메이드가 능력치대로 제안하고, 관리자가 그중 하나를 승인한다
            IReadOnlyList<ServiceActionOption> options =
                _optionSelector.Select(beat, session.Maid, _offerBuffer);

            ServiceApprovalRequest request = new(session, beat, options, _content.Tuning);
            int approvedIndex = await _screens.RequestActionApprovalAsync(request);

            ServiceActionOption approved = options[approvedIndex];

            await _nodes.PlayNodeAsync(approved.ApprovalNodeName);

            // 부담 누적과 몬스터 반응을 함께 기록한다
            ApplyApprovedOption(session, beat, approved);

            // 통제 신호가 거부되면 여기서 개입이 끝난다
            if (HasLostControl(session))
            {
                await RunAutonomousCollapseAsync(session);
                break;
            }

            session.SetCurrentBeat(ResolveNextBeat(session, beat, approved));
        }

        // 통제를 지킨 접객만 마무리 대사를 받는다
        if (!session.IsControlLost)
            await _nodes.PlayNodeAsync(session.Scenario.CompletionNodeName);

        // 반응 점수 × 붕괴 배율 결산
        ServiceSettlementResult result = _settlementCalculator.Settle(session);

        await _screens.PresentSettlementAsync(result);

        return result;
    }

    /// <summary>다음 비트. null 이면 시나리오가 끝났다는 뜻이다.</summary>
    private static ServiceBeat ResolveNextBeat(
        ServiceSessionState session,
        ServiceBeat current,
        ServiceActionOption approved)
    {
        if (approved.IsTerminalAction || current.IsTerminal || session.IsScenarioExhausted)
            return null;

        if (approved.HasExplicitNextBeat &&
            session.Scenario.TryFindBeat(approved.NextBeatKey, out ServiceBeat branched))
            return branched;

        return session.Scenario.TryFindNextInOrder(current.BeatKey, out ServiceBeat next)
            ? next
            : null;
    }

    private void ApplyApprovedOption(
        ServiceSessionState session,
        ServiceBeat beat,
        ServiceActionOption option)
    {
        AxisTriple applied = BurdenAccrualRule.Apply(session.Maid, option.Load, _content.Tuning);

        int satisfaction = session.Encounter.ApplyReaction(option.Reaction, option.SatisfactionBonus);

        session.RecordReaction(new ServiceReactionRecord(
            beat.BeatKey,
            option.OptionKey,
            option.Reaction,
            option.Load,
            applied,
            satisfaction,
            isAutonomous: false));

        session.MarkBeatConsumed();
    }

    /// <summary>붕괴 한계를 넘겨 통제 신호가 거부되었는가.</summary>
    private bool HasLostControl(ServiceSessionState session)
    {
        ControlAuthorityStatus status = ControlAuthorityRule.Evaluate(
            session.Maid.Burden,
            _content.Tuning,
            out BurdenAxis breachAxis);

        session.SetControlStatus(status, breachAxis);

        return status == ControlAuthorityStatus.Lost;
    }

    /// <summary>시나리오와 종족 규약을 묶어 세션을 연다. 콘텐츠가 어긋나면 그대로 터진다.</summary>
    private ServiceSessionState OpenSession(
        ServiceBookingState booking,
        MaidRuntimeState maid,
        int dayNumber)
    {
        MonsterProfile monster = booking.Monster;

        if (!_content.TryFindScenarioForMonster(monster, out ServiceScenario scenario))
            throw new InvalidOperationException(
                $"시나리오를 찾지 못했습니다. monster={monster.MonsterId}, key={monster.ScenarioKey}");

        if (scenario.EntryBeat == null)
            throw new InvalidOperationException(
                $"시나리오에 비트가 없습니다. key={scenario.ScenarioKey}");

        if (!_content.TryFindProtocol(monster.Species, out SpeciesProtocol protocol))
            throw new InvalidOperationException(
                $"종족 규약을 찾지 못했습니다. species={monster.Species}");

        return new ServiceSessionState(
            maid,
            new MonsterEncounterState(monster),
            scenario,
            protocol,
            dayNumber);
    }

    private static string ResolveBriefingNode(ServiceSessionState session)
    {
        string authored = session.Scenario.BriefingNodeName;

        return string.IsNullOrWhiteSpace(authored)
            ? GuesthouseNodeNaming.ServiceBriefingFallback(session.Monster.MonsterId)
            : authored;
    }
}