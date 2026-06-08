using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Noise Preset DB",
    fileName = "ScreenNoisePresetDB")]
public sealed class ScreenNoisePresetDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ScreenNoisePreset preset;

        [Range(0f, 1f)] public float amount;
        public Color color;
        [Min(0f)] public float scale;
        public float speedX;
        public float speedY;
        [Min(0f)] public float contrast;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<ScreenNoisePreset, Entry> _map;

    public bool TryGet(ScreenNoisePreset preset, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(preset, out entry);
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null; // 플레이 모드 인스펙터 수정 → 캐시 무효화 → 다음 발화에 반영.

    private void Build()
    {
        _map = new Dictionary<ScreenNoisePreset, Entry>();

        for (int i = 0; i < entries.Count; i++)
            _map[entries[i].preset] = entries[i];
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            new() { preset = ScreenNoisePreset.Default,   amount = 1f,    color = Color.white,                     scale = 0.8f,  speedX = 0.015f, speedY = 0.012f, contrast = 1f },
            new() { preset = ScreenNoisePreset.Memory,    amount = 0.32f, color = new Color(0.72f, 0.78f, 0.90f),  scale = 0.85f, speedX = 0.010f, speedY = 0.008f, contrast = 1.15f },
            new() { preset = ScreenNoisePreset.Horror,    amount = 0.55f, color = new Color(0.62f, 0.62f, 0.66f),  scale = 1.25f, speedX = 0.030f, speedY = 0.045f, contrast = 1.8f },
            new() { preset = ScreenNoisePreset.Broadcast, amount = 0.50f, color = new Color(0.80f, 0.90f, 1f),     scale = 1.8f,  speedX = 0.080f, speedY = 0.012f, contrast = 2.2f },
            new() { preset = ScreenNoisePreset.Dream,     amount = 0.22f, color = new Color(0.76f, 0.70f, 0.95f),  scale = 0.65f, speedX = 0.006f, speedY = 0.010f, contrast = 0.9f },
            new() { preset = ScreenNoisePreset.RainMood,  amount = 0.28f, color = new Color(0.70f, 0.78f, 0.90f),  scale = 1.0f,  speedX = 0.012f, speedY = 0.035f, contrast = 1.1f },
        };
    }
}