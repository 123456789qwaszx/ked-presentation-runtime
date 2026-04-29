using System;
using UnityEngine;

public sealed class BGHost : MonoBehaviour, IBGViewPrefabProvider
{
    [Serializable]
    public struct BackgroundViewPrefabMapEntry
    {
        public string key;
        public GameObject prefab;
    }
    
    [SerializeField] private BackgroundViewPrefabMapEntry[] prefabMap;
    [SerializeField] private string defaultKey = "default";

    public bool TryGetBackgroundViewPrefab(string key, out GameObject prefab)
    {
        prefab = null;

        if (prefabMap == null || prefabMap.Length == 0)
            return false;

        string resolvedKey = string.IsNullOrWhiteSpace(key) ? defaultKey : key.Trim();

        for (int i = 0; i < prefabMap.Length; i++)
        {
            BackgroundViewPrefabMapEntry entry = prefabMap[i];

            if (string.IsNullOrWhiteSpace(entry.key) || entry.prefab == null)
                continue;

            if (!string.Equals(entry.key.Trim(), resolvedKey, StringComparison.Ordinal))
                continue;

            prefab = entry.prefab;
            return true;
        }

        Debug.LogWarning($"[BGHost] Background view prefab not found. key={resolvedKey}", this);
        return false;
    }
}