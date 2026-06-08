using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Flash Preset DB",
    fileName = "ScreenFlashPresetDB")]
public sealed class ScreenFlashPresetDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ScreenFlashPreset preset;

        public Color color;
        [Range(0f, 1f)] public float amount;
        [Min(0f)] public float attackDuration;
        [Min(0f)] public float holdDuration;
        [Min(0f)] public float releaseDuration;
        public Ease attackEase;
        public Ease releaseEase;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<ScreenFlashPreset, Entry> _map;

    public bool TryGet(ScreenFlashPreset preset, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(preset, out entry);
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void Build()
    {
        _map = new Dictionary<ScreenFlashPreset, Entry>();

        for (int i = 0; i < entries.Count; i++)
            _map[entries[i].preset] = entries[i];
    }

    private void Reset()
    {
        entries = new List<Entry>
        {
            new() { preset = ScreenFlashPreset.Default, color = Color.white,                      amount = 1f,    attackDuration = 0.02f,  holdDuration = 0.01f,  releaseDuration = 0.16f, attackEase = Ease.OutCubic, releaseEase = Ease.OutCubic },
            new() { preset = ScreenFlashPreset.Soft,    color = Color.white,                      amount = 0.5f,  attackDuration = 0.06f,  holdDuration = 0.02f,  releaseDuration = 0.30f, attackEase = Ease.OutCubic, releaseEase = Ease.OutCubic },
            new() { preset = ScreenFlashPreset.Hit,     color = new Color(1f, 0.16f, 0.10f, 1f),  amount = 0.45f, attackDuration = 0.015f, holdDuration = 0.015f, releaseDuration = 0.18f, attackEase = Ease.OutCubic, releaseEase = Ease.OutCubic },
            new() { preset = ScreenFlashPreset.Camera,  color = Color.white,                      amount = 1f,    attackDuration = 0.01f,  holdDuration = 0f,     releaseDuration = 0.10f, attackEase = Ease.OutQuad,  releaseEase = Ease.OutCubic },
        };
    }
}