using System;
using System.Collections.Generic;

/// <summary>
/// 몬스터 1개체의 정의.
/// 종족은 통제 상실 이후의 결말을, 개체는 진행 과정과 요구를 담당한다.
/// </summary>
public sealed class MonsterProfile
{
    private static readonly string[] EmptyNotes = Array.Empty<string>();

    public string MonsterId { get; }
    public string DisplayName { get; }
    public MonsterSpecies Species { get; }

    /// <summary>
    /// 이 몬스터가 요구하는 붕괴 유형.
    /// 결산 배율은 접객 종료 시점의 이 축 붕괴도로 결정된다.
    /// </summary>
    public BurdenAxis DemandAxis { get; }

    /// <summary>부하 성향. 배정 화면과 업무 수첩의 상성 표시에 사용한다.</summary>
    public AxisTriple LoadBias { get; }

    /// <summary>이 예약을 성공으로 인정하기 위해 필요한 최소 만족도.</summary>
    public int RequiredSatisfaction { get; }

    public int MaxSatisfaction { get; }

    /// <summary>반응 점수 1점당 차오르는 만족도.</summary>
    public int SatisfactionPerScore { get; }

    public string ScenarioKey { get; }

    /// <summary>게시판에 노출되는 예약 문의 본문. 이 단계에서는 종족과 겉모습만 드러난다.</summary>
    public string ReservationPostText { get; }

    /// <summary>예약 확정 통화 노드. 확정 시 업무 수첩에 대응 타입이 기재된다.</summary>
    public string PhoneCallNodeName { get; }

    /// <summary>업무 수첩 개체 항목.</summary>
    public IReadOnlyList<string> CodexNotes { get; }

    public MonsterProfile(
        string monsterId,
        string displayName,
        MonsterSpecies species,
        BurdenAxis demandAxis,
        AxisTriple loadBias,
        int requiredSatisfaction,
        int maxSatisfaction,
        int satisfactionPerScore,
        string scenarioKey,
        string reservationPostText,
        string phoneCallNodeName,
        IReadOnlyList<string> codexNotes)
    {
        MonsterId = monsterId;
        DisplayName = displayName;
        Species = species;
        DemandAxis = demandAxis;
        LoadBias = loadBias;
        RequiredSatisfaction = requiredSatisfaction;
        MaxSatisfaction = maxSatisfaction <= 0 ? 100 : maxSatisfaction;
        SatisfactionPerScore = satisfactionPerScore <= 0 ? 10 : satisfactionPerScore;
        ScenarioKey = scenarioKey;
        ReservationPostText = reservationPostText;
        PhoneCallNodeName = phoneCallNodeName;
        CodexNotes = codexNotes ?? EmptyNotes;
    }
}
