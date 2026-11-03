using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DialogueSpeakerPresentationPolicyDB",
    menuName = "VN/Dialogue/Speaker Presentation Policy DB")]
public sealed class DialogueSpeakerPresentationPolicyDBSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Header("Identity")]
        public string speakerKey;

        [Header("Display")]
        public string displayNameKey;
        public string fallbackDisplayName;

        [Header("Box Override")]
        public bool useBoxKindOverride;
        public DialogueBoxKind boxKind;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private readonly Dictionary<string, Entry> _entryBySpeakerKey =
        new(StringComparer.OrdinalIgnoreCase);

    private void OnEnable()
    {
        RebuildIndex();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildIndex();
    }
#endif

    private void RebuildIndex()
    {
        _entryBySpeakerKey.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            string key = NormalizeKey(entries[i].speakerKey);

            if (string.IsNullOrEmpty(key))
                continue;

            _entryBySpeakerKey[key] = entries[i];
        }
    }

    public bool TryFind(string speakerName, out Entry entry)
    {
        return _entryBySpeakerKey.TryGetValue(
            NormalizeKey(speakerName),
            out entry);
    }

    public string ResolveDisplayName(
        string speakerName,
        Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.fallbackDisplayName))
            return entry.fallbackDisplayName;

        return speakerName;
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim();
    }
}