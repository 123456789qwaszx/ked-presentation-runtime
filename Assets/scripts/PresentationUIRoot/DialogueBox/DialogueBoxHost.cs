using System;
using UnityEngine;

[Serializable]
public struct DialogueBoxViewPrefabMapEntry
{
    public string key;
    public GameObject prefab;
}

public sealed class DialogueBoxHost : MonoBehaviour, IDialogueBoxViewPrefabProvider
{
    [SerializeField] private DialogueBoxViewPrefabMapEntry[] prefabMap;
    [SerializeField] private string defaultKey = "default";

    public bool TryGetDialogueBoxViewPrefab(string key, out GameObject prefab)
    {
        prefab = null;

        if (prefabMap == null || prefabMap.Length == 0)
            return false;

        string resolvedKey = string.IsNullOrWhiteSpace(key) ? defaultKey : key.Trim();

        for (int i = 0; i < prefabMap.Length; i++)
        {
            DialogueBoxViewPrefabMapEntry entry = prefabMap[i];

            if (string.IsNullOrWhiteSpace(entry.key) || entry.prefab == null)
                continue;

            if (!string.Equals(entry.key.Trim(), resolvedKey, StringComparison.Ordinal))
                continue;

            prefab = entry.prefab;
            return true;
        }

        Debug.LogWarning($"[DialogueBoxHost] DialogueBox view prefab not found. key={resolvedKey}", this);
        return false;
    }
}