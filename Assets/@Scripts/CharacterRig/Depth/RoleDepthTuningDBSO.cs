using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CharacterDepthPresetCorrection
{
    [Header("Transform Correction")]
    [Tooltip("Global depthY에 추가로 더할 캐릭터/포즈별 보정값입니다.")]
    public Vector2 yOffsetAdd;

    [Tooltip("Global depthScale에 곱할 캐릭터/포즈별 보정 배율입니다. 0 이하이면 1로 처리됩니다.")]
    public float scaleMultiplier;

    [Tooltip("preserve focus point에 추가로 더할 캐릭터/포즈별 offset입니다.")]
    public Vector2 preserveFocusOffsetAdd;
}

[Serializable]
public struct CharacterDepthCorrectionSet
{
    public CharacterDepthPresetCorrection far;
    public CharacterDepthPresetCorrection mid;
    public CharacterDepthPresetCorrection close;
    public CharacterDepthPresetCorrection front;

    public CharacterDepthPresetCorrection exp1;
    public CharacterDepthPresetCorrection exp2;

    public CharacterDepthPresetCorrection Get(CharacterDepthPreset preset) => preset switch
    {
        CharacterDepthPreset.None => mid,

        CharacterDepthPreset.Far => far,
        CharacterDepthPreset.Mid => mid,
        CharacterDepthPreset.Close => close,
        CharacterDepthPreset.Front => front,

        CharacterDepthPreset.Exp1 => exp1,
        CharacterDepthPreset.Exp2 => exp2,

        _ => mid,
    };
}

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Role Depth Tuning DB", fileName = "RoleDepthTuningDB")]
public sealed class RoleDepthTuningDBSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("예: seina 또는 seina:pose_wide")]
        public string key;

        [Header("Default Correction")]
        [Tooltip("이 캐릭터/포즈의 모든 depth preset에 추가로 적용할 Y 보정값입니다.")]
        public Vector2 defaultYOffsetAdd = Vector2.zero;

        [Tooltip("이 캐릭터/포즈의 모든 depth scale에 곱할 기본 배율입니다.")]
        public float defaultScaleMultiplier = 1f;

        [Header("Preset Corrections")]
        public CharacterDepthCorrectionSet corrections;
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
                Debug.LogWarning($"[RoleDepthTuningDB] Duplicate key: '{key}'", this);
#endif

            _map[key] = entry;
        }
    }
}