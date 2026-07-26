using System.Collections.Generic;
using System.Threading.Tasks;

// ---- 통제 상실 이후 ----
// 관리자 통제 신호가 거부된 뒤에는 플레이어가 개입하지 못한다.
// 메이드가 스스로 행동을 선택하며, 그 선택은 '가장 강한 반응을 끌어내는 쪽'으로 고정된다.
// 몬스터의 충동을 자신의 의지로 받아들이기 시작했다는 뜻이기 때문이다.
//
// 진행 횟수와 잔여 부담은 종족 규약이 정한다. 시나리오는 더 이상 분기하지 않는다.
public sealed partial class ServiceSessionFlow
{
    private const string AutonomousKey = "autonomous";

    private async Task RunAutonomousCollapseAsync(ServiceSessionState session)
    {
        SpeciesProtocol protocol = session.SpeciesProtocol;

        _screens.NotifyControlLost(session);

        await _nodes.PlayNodeAsync(protocol.ControlLossNodeName);

        for (int i = 0; i < protocol.AutonomousBeatCount; i++)
            RunAutonomousBeat(session, protocol);

        await _nodes.PlayNodeAsync(protocol.CollapseEndingNodeName);
    }

    private void RunAutonomousBeat(ServiceSessionState session, SpeciesProtocol protocol)
    {
        ServiceBeat beat = session.CurrentBeat;
        ServiceActionOption chosen = PickStrongestReaction(beat.OptionPool);

        AxisTriple rawLoad = chosen.Load + protocol.AutonomousResidualLoad;
        AxisTriple applied = BurdenAccrualRule.Apply(session.Maid, rawLoad, _content.Tuning);

        int satisfaction = session.Encounter.ApplyReaction(chosen.Reaction, 0);

        session.RecordReaction(new ServiceReactionRecord(
            beat.BeatKey,
            AutonomousKey,
            chosen.Reaction,
            rawLoad,
            applied,
            satisfaction,
            isAutonomous: true));

        session.MarkBeatConsumed();
    }

    private static ServiceActionOption PickStrongestReaction(IReadOnlyList<ServiceActionOption> pool)
    {
        ServiceActionOption best = pool[0];

        for (int i = 1; i < pool.Count; i++)
        {
            if (pool[i].Reaction > best.Reaction)
                best = pool[i];
        }

        return best;
    }
}