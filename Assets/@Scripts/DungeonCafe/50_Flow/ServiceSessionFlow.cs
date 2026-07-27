using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 접객 1회. 입실 -> 비트 반복 -> 결산.
// 비트 하나는 상황 재생 -> 메이드 제안 -> 관리자 승인 -> 부담과 반응 반영이다.
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

        // 입실 브리핑 재생
        await _nodes.PlayNodeAsync(ResolveBriefingNode(session));

        session.SetCurrentBeat(session.Scenario.EntryBeat);

        while (session.CurrentBeat != null)
        {
            ServiceBeat beat = session.CurrentBeat;

            // 현재 상황 재생
            await _nodes.PlayNodeAsync(beat.SituationNodeName);

            // 메이드가 선택지를 제안
            IReadOnlyList<ServiceActionOption> options =
                _optionSelector.Select(beat, session.Maid, _offerBuffer);
            
            // 플레이어의 승인 대기
            ServiceApprovalRequest request = new(session, beat, options, _content.Tuning);
            int approvedIndex = await _screens.RequestActionApprovalAsync(request);

            // 승인된 행동의 연출 재생
            ServiceActionOption approved = options[approvedIndex];
            await _nodes.PlayNodeAsync(approved.ApprovalNodeName);

            // 부담과 몬스터 반응 적용
            ApplyApprovedOption(session, beat, approved);

            // 통제권을 잃을 시 자동 진행
            if (HasLostControl(session))
            {
                await RunAutonomousCollapseAsync(session);
                break;
            }

            session.SetCurrentBeat(ResolveNextBeat(session, beat, approved));
        }

        // 정상 완료 대사 재생
        if (!session.IsControlLost)
            await _nodes.PlayNodeAsync(session.Scenario.CompletionNodeName);

        // 결산 화면 계산 및 표시
        ServiceSettlementResult result = _settlementCalculator.Settle(session);
        await _screens.PresentSettlementAsync(result);

        return result;
    }

    // 다음 비트 재생.
    // null은 시나리오가 종료됐다는 의미
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

    // 붕괴한계 초과 및 통제권 상실
    private bool HasLostControl(ServiceSessionState session)
    {
        ControlAuthorityStatus status = ControlAuthorityRule.Evaluate(
            session.Maid.Burden,
            _content.Tuning,
            out BurdenAxis breachAxis);

        session.SetControlStatus(status, breachAxis);

        return status == ControlAuthorityStatus.Lost;
    }

    // 시나리오와 종족 규약을 묶어 세션을 연다. 콘텐츠가 어긋나면 그대로 터뜨림.
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