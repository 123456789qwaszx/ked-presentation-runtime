using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public struct DialogueBoxHostEntry
{
    public DialogueBoxKind kind;
    public string dialogueKey;
    public GameObject prefab;
}

public sealed class DialogueBoxHost : MonoBehaviour, IDialogueBoxViewResolver
{
    private PresentationSessionContext _context;

    public void Initialize(PresentationSessionContext context)
    {
        _context = context;
    }
    
    [Header("Root")]
    [SerializeField] private RectTransform dialogueBoxRoot;

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

        if (view == null)
            throw new InvalidOperationException("[DialogueBoxHost] Cannot register null view.");

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

    public IDialogueTextTarget ResolveTarget(DialogueBoxKind kind)
    {
        DialogueBoxHostEntry entry = FindEntry(kind);
        IPresentationDialogueBoxView view = GetOrCreateView(entry);

        view.Validate();
        view.SetVisible(false);

        return view;
    }

    public void ShowOnly(IDialogueTextTarget target)
    {
        HideAll();

        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view == null)
            return;

        view.SetVisible(true);
    }

    private bool ShouldSuppressActivation()
    {
        return _context != null &&
               _context.IsRollbackSeeking || _context.IsSkipping;
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

    private IPresentationDialogueBoxView GetOrCreateView(DialogueBoxHostEntry entry)
    {
        EnsureDialogueKey(entry.dialogueKey);

        if (TryGetView(entry.dialogueKey, out IPresentationDialogueBoxView view))
            return view;

        return CreateAndRegisterView(entry);
    }

    private IPresentationDialogueBoxView CreateAndRegisterView(DialogueBoxHostEntry entry)
    {
        if (dialogueBoxRoot == null)
            throw new InvalidOperationException("[DialogueBoxHost] dialogueBoxRoot is required.");

        if (entry.prefab == null)
        {
            throw new InvalidOperationException(
                $"[DialogueBoxHost] DialogueBox prefab is missing. kind={entry.kind}, dialogueKey={entry.dialogueKey}");
        }

        GameObject go = Object.Instantiate(entry.prefab, dialogueBoxRoot, false);
        go.name = $"DialogueBox_{entry.dialogueKey}";

        IPresentationDialogueBoxView view = FindView(go);
        view.Validate();
        view.SetVisible(false);

        Register(entry.dialogueKey, view);
        return view;
    }

    private static IPresentationDialogueBoxView FindView(GameObject go)
    {
        MonoBehaviour[] behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPresentationDialogueBoxView view)
                return view;
        }

        throw new InvalidOperationException(
            $"[DialogueBoxHost] Prefab must have IPresentationDialogueBoxView. prefab={go.name}");
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