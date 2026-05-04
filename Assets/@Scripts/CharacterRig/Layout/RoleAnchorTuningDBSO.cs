using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Role Anchor Tuning DB", fileName = "RoleAnchorTuningDB")]
public sealed class RoleAnchorTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("예: seina 또는 seina:pose_wide")]
        public string key;

        [Header("Default Offset")]
        [Tooltip("이 캐릭터/포즈에 항상 적용되는 기본 위치 보정값입니다.")]
        public Vector2 defaultOffset = Vector2.zero;
        
        [Header("Anchor Offsets")]
        public CharPlacementTuningSet offsets;

        [Header("Scale")]
        [Tooltip("캐릭터/리소스 정규화용 기본 스케일")]
        public float defaultScale = 1f;

        [Tooltip("프리셋별 캐릭터 추가 배율")]
        public CharScaleTuningSet scales = CharScaleTuningSet.Default;
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
        _map = new Dictionary<string, Entry>(StringComparer.Ordinal);

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