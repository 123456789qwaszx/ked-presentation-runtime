using System.Collections.Generic;
using Yarn.Unity;

public sealed partial class ServiceSessionFlow
{
    // ---- 통제 상실 이후 ----
    // 관리자 통제 신호가 거부된 뒤에는 플레이어가 개입하지 못한다.
    // 메이드가 스스로 행동을 선택하며, 그 선택은 '가장 강한 반응을 끌어내는 쪽'으로 고정된다.
    // 몬스터의 충동을 자신의 의지로 받아들이기 시작했다는 뜻이기 때문이다.
    //
    // 진행 자체는 종족 규약이 결정하고, 개체 시나리오는 더 이상 참조하지 않는다.
    private async YarnTask RunAutonomousCollapseAsync(
        ServiceSessionState session,
        ServiceSessionToken token)
    {
        session.SetPhase(ServiceSessionPhase.ControlLost);
        _screens.NotifyControlLost(session);

        SpeciesProtocol protocol = session.SpeciesProtocol;

        if (protocol == null)
            return;

        await _nodes.PlayNodeAsync(protocol.ControlLossNodeName);

        if (!IsCurrent(token))
            return;

        session.SetPhase(ServiceSessionPhase.AutonomousRunning);

        for (int i = 0; i < protocol.AutonomousBeatCount; i++)
        {
            RunAutonomousBeat(session, protocol);

            if (!IsCurrent(token))
                return;
        }

        await _nodes.PlayNodeAsync(protocol.CollapseEndingNodeName);
    }

    private void RunAutonomousBeat(ServiceSessionState session, SpeciesProtocol protocol)
    {
        ServiceBeat beat = session.CurrentBeat;
        ServiceActionOption chosen = beat != null ? PickStrongestReaction(beat.OptionPool) : null;

        MonsterReactionGrade reaction = chosen?.Reaction ?? MonsterReactionGrade.GreatlySatisfied;
        AxisTriple rawLoad = (chosen?.Load ?? AxisTriple.Zero) + protocol.AutonomousResidualLoad;

        AxisTriple applied = BurdenAccrualRule.Apply(session.Maid, rawLoad, Tuning);
        int satisfaction = session.Encounter.ApplyReaction(reaction, 0);

        session.RecordReaction(new ServiceReactionRecord(
            beat?.BeatKey ?? "autonomous",
            chosen?.OptionKey ?? "autonomous",
            reaction,
            rawLoad,
            applied,
            satisfaction,
            isAutonomous: true));

        session.MarkBeatConsumed();
    }

    private static ServiceActionOption PickStrongestReaction(IReadOnlyList<ServiceActionOption> pool)
    {
        ServiceActionOption best = null;

        for (int i = 0; i < pool.Count; i++)
        {
            if (best != null && pool[i].Reaction <= best.Reaction)
                continue;

            best = pool[i];
        }

        return best;
    }
}
