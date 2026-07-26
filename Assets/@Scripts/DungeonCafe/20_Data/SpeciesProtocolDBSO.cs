using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpeciesProtocolDB",
    menuName = "Guesthouse/Species Protocol DB")]
public sealed class SpeciesProtocolDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public MonsterSpecies species = MonsterSpecies.ParasiticEquipment;
        public string displayName;

        [Header("Control Loss")]
        public string controlLossNodeName;
        public string collapseEndingNodeName;
        public string collapseEndingKey;

        [Tooltip("통제 상실 이후 철수가 가능한 종족인지 여부. 꺼두면 그 메이드는 이탈한다.")]
        public bool allowsWithdrawAfterControlLoss;

        [Tooltip("자동 사건 1비트당 추가 누적되는 부담.")]
        public AxisTriple autonomousResidualLoad = new(6, 6, 6);

        [Min(1)]
        public int autonomousBeatCount = 2;

        [Header("Codex")]
        [TextArea(1, 4)]
        public string[] riskNotes = Array.Empty<string>();
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private readonly List<SpeciesProtocol> _protocols = new();

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
        _protocols.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];

            if (entry == null || entry.species == MonsterSpecies.None)
                continue;

            _protocols.Add(new SpeciesProtocol(
                entry.species,
                entry.displayName,
                entry.controlLossNodeName,
                entry.collapseEndingNodeName,
                entry.collapseEndingKey,
                entry.allowsWithdrawAfterControlLoss,
                entry.autonomousResidualLoad,
                entry.autonomousBeatCount,
                entry.riskNotes));
        }
    }

    public IReadOnlyList<SpeciesProtocol> BuildProtocols()
    {
        if (_protocols.Count != entries.Length)
            RebuildIndex();

        return _protocols;
    }
}
