using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public struct DialogueBoxRouteEntry
{
    public DialogueBoxKind kind;

    [Header("Runtime Identity")]
    public string dialogueKey;
}
public sealed class RuntimeDialogueBoxViewResolver : MonoBehaviour, IDialogueBoxViewResolver
{
    [SerializeField] private DialogueBoxRouteEntry[] routes;

    private Dictionary<string, object> _refs;

    public void BindRefs(Dictionary<string, object> refs)
    {
        _refs = refs;
    }

    public IDialogueTextTarget Activate(DialogueBoxKind kind)
    {
        HideAll();

        string dialogueKey = ResolveDialogueKey(kind);

        if (!_refs.TryGetDialogueBoxView(dialogueKey, out PresentationDialogueBoxView view))
            throw new InvalidOperationException($"[RuntimeDialogueBoxViewResolver] DialogueBox view not found. dialogueKey={dialogueKey}");

        view.Validate();
        view.SetVisible(true);

        return view;
    }

    public void HideAll()
    {
        for (int i = 0; i < routes.Length; i++)
        {
            string dialogueKey = routes[i].dialogueKey;

            if (_refs.TryGetDialogueBoxView(dialogueKey, out PresentationDialogueBoxView view))
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

        throw new InvalidOperationException($"[RuntimeDialogueBoxViewResolver] Route not found. kind={kind}");
    }
}