// 접객 세션 1회분 상태
public sealed class ServiceSessionState
{
    public MaidState Maid { get; }
    public MonsterProfile Monster { get; }
    public SpeciesProtocol Protocol { get; }

    public int BeatIndex { get; set; }
    
    public int DayReactionScore { get; set; }   // 낮 반응 점수 (심층 제외 - 결산 산입분).
    
    public int DepthReactionScore { get; set; } // 심층 중 발생 반응 (미산입, 기록용).
    public OptionIntensity? LastApprovedIntensity { get; set; }

    public bool InDepth { get; set; }
    
    public BurdenAxis DepthAxis { get; set; }           // 심층 진입 기준축 (초과한 축)
    public int DepthBeatCount { get; set; }
    public bool FirstRecoverySuppressed { get; set; }   // 침면 §13.2
    public bool SpecialSealed { get; set; }             // 결과 봉인 §11.2
    public string RemovedActionNode { get; set; }       // 말하지 않은 거절 §11.3
    public bool StopAt199Consumed { get; set; }

    public SettlementOutcomeKind EndKind { get; set; } = SettlementOutcomeKind.Normal;
    
    public AxisTriple AccumulatedRawLoad { get; set; } = AxisTriple.Zero; // 완화 전 원본 부하 누적 (숙련 XP 기준)

    public int Satisfaction => DayReactionScore * 10;

    public ServiceSessionState(MaidState maid, MonsterProfile monster, SpeciesProtocol protocol)
    {
        Maid = maid; 
        Monster = monster; 
        Protocol = protocol;
    }
}
