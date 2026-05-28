using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "VN/Episode/Chapter Episode Progression Catalog",
    fileName = "ChapterEpisodeProgressionCatalog")]
public sealed class ChapterEpisodeProgressionCatalogSO : ScriptableObject
{
    [Serializable]
    private sealed class Entry
    {
        [Tooltip("ChapterButtonCard.ChapterId와 매칭되는 값")]
        public string ChapterButtonId;

        [Tooltip("이 버튼이 열어야 하는 챕터 진행 데이터")]
        public ChapterEpisodeProgressionSO Progression;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private Dictionary<string, ChapterEpisodeProgressionSO> _cachedMap;
    private bool _cacheBuilt;

    public bool TryGetProgression(
        string chapterButtonId,
        out ChapterEpisodeProgressionSO progression)
    {
        EnsureCache();

        return _cachedMap.TryGetValue(chapterButtonId, out progression) &&
               progression != null;
    }

    public IEnumerable<KeyValuePair<string, ChapterEpisodeProgressionSO>> EnumerateProgressions()
    {
        EnsureCache();

        foreach (KeyValuePair<string, ChapterEpisodeProgressionSO> pair in _cachedMap)
        {
            if (pair.Value == null)
                continue;

            yield return pair;
        }
    }

    private void EnsureCache()
    {
        if (_cacheBuilt && _cachedMap != null)
            return;

        RebuildCache();
    }

    private void RebuildCache()
    {
        _cachedMap = new Dictionary<string, ChapterEpisodeProgressionSO>();
        _cacheBuilt = true;

        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];

            if (entry == null)
                continue;

            if (entry.Progression == null)
                continue;

            _cachedMap[entry.ChapterButtonId] = entry.Progression;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _cacheBuilt = false;
    }
#endif
}