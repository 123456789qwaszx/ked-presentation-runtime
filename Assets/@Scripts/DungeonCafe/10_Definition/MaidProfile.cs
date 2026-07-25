using System;
using System.Collections.Generic;

/// <summary>
/// 메이드의 영구 정의. 런타임 중 변하지 않는다.
/// 누적 부담과 숙련도는 MaidRuntimeState 가 보유한다.
/// </summary>
public sealed class MaidProfile
{
    private static readonly string[] EmptyTraits = Array.Empty<string>();

    /// <summary>영구 대응력. 몬스터가 가한 부하를 완화하고 제안 가능한 행동의 폭을 결정한다.</summary>
    public AxisTriple Aptitude { get; }

    /// <summary>축별 붕괴 한계. 이 값을 넘으면 관리자 통제 신호가 거부된다.</summary>
    public AxisTriple CollapseLimit { get; }

    public string MaidId { get; }
    public string DisplayName { get; }

    /// <summary>제안 문구의 톤. 같은 행동이라도 어떤 태도로 승인을 요청하는지 결정한다.</summary>
    public string ProposalStyleKey { get; }

    /// <summary>성향 태그. 행동 후보 가중치와 야간 이벤트 분기에 사용한다.</summary>
    public IReadOnlyList<string> TraitKeys { get; }

    public MaidProfile(
        string maidId,
        string displayName,
        AxisTriple aptitude,
        AxisTriple collapseLimit,
        string proposalStyleKey,
        IReadOnlyList<string> traitKeys)
    {
        MaidId = maidId;
        DisplayName = displayName;
        Aptitude = aptitude;
        CollapseLimit = collapseLimit;
        ProposalStyleKey = proposalStyleKey;
        TraitKeys = traitKeys ?? EmptyTraits;
    }

    public bool HasTrait(string traitKey)
    {
        if (string.IsNullOrWhiteSpace(traitKey))
            return false;

        for (int i = 0; i < TraitKeys.Count; i++)
        {
            if (string.Equals(TraitKeys[i], traitKey, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
