using System;
using System.Collections.Generic;

/// <summary>
/// 종족 단위 대응 규약.
/// 업무 수첩의 위험 경고와, 통제 상실 이후 수렴하는 파멸 방향을 정의한다.
/// 같은 종족의 몬스터는 서로 다른 과정을 갖지만 한계 초과 이후는 이 규약으로 수렴한다.
/// </summary>
public sealed class SpeciesProtocol
{
    private static readonly string[] EmptyNotes = Array.Empty<string>();

    public MonsterSpecies Species { get; }
    public string DisplayName { get; }

    /// <summary>업무 수첩 종족 페이지에 기재되는 위험 항목.</summary>
    public IReadOnlyList<string> RiskNotes { get; }

    /// <summary>통제 신호 거부 직후 재생되는 노드.</summary>
    public string ControlLossNodeName { get; }

    /// <summary>자동 사건이 끝난 뒤 재생되는 종족 배드엔딩 노드.</summary>
    public string CollapseEndingNodeName { get; }

    public string CollapseEndingKey { get; }

    /// <summary>통제 상실 이후 즉시 철수가 가능한 종족인지 여부.</summary>
    public bool AllowsWithdrawAfterControlLoss { get; }

    /// <summary>자동 사건 1비트당 추가로 누적되는 부담.</summary>
    public AxisTriple AutonomousResidualLoad { get; }

    /// <summary>통제 상실 이후 자동으로 진행되는 비트 수.</summary>
    public int AutonomousBeatCount { get; }

    public SpeciesProtocol(
        MonsterSpecies species,
        string displayName,
        string controlLossNodeName,
        string collapseEndingNodeName,
        string collapseEndingKey,
        bool allowsWithdrawAfterControlLoss,
        AxisTriple autonomousResidualLoad,
        int autonomousBeatCount,
        IReadOnlyList<string> riskNotes)
    {
        Species = species;
        DisplayName = displayName;
        ControlLossNodeName = controlLossNodeName;
        CollapseEndingNodeName = collapseEndingNodeName;
        CollapseEndingKey = collapseEndingKey;
        AllowsWithdrawAfterControlLoss = allowsWithdrawAfterControlLoss;
        AutonomousResidualLoad = autonomousResidualLoad;
        AutonomousBeatCount = autonomousBeatCount <= 0 ? 1 : autonomousBeatCount;
        RiskNotes = riskNotes ?? EmptyNotes;
    }
}
