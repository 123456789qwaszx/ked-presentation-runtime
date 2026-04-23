using UnityEngine;

public interface IBGViewPrefabProvider
{
    bool TryGetBackgroundViewPrefab(string key, out GameObject prefab);
}