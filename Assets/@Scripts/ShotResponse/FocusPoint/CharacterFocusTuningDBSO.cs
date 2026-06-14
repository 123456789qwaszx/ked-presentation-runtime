using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/CharRig/Tuning/Character Focus Tuning DB",
    fileName = "CharacterFocusTuningDB")]
public sealed class CharacterFocusTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("예: leafia 또는 leafia:pose_wide")]
        public string key;

        [Header("Default Offset")]
        [Tooltip("이 캐릭터/포즈에 항상 적용되는 기본 focus 보정값입니다.")]
        public Vector2 defaultOffset = Vector2.zero;

        [Header("Preset Offsets")]
        [Tooltip("Feet/Body/Bust/Face 등 프리셋별 캐릭터 보정값입니다.")]
        public CharacterFocusOffsetSet offsets;
    }

    [Header("Global Base Offsets")]
    [Tooltip("캐릭터별 보정이 적용되기 전의 기본 focus preset 위치입니다.")]
    public CharacterFocusOffsetSet baseOffsets = CharacterFocusOffsetSet.Default;

    [Header("Character / Pose Entries")]
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

    private void OnEnable()
    {
        _map = null;
    }

    private void OnValidate()
    {
        _map = null;
    }

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
                Debug.LogWarning($"[CharacterFocusTuningDB] Duplicate key: '{key}'", this);
#endif

            _map[key] = entry;
        }
    }
}