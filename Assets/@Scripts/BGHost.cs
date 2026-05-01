using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IBGRuntimeRegistry
{
    void RegisterRuntimeBackground(string bgKey, PresentationBackgroundView view);
    void UnregisterRuntimeBackground(string bgKey, PresentationBackgroundView expected = null);
    void DestroyRuntimeBackground(string bgKey);
    void ClearRuntimeBackgrounds();
}

public sealed class BGHost : MonoBehaviour, IBGViewPrefabProvider, IBGRuntimeRegistry
{
    [Serializable]
    public struct BackgroundViewPrefabMapEntry
    {
        public string key;
        public GameObject prefab;
    }

    [SerializeField] private BackgroundViewPrefabMapEntry[] prefabMap;
    [SerializeField] private string defaultKey = "default";

    private readonly Dictionary<string, PresentationBackgroundView> _runtimeViews = new();

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

    public void RegisterRuntimeBackground(string bgKey, PresentationBackgroundView view)
    {
        string key = NormalizeRuntimeKey(bgKey);

        if (view == null)
            return;

        _runtimeViews[key] = view;
    }

    public void UnregisterRuntimeBackground(string bgKey, PresentationBackgroundView expected = null)
    {
        string key = NormalizeRuntimeKey(bgKey);

        if (!_runtimeViews.TryGetValue(key, out PresentationBackgroundView current))
            return;

        if (expected != null && !ReferenceEquals(current, expected))
            return;

        _runtimeViews.Remove(key);
    }
    
    public void DestroyRuntimeBackground(string bgKey)
    {
        string key = NormalizeRuntimeKey(bgKey);

        if (!_runtimeViews.TryGetValue(key, out PresentationBackgroundView view))
            return;

        _runtimeViews.Remove(key);

        if (view == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(view.gameObject);
        else
#endif
            Object.Destroy(view.gameObject);
    }
    
    public void ClearRuntimeBackgrounds()
    {
        foreach (PresentationBackgroundView view in _runtimeViews.Values)
        {
            if (view == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(view.gameObject);
            else
#endif
                Object.Destroy(view.gameObject);
        }

        _runtimeViews.Clear();
    }
    
    

    private static string NormalizeRuntimeKey(string bgKey)
    {
        return string.IsNullOrWhiteSpace(bgKey)
            ? "current"
            : bgKey.Trim();
    }
}