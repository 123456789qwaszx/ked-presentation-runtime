using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 접객 1회(입실 ~ 결산)를 진행한다.
///
/// 흐름은 항상 다음 순서를 따른다.
///   상황 재생 → 메이드가 능력치대로 행동 제안 → 관리자 승인 → 부담/반응 반영 → 통제 권한 재판정
/// 붕괴 한계를 넘으면 승인 흐름이 끊기고 종족 규약에 따른 자동 사건으로 넘어간다.
///
/// 세션 도중 취소/재시작이 발생할 수 있으므로, await 이후에는 항상 토큰을 검사한 뒤에만
/// 공유 상태(메이드 붕괴도, 숙련도)를 커밋한다.
/// </summary>
public sealed partial class ServiceSessionFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly ServiceOptionSelector _optionSelector;
    private readonly ServiceSettlementCalculator _settlementCalculator;
    private readonly IServicePresentationPort _presentation;

    /// <summary>표시용 문맥을 잡기 위해서만 참조한다. 판정에는 쓰지 않는다.</summary>
    public CampaignState Campaign { get; set; }

    private readonly List<ServiceActionOption> _offerBuffer = new();

    private int _runVersion;

    public ServiceSessionState Current { get; private set; }

    public ProgressionTuning Tuning => _content.Tuning;

    public ServiceSessionToken CurrentRun => new(_runVersion);

    public ServiceSessionFlow(
        GuesthouseContentDB content,
        IServicePresentationPort presentation,
        ServiceOptionSelector optionSelector = null,
        ServiceSettlementCalculator settlementCalculator = null)
    {
        _content = content;
        _presentation = presentation;
        _optionSelector = optionSelector ?? new ServiceOptionSelector();
        _settlementCalculator = settlementCalculator ?? new ServiceSettlementCalculator(content.Tuning);
    }

    public bool IsCurrent(ServiceSessionToken token) => token.Version == _runVersion;

    /// <summary>진행 중인 세션을 무효화한다. 이후 도착하는 콜백은 커밋되지 않는다.</summary>
    public void Invalidate()
    {
        _runVersion++;

        if (Current == null)
            return;

        Current.SetPhase(ServiceSessionPhase.Aborted);
        Current = null;
    }

    /// <summary>세션이 중간에 무효화되면 null 을 반환한다.</summary>
    public async YarnTask<ServiceSettlementResult> RunAsync(
        MaidRuntimeState maid,
        MonsterProfile monster,
        int dayNumber,
        int slotIndex)
    {
        if (!TryBuildSession(maid, monster, dayNumber, slotIndex, out ServiceSessionState session))
            return null;

        ServiceSessionToken token = CurrentRun;

        _presentation.NotifySessionContext(session);
        NotifyHud(session, "입실 준비");

        await _presentation.PlayNodeAsync(ResolveBriefingNode(session));

        if (!IsCurrent(token))
            return AbortSession(session);

        session.SetPhase(ServiceSessionPhase.BriefingPlayed);
        session.SetPhase(ServiceSessionPhase.RoomSealed);

        session.SetCurrentBeat(session.Scenario.EntryBeat);

        while (session.CurrentBeat != null)
        {
            bool shouldContinue = await RunBeatAsync(session, token);

            if (!IsCurrent(token))
                return AbortSession(session);

            if (!shouldContinue)
                break;
        }

        if (!session.IsControlLost)
        {
            session.SetPhase(ServiceSessionPhase.ScenarioCompleted);
            await _presentation.PlayNodeAsync(session.Scenario.CompletionNodeName);

            if (!IsCurrent(token))
                return AbortSession(session);
        }

        ServiceSettlementResult result = _settlementCalculator.Settle(session);
        session.SetPhase(ServiceSessionPhase.Settled);

        await _presentation.PresentSettlementAsync(result);

        if (!IsCurrent(token))
            return AbortSession(session);

        session.SetPhase(ServiceSessionPhase.Completed);
        Current = null;

        return result;
    }

    /// <summary>
    /// 상시 표시용 갱신. 노드를 재생하기 직전에만 불린다.
    /// 진행 문맥(일차/에너지)은 DayCycleFlow 가 세션 진입 전에 주입해 둔다.
    /// </summary>
    private void NotifyHud(ServiceSessionState session, string phaseLabel)
    {
        if (Campaign == null)
            return;

        _presentation.NotifyHud(
            GuesthouseHudSnapshot.ForSession(Campaign, Campaign.CurrentDay, session, phaseLabel));
    }

    /// <summary>다음 비트로 계속 진행할지 여부를 반환한다.</summary>
    private async YarnTask<bool> RunBeatAsync(ServiceSessionState session, ServiceSessionToken token)
    {
        ServiceBeat beat = session.CurrentBeat;

        _presentation.NotifySessionContext(session);
        NotifyHud(session, "접객 진행");

        await _presentation.PlayNodeAsync(beat.SituationNodeName);

        if (!IsCurrent(token))
            return false;

        session.SetPhase(ServiceSessionPhase.BeatSituationPlayed);

        IReadOnlyList<ServiceActionOption> options =
            _optionSelector.Select(beat, session.Maid, _offerBuffer);

        if (options.Count == 0)
            return false;

        session.SetPhase(ServiceSessionPhase.OptionsOffered);

        ServiceApprovalRequest request = new(session, beat, options, Tuning);
        int approvedIndex = await _presentation.RequestActionApprovalAsync(request);

        if (!IsCurrent(token))
            return false;

        if (approvedIndex < 0 || approvedIndex >= options.Count)
            approvedIndex = 0;

        ServiceActionOption approved = options[approvedIndex];
        session.SetPhase(ServiceSessionPhase.OptionApproved);

        await _presentation.PlayNodeAsync(approved.ApprovalNodeName);

        if (!IsCurrent(token))
            return false;

        ApplyApprovedOption(session, beat, approved);
        session.SetPhase(ServiceSessionPhase.OptionResolved);

        if (EvaluateControlAuthority(session))
        {
            await RunAutonomousCollapseAsync(session, token);
            return false;
        }

        if (approved.IsTerminalAction || beat.IsTerminal || session.IsScenarioExhausted)
            return false;

        return TryAdvanceBeat(session, beat, approved);
    }

    private void ApplyApprovedOption(
        ServiceSessionState session,
        ServiceBeat beat,
        ServiceActionOption option)
    {
        AxisTriple applied = BurdenAccrualRule.Apply(session.Maid, option.Load, Tuning);

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

    /// <summary>통제 상실이 발생했으면 true.</summary>
    private bool EvaluateControlAuthority(ServiceSessionState session)
    {
        ControlAuthorityStatus status = ControlAuthorityRule.Evaluate(
            session.Maid.Burden,
            Tuning,
            out BurdenAxis breachAxis);

        session.SetControlStatus(status, breachAxis);

        return status == ControlAuthorityStatus.Lost;
    }

    private bool TryAdvanceBeat(
        ServiceSessionState session,
        ServiceBeat currentBeat,
        ServiceActionOption approved)
    {
        if (approved.HasExplicitNextBeat &&
            session.Scenario.TryFindBeat(approved.NextBeatKey, out ServiceBeat branched))
        {
            session.SetCurrentBeat(branched);
            return true;
        }

        if (session.Scenario.TryFindNextInOrder(currentBeat.BeatKey, out ServiceBeat next))
        {
            session.SetCurrentBeat(next);
            return true;
        }

        session.SetCurrentBeat(null);
        return false;
    }

    private bool TryBuildSession(
        MaidRuntimeState maid,
        MonsterProfile monster,
        int dayNumber,
        int slotIndex,
        out ServiceSessionState session)
    {
        session = null;

        if (maid == null || monster == null)
            return false;

        if (!_content.TryFindScenarioForMonster(monster, out ServiceScenario scenario))
        {
            UnityEngine.Debug.LogError(
                $"[ServiceSessionFlow] Scenario not found. monster={monster.MonsterId}, key={monster.ScenarioKey}");
            return false;
        }

        if (scenario.EntryBeat == null)
        {
            UnityEngine.Debug.LogError(
                $"[ServiceSessionFlow] Scenario has no beat. key={scenario.ScenarioKey}");
            return false;
        }

        _content.TryFindProtocol(monster.Species, out SpeciesProtocol protocol);

        _runVersion++;

        session = new ServiceSessionState(
            CurrentRun,
            maid,
            new MonsterEncounterState(monster),
            scenario,
            protocol,
            dayNumber,
            slotIndex);

        session.SetPhase(ServiceSessionPhase.AssignmentCommitted);
        Current = session;

        return true;
    }

    private static string ResolveBriefingNode(ServiceSessionState session)
    {
        string authored = session.Scenario.BriefingNodeName;

        return string.IsNullOrWhiteSpace(authored)
            ? GuesthouseNodeNaming.ServiceBriefingFallback(session.Monster.MonsterId)
            : authored;
    }

    private ServiceSettlementResult AbortSession(ServiceSessionState session)
    {
        session.SetPhase(ServiceSessionPhase.Aborted);

        if (ReferenceEquals(Current, session))
            Current = null;

        return null;
    }
}
