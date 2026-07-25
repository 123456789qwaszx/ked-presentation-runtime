using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MaidProfileDB",
    menuName = "Guesthouse/Maid Profile DB")]
public sealed class MaidProfileDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Header("Identity")]
        public string maidId;
        public string displayName;

        [Header("Aptitude")]
        [Tooltip("영구 대응력. 부하 완화량과 제안 가능한 행동의 폭을 결정한다.")]
        public AxisTriple aptitude = new(2, 2, 2);

        [Tooltip("축별 붕괴 한계. 도달하면 관리자 통제 신호가 거부된다.")]
        public AxisTriple collapseLimit = AxisTriple.Uniform(100);

        [Header("Behaviour")]
        [Tooltip("제안 문구의 톤 키. 승인 요청 대사 선택에 사용한다.")]
        public string proposalStyleKey;

        public string[] traitKeys = Array.Empty<string>();
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private readonly List<MaidProfile> _profiles = new();

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

            if (entry == null || string.IsNullOrWhiteSpace(entry.maidId))
                continue;

            _profiles.Add(new MaidProfile(
                entry.maidId.Trim(),
                entry.displayName,
                entry.aptitude,
                entry.collapseLimit,
                entry.proposalStyleKey,
                entry.traitKeys));
        }
    }

    public IReadOnlyList<MaidProfile> BuildProfiles()
    {
        if (_profiles.Count != entries.Length)
            RebuildIndex();

        return _profiles;
    }
}
