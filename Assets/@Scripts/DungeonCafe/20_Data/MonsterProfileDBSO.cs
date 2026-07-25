using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MonsterProfileDB",
    menuName = "Guesthouse/Monster Profile DB")]
public sealed class MonsterProfileDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Header("Identity")]
        public string monsterId;
        public string displayName;
        public MonsterSpecies species = MonsterSpecies.ParasiticEquipment;

        [Header("Demand")]
        [Tooltip("결산 배율을 결정하는 붕괴 축.")]
        public BurdenAxis demandAxis = BurdenAxis.Physical;

        [Tooltip("부하 성향. 배정 화면과 업무 수첩의 상성 표시에 쓴다.")]
        public AxisTriple loadBias;

        [Header("Satisfaction")]
        public int requiredSatisfaction = 60;
        public int maxSatisfaction = 100;

        [Tooltip("반응 점수 1점당 차오르는 만족도.")]
        public int satisfactionPerScore = 10;

        [Header("Content")]
        public string scenarioKey;

        [TextArea(2, 5)]
        public string reservationPostText;

        public string phoneCallNodeName;

        [TextArea(1, 4)]
        public string[] codexNotes = Array.Empty<string>();
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private readonly List<MonsterProfile> _profiles = new();

    private void OnEnable()
    {
        RebuildIndex();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildIndex();
    }
#endif

    private void RebuildIndex()
    {
        _profiles.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.monsterId))
                continue;

            _profiles.Add(new MonsterProfile(
                entry.monsterId.Trim(),
                entry.displayName,
                entry.species,
                entry.demandAxis,
                entry.loadBias,
                entry.requiredSatisfaction,
                entry.maxSatisfaction,
                entry.satisfactionPerScore,
                entry.scenarioKey,
                entry.reservationPostText,
                entry.phoneCallNodeName,
                entry.codexNotes));
        }
    }

    public IReadOnlyList<MonsterProfile> BuildProfiles()
    {
        if (_profiles.Count != entries.Length)
            RebuildIndex();

        return _profiles;
    }
}
