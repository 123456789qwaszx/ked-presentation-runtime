using UnityEngine;

public interface IDialogueBoxViewPrefabProvider
{
    bool TryGetDialogueBoxViewPrefab(string key, out GameObject prefab);
}