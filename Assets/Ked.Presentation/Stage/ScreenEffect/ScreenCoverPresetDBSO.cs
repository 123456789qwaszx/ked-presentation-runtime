using System;
using System.Collections.Generic;
using UnityEngine;

// 덮개의 프리셋 — 대본은 이름(black · white …), 색은 여기(docs/PLAN_screen-cover.md §3.1 · D3).
// 같은 식구(플래시 · 비네트 · 노이즈의 프리셋 DB)와 같은 꼴이다.
[CreateAssetMenu(
    menuName = "CPS/ScreenEffect/Cover Preset DB",
    fileName = "ScreenCoverPresetDB")]
public sealed class ScreenCoverPresetDBSO : ScriptableObject
{
    public const string DefaultPresetKey = "black";

    [Serializable]
    public struct Entry
    {
        [Tooltip("Yarn/Command에서 사용할 preset key. ex) black, white")]
        public string key;

        [Tooltip("덮개의 색. 알파는 쓰지 않는다 — 덮기는 늘 다 덮는다.")]
        public Color color;
    }

    [SerializeField] private List<Entry> entries = DefaultEntries();

    private Dictionary<string, Entry> _map;

    public bool TryGet(string key, out Entry entry)
    {
        if (_map == null)
            Build();

        return _map.TryGetValue(NormalizeKey(key), out entry);
    }

    public static string NormalizeKey(string key)
    {
        key = (key ?? "").Trim();

        if (string.IsNullOrEmpty(key))
            return DefaultPresetKey;

        key = key.ToLowerInvariant();
        key = key.Replace(" ", "_");
        key = key.Replace("-", "_");

        return key;
    }

    /// <summary>조립에 에셋이 연결되지 않았을 때 쓰는 코드 기본값(검정 · 흰색).</summary>
    public static ScreenCoverPresetDBSO CreateDefault()
    {
        ScreenCoverPresetDBSO preset = CreateInstance<ScreenCoverPresetDBSO>();
        preset.name = "ScreenCoverPresetDB (코드 기본값)";
        preset.hideFlags = HideFlags.DontSave;
        return preset;
    }

    private void OnEnable() => _map = null;
    private void OnValidate() => _map = null;

    private void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            string key = NormalizeKey(entry.key);

            if (string.IsNullOrEmpty(key))
                continue;

            entry.key = key;
            _map[key] = entry;
        }
    }

    private void Reset()
    {
        entries = DefaultEntries();
        _map = null;
    }

    private static List<Entry> DefaultEntries() => new()
    {
        new() { key = "black", color = Color.black },
        new() { key = "white", color = Color.white },
    };
}
