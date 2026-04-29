using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DialogueBoxHostEntry
{
    public DialogueBoxKind kind;
    public string dialogueKey;
    public GameObject prefab;
}

public sealed class DialogueBoxHost : MonoBehaviour, IDialogueBoxHost
{
    [Header("Dialogue Box Entries")]
    [SerializeField] private DialogueBoxHostEntry[] entries;

    private readonly Dictionary<string, IPresentationDialogueBoxView> _views = new();

    public bool TryGetDialogueBoxViewPrefab(string key, out GameObject prefab)
    {
        EnsureDialogueKey(key);

        for (int i = 0; i < entries.Length; i++)
        {
            DialogueBoxHostEntry entry = entries[i];

            if (entry.dialogueKey != key)
                continue;

            prefab = entry.prefab;
            return prefab != null;
        }

        prefab = null;
        return false;
    }

    public void Register(string dialogueKey, IPresentationDialogueBoxView view)
    {
        EnsureDialogueKey(dialogueKey);
        _views[dialogueKey] = view;
    }

    public void Unregister(string dialogueKey, IPresentationDialogueBoxView expected = null)
    {
        EnsureDialogueKey(dialogueKey);

        if (expected != null &&
            _views.TryGetValue(dialogueKey, out IPresentationDialogueBoxView current) &&
            !ReferenceEquals(current, expected))
        {
            return;
        }

        _views.Remove(dialogueKey);
    }

    public bool TryGetView(string dialogueKey, out IPresentationDialogueBoxView view)
    {
        EnsureDialogueKey(dialogueKey);
        return _views.TryGetValue(dialogueKey, out view) && view != null;
    }

    public IDialogueTextTarget Activate(DialogueBoxKind kind)
    {
        HideAll();

        DialogueBoxHostEntry entry = FindEntry(kind);

        if (!TryGetView(entry.dialogueKey, out IPresentationDialogueBoxView view))
        {
            throw new InvalidOperationException(
                $"[DialogueBoxHost] DialogueBox view is not registered. kind={kind}, dialogueKey={entry.dialogueKey}");
        }

        view.Validate();
        view.SetVisible(true);
        return view;
    }

    public void HideAll()
    {
        foreach (IPresentationDialogueBoxView view in _views.Values)
        {
            if (view == null)
                continue;

            view.SetVisible(false);
        }
    }

    private DialogueBoxHostEntry FindEntry(DialogueBoxKind kind)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            DialogueBoxHostEntry entry = entries[i];

            if (entry.kind != kind)
                continue;

            EnsureDialogueKey(entry.dialogueKey);
            return entry;
        }

        throw new InvalidOperationException($"[DialogueBoxHost] Entry not found. kind={kind}");
    }

    private static void EnsureDialogueKey(string dialogueKey)
    {
        if (string.IsNullOrEmpty(dialogueKey))
            throw new InvalidOperationException("[DialogueBoxHost] dialogueKey is required.");
    }
}