using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DialogueBoxViewPrefabMapEntry
{
    public string key;
    public GameObject prefab;
}

public interface IDialogueBoxHost : IDialogueBoxViewPrefabProvider, IDialogueBoxViewResolver
{
    void Register(string dialogueKey, PresentationDialogueBoxView view);
    void Unregister(string dialogueKey, PresentationDialogueBoxView expected = null);
    bool TryGetView(string dialogueKey, out PresentationDialogueBoxView view);
}

public sealed class DialogueBoxHost : MonoBehaviour, IDialogueBoxHost
{
    [Header("Prefab Catalog")]
    [SerializeField] private DialogueBoxViewPrefabMapEntry[] prefabMap;
    [SerializeField] private string defaultPrefabKey = "default";

    [Header("Yarn Auto Route")]
    [SerializeField] private DialogueBoxRouteEntry[] routes;

    private readonly Dictionary<string, PresentationDialogueBoxView> _views = new();

    public bool TryGetDialogueBoxViewPrefab(string key, out GameObject prefab)
    {
        string resolvedKey = Normalize(string.IsNullOrWhiteSpace(key) ? defaultPrefabKey : key);

        for (int i = 0; i < prefabMap.Length; i++)
        {
            DialogueBoxViewPrefabMapEntry entry = prefabMap[i];

            if (Normalize(entry.key) != resolvedKey)
                continue;

            prefab = entry.prefab;
            return true;
        }

        prefab = null;
        return false;
    }

    public void Register(string dialogueKey, PresentationDialogueBoxView view)
    {
        _views[Normalize(dialogueKey)] = view;
    }

    public void Unregister(string dialogueKey, PresentationDialogueBoxView expected = null)
    {
        string key = Normalize(dialogueKey);

        if (expected != null && _views.TryGetValue(key, out PresentationDialogueBoxView current) && current != expected)
            return;

        _views.Remove(key);
    }

    public bool TryGetView(string dialogueKey, out PresentationDialogueBoxView view)
    {
        return _views.TryGetValue(Normalize(dialogueKey), out view) && view != null;
    }

    public IDialogueBoxView Activate(DialogueBoxKind kind)
    {
        HideAll();

        string dialogueKey = ResolveDialogueKey(kind);
        PresentationDialogueBoxView view = _views[Normalize(dialogueKey)];

        view.Validate();
        view.SetVisible(true);

        return view;
    }

    public void HideAll()
    {
        foreach (PresentationDialogueBoxView view in _views.Values)
        {
            if (view != null)
                view.SetVisible(false);
        }
    }

    private string ResolveDialogueKey(DialogueBoxKind kind)
    {
        for (int i = 0; i < routes.Length; i++)
        {
            if (routes[i].kind == kind)
                return routes[i].dialogueKey;
        }

        throw new InvalidOperationException($"[DialogueBoxHost] Route not found. kind={kind}");
    }

    private static string Normalize(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? "main" : key.Trim();
    }
}