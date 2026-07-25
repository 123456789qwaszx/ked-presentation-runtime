using System;
using System.Collections.Generic;

/// <summary>
/// 격리실 안의 한 상황. 상황 노드 + 행동 후보 풀로 구성된다.
/// 후보 풀은 3개 이상 담을 수 있으며, 실제 제안은 메이드 능력치/성향으로 추려진다.
/// </summary>
public sealed class ServiceBeat
{
    public const int DefaultOfferCount = 3;

    private readonly ServiceActionOption[] _optionPool;

    public string BeatKey { get; }

    /// <summary>승인 요청 전에 재생되는 상황 노드.</summary>
    public string SituationNodeName { get; }

    /// <summary>이 비트를 소화하면 시나리오가 종료되는지 여부.</summary>
    public bool IsTerminal { get; }

    /// <summary>이번 비트에서 메이드가 제안할 행동 수.</summary>
    public int OfferCount { get; }

    public IReadOnlyList<ServiceActionOption> OptionPool => _optionPool;

    public ServiceBeat(
        string beatKey,
        string situationNodeName,
        IReadOnlyList<ServiceActionOption> optionPool,
        bool isTerminal = false,
        int offerCount = DefaultOfferCount)
    {
        BeatKey = beatKey;
        SituationNodeName = situationNodeName;
        IsTerminal = isTerminal;
        OfferCount = offerCount <= 0 ? DefaultOfferCount : offerCount;

        if (optionPool == null)
        {
            _optionPool = Array.Empty<ServiceActionOption>();
            return;
        }

        _optionPool = new ServiceActionOption[optionPool.Count];

        for (int i = 0; i < optionPool.Count; i++)
            _optionPool[i] = optionPool[i];
    }

    public bool TryFindOption(string optionKey, out ServiceActionOption option)
    {
        for (int i = 0; i < _optionPool.Length; i++)
        {
            if (!string.Equals(_optionPool[i].OptionKey, optionKey, StringComparison.OrdinalIgnoreCase))
                continue;

            option = _optionPool[i];
            return true;
        }

        option = null;
        return false;
    }
}
