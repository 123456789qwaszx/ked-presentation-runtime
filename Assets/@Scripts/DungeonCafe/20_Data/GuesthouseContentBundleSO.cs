using UnityEngine;

/// <summary>
/// 저작 에셋을 하나로 묶어 GuesthouseContentDB 를 만든다.
/// 씬/부트스트랩은 이 에셋 하나만 참조한다.
/// 비어 있는 슬롯은 코드 기본 콘텐츠(GuesthouseDemoContent)로 대체된다.
/// </summary>
[CreateAssetMenu(
    fileName = "GuesthouseContentBundle",
    menuName = "Guesthouse/Content Bundle")]
public sealed class GuesthouseContentBundleSO : ScriptableObject
{
    [SerializeField] private ProgressionTuningSO tuning;
    [SerializeField] private MaidProfileDBSO maidDb;
    [SerializeField] private MonsterProfileDBSO monsterDb;
    [SerializeField] private ServiceScenarioDBSO scenarioDb;
    [SerializeField] private SpeciesProtocolDBSO speciesDb;

    [Header("Fallback")]
    [SerializeField]
    [Tooltip("슬롯이 비어 있을 때 코드에 내장된 버티컬 슬라이스 콘텐츠로 대체한다.")]
    private bool useDemoContentFallback = true;

    public GuesthouseContentDB BuildContentDB()
    {
        GuesthouseContentDB fallback = useDemoContentFallback
            ? GuesthouseDemoContent.Build()
            : null;

        return new GuesthouseContentDB(
            tuning != null ? tuning.BuildTuning() : fallback?.Tuning,
            maidDb != null ? maidDb.BuildProfiles() : fallback?.Maids,
            monsterDb != null ? monsterDb.BuildProfiles() : fallback?.Monsters,
            scenarioDb != null ? scenarioDb.BuildScenarios() : CollectFallbackScenarios(fallback),
            speciesDb != null ? speciesDb.BuildProtocols() : CollectFallbackProtocols(fallback));
    }

    private static System.Collections.Generic.IReadOnlyList<ServiceScenario> CollectFallbackScenarios(
        GuesthouseContentDB fallback)
    {
        if (fallback == null)
            return null;

        System.Collections.Generic.List<ServiceScenario> scenarios = new();

        for (int i = 0; i < fallback.Monsters.Count; i++)
        {
            if (fallback.TryFindScenarioForMonster(fallback.Monsters[i], out ServiceScenario scenario))
                scenarios.Add(scenario);
        }

        return scenarios;
    }

    private static System.Collections.Generic.IReadOnlyList<SpeciesProtocol> CollectFallbackProtocols(
        GuesthouseContentDB fallback)
    {
        if (fallback == null)
            return null;

        System.Collections.Generic.List<SpeciesProtocol> protocols = new();

        foreach (SpeciesProtocol protocol in fallback.ProtocolBySpecies.Values)
            protocols.Add(protocol);

        return protocols;
    }
}
