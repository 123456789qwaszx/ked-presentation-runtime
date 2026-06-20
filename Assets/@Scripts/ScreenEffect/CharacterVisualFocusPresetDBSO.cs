using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterVisualFocusPreset
{
    Focus = 0,
    Defocus = 1
}

[CreateAssetMenu(
    menuName = "CPS/CharRig/Visual Focus Preset DB",
    fileName = "CharacterVisualFocusPresetDB")]
public sealed class CharacterVisualFocusPresetDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public CharacterVisualFocusPreset preset;

        [Header("Dim")]
        [Range(0f, 1f)] public float dim;
        public Color dimTintColor;

        [Header("Rim")]
        [Range(0f, 1f)] public float outerRim;
        [Range(0f, 1f)] public float innerRim;
        public Color outerRimColor;
        public Color innerRimColor;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<CharacterVisualFocusPreset, Entry> _map;

    public bool TryGet(CharacterVisualFocusPreset preset, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(preset, out entry);
    }

    public static Entry DefaultFocus()
    {
        return new Entry
        {
            preset = CharacterVisualFocusPreset.Focus,

            dim = 0f,
            dimTintColor = new Color(0.45f, 0.48f, 0.55f, 1f),

            outerRim = 0.4f,
            innerRim = 0.09f,

            outerRimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
        };
    }

    public static Entry DefaultDefocus()
    {
        return new Entry
        {
            preset = CharacterVisualFocusPreset.Defocus,

            dim = 0.45f,
            dimTintColor = new Color(0.45f, 0.48f, 0.55f, 1f),

            outerRim = 0f,
            innerRim = 0f,

            outerRimColor = Color.white,
            innerRimColor = new Color(1f, 0.96f, 0.86f, 1f),
        };
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null; // 플레이 모드 인스펙터 수정 → 캐시 무효화 → 다음 발화에 반영.

    private void Build()
    {
        _map = new Dictionary<CharacterVisualFocusPreset, Entry>();

        for (int i = 0; i < entries.Count; i++)
            _map[entries[i].preset] = entries[i];
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            DefaultFocus(),
            DefaultDefocus()
        };
    }
}