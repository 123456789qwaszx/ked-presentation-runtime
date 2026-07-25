using System;
using System.Collections.Generic;

/// <summary>
/// 몬스터별 텍스트 어드벤처 템플릿.
/// 비트를 키로 참조하는 그래프이며, 선형 진행과 분기를 동시에 지원한다.
/// </summary>
public sealed class ServiceScenario
{
    private readonly ServiceBeat[] _beats;
    private readonly Dictionary<string, int> _indexByBeatKey =
        new(StringComparer.OrdinalIgnoreCase);

    public string ScenarioKey { get; }
    public string MonsterId { get; }

    /// <summary>입실 브리핑 노드. 위임 프로토콜 고지와 첫 인상 묘사를 담는다.</summary>
    public string BriefingNodeName { get; }

    /// <summary>템플릿을 정상적으로 마쳤을 때 재생되는 노드.</summary>
    public string CompletionNodeName { get; }

    /// <summary>
    /// 소화할 비트 수의 상한.
    /// 분기 때문에 무한 순환하는 템플릿을 방지하고, 접객 1회의 길이를 일정하게 유지한다.
    /// </summary>
    public int BeatBudget { get; }

    public IReadOnlyList<ServiceBeat> Beats => _beats;

    public ServiceScenario(
        string scenarioKey,
        string monsterId,
        string briefingNodeName,
        string completionNodeName,
        IReadOnlyList<ServiceBeat> beats,
        int beatBudget = 0)
    {
        ScenarioKey = scenarioKey;
        MonsterId = monsterId;
        BriefingNodeName = briefingNodeName;
        CompletionNodeName = completionNodeName;

        if (beats == null)
        {
            _beats = Array.Empty<ServiceBeat>();
            BeatBudget = 0;
            return;
        }

        _beats = new ServiceBeat[beats.Count];

        for (int i = 0; i < beats.Count; i++)
        {
            _beats[i] = beats[i];

            string key = beats[i].BeatKey;

            if (string.IsNullOrWhiteSpace(key) || _indexByBeatKey.ContainsKey(key))
                continue;

            _indexByBeatKey.Add(key, i);
        }

        BeatBudget = beatBudget <= 0 ? _beats.Length : beatBudget;
    }

    public ServiceBeat EntryBeat => _beats.Length > 0 ? _beats[0] : null;

    public bool TryFindBeat(string beatKey, out ServiceBeat beat)
    {
        if (!string.IsNullOrWhiteSpace(beatKey) &&
            _indexByBeatKey.TryGetValue(beatKey, out int index))
        {
            beat = _beats[index];
            return true;
        }

        beat = null;
        return false;
    }

    /// <summary>선형 진행용. 현재 비트의 다음 비트를 반환한다.</summary>
    public bool TryFindNextInOrder(string currentBeatKey, out ServiceBeat beat)
    {
        if (!string.IsNullOrWhiteSpace(currentBeatKey) &&
            _indexByBeatKey.TryGetValue(currentBeatKey, out int index) &&
            index + 1 < _beats.Length)
        {
            beat = _beats[index + 1];
            return true;
        }

        beat = null;
        return false;
    }
}
