using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 접객 1회(입실 ~ 결산)를 진행한다.
///
/// 흐름은 항상 다음 순서를 따른다.
///   상황 재생 → 메이드가 능력치대로 행동 제안 → 관리자 승인 → 부담/반응 반영 → 통제 권한 재판정
/// 붕괴 한계를 넘으면 승인 흐름이 끊기고 종족 규약에 따른 자동 사건으로 넘어간다.
///
/// 세션은 언제든 Invalidate 로 무효화될 수 있다.
/// 노드 재생과 화면 대기는 전부 TryPlayNodeAsync / IsCurrent 를 거치며,
/// 무효화된 뒤에는 Abort 만 호출되고 결과는 커밋되지 않는다.
/// </summary>
public sealed partial class ServiceSessionFlow
{
    private readonly GuesthouseContentDB _content;
    private readonly ServiceOptionSelector _optionSelector;
    private readonly ServiceSettlementCalculator _settlementCalculator;

    private readonly VnScreenBindings _screens;
    private readonly ScenarioNodeRunner _nodes;
    private readonly GuesthouseYarnContext _yarnContext;

    private readonly List<ServiceActionOption> _offerBuffer = new();

    /// <summary>표시용 문맥. 판정에는 쓰지 않는다.</summary>
    private CampaignState _campaign;

    private int _runVersion;

    public ServiceSessionState Current { get; private set; }

    public ProgressionTuning Tuning => _content.Tuning;

    public ServiceSessionToken CurrentRun => new(_runVersion);

    public ServiceSessionFlow(
        GuesthouseContentDB content,
        VnScreenBindings screens,
        ScenarioNodeRunner nodes,
        GuesthouseYarnContext yarnContext = null,
        ServiceOptionSelector optionSelector = null,
        ServiceSettlementCalculator settlementCalculator = null)
    {
        _content = content;
        _screens = screens;
        _nodes = nodes;
        _yarnContext = yarnContext;
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

    public async YarnTask<ServiceSettlementResult> RunAsync(
        CampaignState campaign,
        ServiceBookingState booking,
        MaidRuntimeState maid)
    {
        _campaign = campaign;

        if (!TryBuildSession(booking, maid, campaign.CurrentDay.DayNumber, out ServiceSessionState session))
            return null;

        Enter(session, ServiceSessionPhase.AssignmentCommitted);

        if (!await TryPlayNodeAsync(session, ResolveBriefingNode(session)))
            return Abort(session);

        Enter(session, ServiceSessionPhase.RoomSealed);

        session.SetCurrentBeat(session.Scenario.EntryBeat);

        while (session.CurrentBeat != null)
        {
            bool shouldContinue = await RunBeatAsync(session);

            if (!IsCurrent(session))
                return Abort(session);

            if (!shouldContinue)
                break;
        }

        if (!session.IsControlLost)
        {
            Enter(session, ServiceSessionPhase.ScenarioCompleted);

            if (!await TryPlayNodeAsync(session, session.Scenario.CompletionNodeName))
                return Abort(session);
        }

        return await SettleAsync(session);
    }

    private async YarnTask<ServiceSettlementResult> SettleAsync(ServiceSessionState session)
    {
        ServiceSettlementResult result = _settlementCalculator.Settle(session);

        Enter(session, ServiceSessionPhase.Settled);
        await _screens.PresentSettlementAsync(result);

        if (!IsCurrent(session))
            return Abort(session);

        session.SetPhase(ServiceSessionPhase.Completed);
        Current = null;

        return result;
    }

    /// <summary>다음 비트로 계속 진행할지 여부를 반환한다.</summary>
    private async YarnTask<bool> RunBeatAsync(ServiceSessionState session)
    {
        ServiceBeat beat = session.CurrentBeat;

        Enter(session, ServiceSessionPhase.BeatSituationPlayed);

        if (!await TryPlayNodeAsync(session, beat.SituationNodeName))
            return false;

        IReadOnlyList<ServiceActionOption> options =
            _optionSelector.Select(beat, session.Maid, _offerBuffer);

        if (options.Count == 0)
            return false;

        session.SetPhase(ServiceSessionPhase.OptionsOffered);

        ServiceActionOption approved = await ResolveApprovedOptionAsync(session, beat, options);

        if (approved == null)
            return false;

        if (!await TryPlayNodeAsync(session, approved.ApprovalNodeName))
            return false;

        ApplyApprovedOption(session, beat, approved);
        session.SetPhase(ServiceSessionPhase.OptionResolved);

        if (EvaluateControlAuthority(session))
        {
            await RunAutonomousCollapseAsync(session);
            return false;
        }

        if (approved.IsTerminalAction || beat.IsTerminal || session.IsScenarioExhausted)
            return false;

        return TryAdvanceBeat(session, beat, approved);
    }

    /// <summary>승인 입력을 기다린다. 세션이 무효화되면 null 을 반환한다.</summary>
    private async YarnTask<ServiceActionOption> ResolveApprovedOptionAsync(
        ServiceSessionState session,
        ServiceBeat beat,
        IReadOnlyList<ServiceActionOption> options)
    {
        ServiceApprovalRequest request = new(session, beat, options, Tuning);
        int index = await _screens.RequestActionApprovalAsync(request);

        if (!IsCurrent(session))
            return null;

        // 패널이 취소나 범위 밖 값을 돌려주면 첫 번째 제안으로 진행한다.
        if (index < 0 || index >= options.Count)
            index = 0;

        session.SetPhase(ServiceSessionPhase.OptionApproved);

        return options[index];
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
        ServiceBookingState booking,
        MaidRuntimeState maid,
        int dayNumber,
        out ServiceSessionState session)
    {
        session = null;

        MonsterProfile monster = booking.Monster;

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
            dayNumber);

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

    // ------------------------------------------------------------
    // 진행 보조
    // ------------------------------------------------------------

    /// <summary>세션이 아직 유효한가. 토큰은 세션이 들고 있으므로 따로 넘기지 않는다.</summary>
    private bool IsCurrent(ServiceSessionState session) => IsCurrent(session.Token);

    /// <summary>
    /// 단계 진입. 페이즈 기록 · 대본 변수 주입 · 표시 갱신이 항상 함께 일어난다.
    /// 노드를 재생하기 '직전'에 불려야 대사와 표시가 같은 프레임에 나타난다.
    /// </summary>
    private void Enter(ServiceSessionState session, ServiceSessionPhase phase)
    {
        session.SetPhase(phase);

        _yarnContext?.PushSession(session);

        if (_campaign == null)
            return;

        _screens.UpdateGuesthouseHud(
            GuesthouseHudSnapshot.ForSession(
                _campaign,
                _campaign.CurrentDay,
                session,
                ServiceSessionPhaseLabels.Of(phase)));
    }

    /// <summary>노드를 재생한다. 재생 도중 세션이 무효화되지 않았으면 true.</summary>
    private async YarnTask<bool> TryPlayNodeAsync(ServiceSessionState session, string nodeName)
    {
        await _nodes.PlayNodeAsync(nodeName);

        return IsCurrent(session);
    }

    private ServiceSettlementResult Abort(ServiceSessionState session)
    {
        session.SetPhase(ServiceSessionPhase.Aborted);

        if (ReferenceEquals(Current, session))
            Current = null;

        return null;
    }
}