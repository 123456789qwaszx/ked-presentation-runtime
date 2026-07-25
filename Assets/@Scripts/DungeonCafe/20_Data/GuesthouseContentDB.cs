using System;
using System.Collections.Generic;

/// <summary>
/// 정의 데이터의 런타임 조회 창구.
/// SO 저작본과 코드 기본 콘텐츠 모두 이 타입으로 수렴시켜, 상위 레이어가 저작 경로를 모르게 한다.
/// </summary>
public sealed class GuesthouseContentDB
{
    private readonly Dictionary<string, MaidProfile> _maidById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, MonsterProfile> _monsterById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ServiceScenario> _scenarioByKey =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<MonsterSpecies, SpeciesProtocol> _protocolBySpecies = new();

    private readonly List<MaidProfile> _maids = new();
    private readonly List<MonsterProfile> _monsters = new();

    public ProgressionTuning Tuning { get; }

    public IReadOnlyList<MaidProfile> Maids => _maids;
    public IReadOnlyList<MonsterProfile> Monsters => _monsters;
    public IReadOnlyDictionary<MonsterSpecies, SpeciesProtocol> ProtocolBySpecies => _protocolBySpecies;

    public GuesthouseContentDB(
        ProgressionTuning tuning,
        IReadOnlyList<MaidProfile> maids,
        IReadOnlyList<MonsterProfile> monsters,
        IReadOnlyList<ServiceScenario> scenarios,
        IReadOnlyList<SpeciesProtocol> protocols)
    {
        Tuning = tuning ?? ProgressionTuning.CreateDefault();

        AddRange(maids, profile => profile.MaidId, _maidById, _maids);
        AddRange(monsters, profile => profile.MonsterId, _monsterById, _monsters);

        if (scenarios != null)
        {
            for (int i = 0; i < scenarios.Count; i++)
            {
                ServiceScenario scenario = scenarios[i];

                if (scenario == null || string.IsNullOrWhiteSpace(scenario.ScenarioKey))
                    continue;

                _scenarioByKey[scenario.ScenarioKey] = scenario;
            }
        }

        if (protocols == null)
            return;

        for (int i = 0; i < protocols.Count; i++)
        {
            SpeciesProtocol protocol = protocols[i];

            if (protocol == null || protocol.Species == MonsterSpecies.None)
                continue;

            _protocolBySpecies[protocol.Species] = protocol;
        }
    }

    public bool TryFindMaid(string maidId, out MaidProfile profile)
        => _maidById.TryGetValue(maidId ?? string.Empty, out profile);

    public bool TryFindMonster(string monsterId, out MonsterProfile profile)
        => _monsterById.TryGetValue(monsterId ?? string.Empty, out profile);

    public bool TryFindScenario(string scenarioKey, out ServiceScenario scenario)
        => _scenarioByKey.TryGetValue(scenarioKey ?? string.Empty, out scenario);

    public bool TryFindProtocol(MonsterSpecies species, out SpeciesProtocol protocol)
        => _protocolBySpecies.TryGetValue(species, out protocol);

    /// <summary>몬스터 정의에 걸린 시나리오를 함께 해석한다.</summary>
    public bool TryFindScenarioForMonster(MonsterProfile monster, out ServiceScenario scenario)
    {
        if (monster == null)
        {
            scenario = null;
            return false;
        }

        return TryFindScenario(monster.ScenarioKey, out scenario);
    }

    private static void AddRange<T>(
        IReadOnlyList<T> source,
        Func<T, string> keySelector,
        Dictionary<string, T> map,
        List<T> ordered)
        where T : class
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            T item = source[i];

            if (item == null)
                continue;

            string key = keySelector(item);

            if (string.IsNullOrWhiteSpace(key) || map.ContainsKey(key))
                continue;

            map.Add(key, item);
            ordered.Add(item);
        }
    }
}
