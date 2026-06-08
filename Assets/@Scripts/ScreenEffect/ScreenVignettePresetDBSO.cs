using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Vignette Preset DB",
    fileName = "ScreenVignettePresetDB")]
public sealed class ScreenVignettePresetDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ScreenVignettePreset preset;

        [Range(0f, 1f)] public float amount;
        public Color color;
        [Range(0f, 1f)] public float radius;
        [Range(0.001f, 1f)] public float softness;
        [Min(0f)] public float aspect;
    }

    [Serializable]
    public struct LetterBoxConfig
    {
        public Color color;

        [Tooltip("amount=0 (열림). 클수록 바가 안 보인다.")]
        [Range(0f, 1f)] public float radiusOpen;

        [Tooltip("amount=1 (닫힘). 작을수록 바가 안쪽으로 들어온다.")]
        [Range(0f, 1f)] public float radiusClosed;
    }

    [SerializeField] private List<Entry> entries = new();
    [SerializeField] private LetterBoxConfig letterBox = DefaultLetterBox();

    private Dictionary<ScreenVignettePreset, Entry> _map;

    public LetterBoxConfig LetterBox => letterBox;

    public bool TryGet(ScreenVignettePreset preset, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(preset, out entry);
    }

    public static LetterBoxConfig DefaultLetterBox()
    {
        return new LetterBoxConfig
        {
            color = Color.black,
            radiusOpen = 0.52f,
            radiusClosed = 0.23f
        };
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null; // 플레이 모드 인스펙터 수정 → 캐시 무효화 → 다음 발화에 반영.

    private void Build()
    {
        _map = new Dictionary<ScreenVignettePreset, Entry>();

        for (int i = 0; i < entries.Count; i++)
            _map[entries[i].preset] = entries[i];
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            new() { preset = ScreenVignettePreset.DefaultFocus, amount = 0.35f, color = Color.black,                            radius = 0.25f, softness = 0.10f, aspect = 1.2f },
            new() { preset = ScreenVignettePreset.Tension,      amount = 0.55f, color = Color.black,                            radius = 0.15f, softness = 0.22f, aspect = 1.2f },
            new() { preset = ScreenVignettePreset.Horror,       amount = 0.78f, color = new Color(0.02f, 0.015f, 0.018f, 1f),   radius = 0.14f, softness = 0.36f, aspect = 1.2f },
            new() { preset = ScreenVignettePreset.Danger,       amount = 0.58f, color = new Color(0.35f, 0.02f, 0.015f, 1f),    radius = 0.14f, softness = 0.34f, aspect = 1.2f },
            new() { preset = ScreenVignettePreset.Memory,       amount = 0.36f, color = new Color(0.34f, 0.38f, 0.48f, 1f),     radius = 0.10f, softness = 0.36f, aspect = 1.2f },
            new() { preset = ScreenVignettePreset.Dream,        amount = 0.32f, color = new Color(0.38f, 0.32f, 0.52f, 1f),     radius = 0.34f, softness = 0.12f, aspect = 1.2f },
        };

        letterBox = DefaultLetterBox();
    }
}