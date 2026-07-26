/// <summary>
/// 메이드가 관리자에게 승인을 요청하는 행동 하나.
///
/// 이 타입은 세 가지를 동시에 정의한다.
///  - 승인 시 재생될 연출 노드
///  - 메이드가 받는 축별 부하
///  - 몬스터의 반응 등급(= 결산 기본 점수)
/// 제안 후보 추림은 ServiceOptionSelector 가 담당한다.
/// </summary>
public sealed class ServiceActionOption
{
    public string OptionKey { get; }

    /// <summary>승인 버튼에 표시될 제안 문구.</summary>
    public string ProposalText { get; }

    /// <summary>승인 시 재생되는 Yarn 노드.</summary>
    public string ApprovalNodeName { get; }

    /// <summary>메이드가 받는 원본 부하. 대응력에 의해 완화된 뒤 붕괴도에 누적된다.</summary>
    public AxisTriple Load { get; }

    public MonsterReactionGrade Reaction { get; }

    /// <summary>반응 등급으로 환산된 만족도 외에 추가로 부여할 만족도.</summary>
    public int SatisfactionBonus { get; }

    /// <summary>
    /// 이 행동을 승인했을 때 이어지는 비트 키.
    /// 비어 있으면 시나리오의 선형 순서를 따른다.
    /// (예: '검을 꺼낸다' → 검 사용 비트로 분기)
    /// </summary>
    public string NextBeatKey { get; }

    /// <summary>이 행동을 안정적으로 수행하기 위해 요구되는 대응력 축.</summary>
    public BurdenAxis RequiredAptitudeAxis { get; }

    /// <summary>요구 대응력. 미달이어도 제안될 수 있지만 위험 후보로 분류된다.</summary>
    public int RequiredAptitude { get; }

    /// <summary>이 성향을 가진 메이드가 우선적으로 제안한다.</summary>
    public string PreferredTraitKey { get; }

    /// <summary>업무 수첩과 승인 UI의 위험 표시용 가중치.</summary>
    public int RiskWeight { get; }

    /// <summary>승인 즉시 시나리오를 종료시키는 행동인지 여부(철수, 종결 응대 등).</summary>
    public bool IsTerminalAction { get; }

    public ServiceActionOption(
        string optionKey,
        string proposalText,
        string approvalNodeName,
        AxisTriple load,
        MonsterReactionGrade reaction,
        int satisfactionBonus = 0,
        string nextBeatKey = null,
        BurdenAxis requiredAptitudeAxis = BurdenAxis.Physical,
        int requiredAptitude = 0,
        string preferredTraitKey = null,
        int riskWeight = 0,
        bool isTerminalAction = false)
    {
        OptionKey = optionKey;
        ProposalText = proposalText;
        ApprovalNodeName = approvalNodeName;
        Load = load;
        Reaction = reaction;
        SatisfactionBonus = satisfactionBonus;
        NextBeatKey = nextBeatKey;
        RequiredAptitudeAxis = requiredAptitudeAxis;
        RequiredAptitude = requiredAptitude;
        PreferredTraitKey = preferredTraitKey;
        RiskWeight = riskWeight;
        IsTerminalAction = isTerminalAction;
    }

    public bool HasExplicitNextBeat => !string.IsNullOrWhiteSpace(NextBeatKey);
}
