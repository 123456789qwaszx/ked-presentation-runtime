using System.Collections.Generic;

/// <summary>접객 세션 1회분 상태 v3.</summary>
public sealed class ServiceSessionState
{
    public MaidState Maid { get; }
    public MonsterProfile Monster { get; }
    public SpeciesProtocol Protocol { get; }

    public int BeatIndex { get; set; }
    /// <summary>낮 반응 점수 (심층 제외 - 결산 산입분). (§7.2)</summary>
    public int DayReactionScore { get; set; }
    /// <summary>심층 중 발생 반응 (미산입, 기록용).</summary>
    public int DepthReactionScore { get; set; }
    public OptionIntensity? LastApprovedIntensity { get; set; }

    public bool InDepth { get; set; }
    /// <summary>심층 진입 기준축 (초과한 축). (§3)</summary>
    public BurdenAxis DepthAxis { get; set; }
    public int DepthBeatCount { get; set; }
    public bool FirstRecoverySuppressed { get; set; }   // 침면 §13.2
    public bool SpecialSealed { get; set; }             // 결과 봉인 §11.2
    public string RemovedActionNode { get; set; }       // 말하지 않은 거절 §11.3
    public bool StopAt199Consumed { get; set; }

    public SettlementOutcomeKind EndKind { get; set; } = SettlementOutcomeKind.Normal;
    /// <summary>완화 전 원본 부하 누적 (숙련 XP 기준). (§12.3)</summary>
    public AxisTriple AccumulatedRawLoad { get; set; } = AxisTriple.Zero;

    public int Satisfaction => DayReactionScore * 10;

    public ServiceSessionState(MaidState maid, MonsterProfile monster, SpeciesProtocol protocol)
    {
        Maid = maid; Monster = monster; Protocol = protocol;
    }
}
