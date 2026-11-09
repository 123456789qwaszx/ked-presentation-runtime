using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Role Anchor Tuning DB", fileName = "RoleAnchorTuningDB")]
public sealed class RoleAnchorTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("캐릭터 키(예: seina). 대소문자는 가리지 않는다. 변형은 키에 들어가지 않는다 — 캐릭터마다 항목 하나.")]
        public string key;
        
        [Header("Anchor Offsets")]
        public Vector2 offset;

        [Tooltip("프리셋별 캐릭터 추가 배율")]
        public float visualScale = 1f;
    }

    public List<Entry> entries = new();

    private Dictionary<string, Entry> _map;

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        if (string.IsNullOrWhiteSpace(key))
        {
            entry = null;
            return false;
        }

        return _map.TryGetValue(key.Trim(), out entry) && entry != null;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            string key = entry.key.Trim();

#if UNITY_EDITOR
            if (_map.TryGetValue(key, out Entry prev) && prev != null && !ReferenceEquals(prev, entry))
                Debug.LogWarning($"[RoleAnchorTuningDB] Duplicate key: '{key}'", this);
#endif

            _map[key] = entry;
        }
    }
}