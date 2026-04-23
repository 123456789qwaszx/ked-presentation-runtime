using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PresentationDialogueBoxView : MonoBehaviour
{
    [Header("Optional Explicit Bindings")]
    [SerializeField] private RectTransform root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text nameText;

    public RectTransform Root { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }
    public TMP_Text BodyText { get; private set; }
    public TMP_Text NameText { get; private set; }

    public void EnsureBound(bool strict = true)
    {
        Root = root != null ? root : transform as RectTransform;
        if (Root == null)
        {
            if (strict)
                throw new InvalidOperationException($"[PresentationDialogueBoxView] Root must be RectTransform. go={name}");
            return;
        }

        if (canvasGroup == null && !TryGetComponent(out canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CanvasGroup = canvasGroup;

        BodyText = bodyText != null ? bodyText : FindBodyText();
        NameText = nameText != null ? nameText : FindNameText();

        if (strict && BodyText == null)
            throw new InvalidOperationException($"[PresentationDialogueBoxView] Missing BodyText under '{name}'.");
    }

    private TMP_Text FindBodyText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string n = texts[i].name;
            if (string.Equals(n, "BodyText", StringComparison.Ordinal) ||
                string.Equals(n, "DialogueText", StringComparison.Ordinal) ||
                string.Equals(n, "LineText", StringComparison.Ordinal) ||
                string.Equals(n, "Text", StringComparison.Ordinal))
                return texts[i];
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private TMP_Text FindNameText()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string n = texts[i].name;
            if (string.Equals(n, "NameText", StringComparison.Ordinal) ||
                string.Equals(n, "SpeakerText", StringComparison.Ordinal))
                return texts[i];
        }

        return null;
    }
}

public static class PresentationDialogueBoxRegistryExt
{
    public static string MakeDialogueBoxRefKey(string dialogueKey)
    {
        dialogueKey = SafeTrim(dialogueKey);
        return $"dlg:{dialogueKey}";
    }

    public static bool TryGetDialogueBoxView(this Dictionary<string, object> refs, string dialogueKey, out PresentationDialogueBoxView view)
    {
        string key = MakeDialogueBoxRefKey(dialogueKey);

        if (refs != null && refs.TryGetValue(key, out object obj) && obj is PresentationDialogueBoxView typed)
        {
            view = typed;
            return true;
        }

        view = null;
        return false;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}