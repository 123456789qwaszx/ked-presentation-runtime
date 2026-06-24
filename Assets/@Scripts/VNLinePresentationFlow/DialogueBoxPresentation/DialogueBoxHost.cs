using System;
using UnityEngine;

public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3,
    BlackBook = 4,
    Surface = 5,
}

public enum OptionsBoxKind
{
    Default = 0,
}

[Serializable]
public struct DialogueBoxHostEntry
{
    public DialogueBoxKind kind;

    [Tooltip("IPresentationDialogueBoxView 구현체")]
    public MonoBehaviour view;
}

[Serializable]
public struct OptionsBoxHostEntry
{
    public OptionsBoxKind kind;

    [Tooltip("IPresentationOptionsBoxView 구현체")]
    public MonoBehaviour view;
}

public sealed class DialogueBoxHost : MonoBehaviour
{
    [Header("Dialogue Box Entries")]
    [SerializeField] private DialogueBoxHostEntry[] entries;

    [Header("Options Box Entries")]
    [SerializeField] private OptionsBoxHostEntry[] optionEntries;

    public IPresentationDialogueBoxView ResolveTarget(DialogueBoxKind kind)
    {
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning($"[DialogueBoxHost] No dialogue box entries are assigned. kind={kind}", this);
            return null;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].kind != kind)
                continue;

            MonoBehaviour behaviour = entries[i].view;

            if (!behaviour)
            {
                Debug.LogWarning($"[DialogueBoxHost] View is null. kind={entries[i].kind}", this);
                return null;
            }

            IPresentationDialogueBoxView view = behaviour as IPresentationDialogueBoxView;

            if (view == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] View must implement IPresentationDialogueBoxView. kind={entries[i].kind}, go={behaviour.name}",
                    behaviour);

                return null;
            }

            return view;
        }

        Debug.LogWarning($"[DialogueBoxHost] Entry not found. kind={kind}", this);
        return null;
    }

    public IPresentationOptionsBoxView ResolveOptionsTarget(OptionsBoxKind kind)
    {
        if (optionEntries == null || optionEntries.Length == 0)
        {
            Debug.LogWarning($"[DialogueBoxHost] No options box entries are assigned. kind={kind}", this);
            return null;
        }

        for (int i = 0; i < optionEntries.Length; i++)
        {
            if (optionEntries[i].kind != kind)
                continue;

            MonoBehaviour behaviour = optionEntries[i].view;

            if (!behaviour)
            {
                Debug.LogWarning($"[DialogueBoxHost] Options view is null. kind={optionEntries[i].kind}", this);
                return null;
            }

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;

            if (view == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Options view must implement IPresentationOptionsBoxView. kind={optionEntries[i].kind}, go={behaviour.name}",
                    behaviour);

                return null;
            }

            return view;
        }

        Debug.LogWarning($"[DialogueBoxHost] Options entry not found. kind={kind}", this);
        return null;
    }

    public void HideAllDialogueBoxes()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            MonoBehaviour behaviour = entries[i].view;
            if (!behaviour)
                continue;

            IPresentationDialogueBoxView view = behaviour as IPresentationDialogueBoxView;
            if (view == null)
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    public void HideAllDialogueBoxesExcept(IPresentationDialogueBoxView target)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            MonoBehaviour behaviour = entries[i].view;
            if (!behaviour)
                continue;

            IPresentationDialogueBoxView view = behaviour as IPresentationDialogueBoxView;
            if (view == null)
                continue;

            if (ReferenceEquals(view, target))
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    public void HideAllOptionsBoxes()
    {
        if (optionEntries == null)
            return;

        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;
            if (!behaviour)
                continue;

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;
            if (view == null)
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    public void HideAllOptionsBoxesExcept(IPresentationOptionsBoxView target)
    {
        if (optionEntries == null)
            return;

        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;
            if (!behaviour)
                continue;

            IPresentationOptionsBoxView view = behaviour as IPresentationOptionsBoxView;
            if (view == null)
                continue;

            if (ReferenceEquals(view, target))
                continue;

            view.SetVisibleImmediate(false);
        }
    }

    #region Validate

    private void OnValidate()
    {
        ValidateDialogueEntries();
        ValidateOptionsEntries();
    }

    private void ValidateDialogueEntries()
    {
        if (entries == null)
        {
            Debug.LogWarning("[DialogueBoxHost] entries is null.", this);
            return;
        }

        if (entries.Length == 0)
        {
            Debug.LogWarning(
                "[DialogueBoxHost] entries is empty. At least one dialogue box view should be assigned.",
                this);

            return;
        }

        ValidateDuplicateDialogueKinds();
        ValidateDialogueEntryViews();
    }

    private void ValidateOptionsEntries()
    {
        if (optionEntries == null || optionEntries.Length == 0)
            return;

        ValidateDuplicateOptionsKinds();
        ValidateOptionsEntryViews();
    }

    private void ValidateDuplicateDialogueKinds()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            for (int j = i + 1; j < entries.Length; j++)
            {
                if (entries[i].kind != entries[j].kind)
                    continue;

                Debug.LogWarning(
                    $"[DialogueBoxHost] Duplicate entry for DialogueBoxKind.{entries[i].kind}. indices={i}, {j}",
                    this);
            }
        }
    }

    private void ValidateDuplicateOptionsKinds()
    {
        for (int i = 0; i < optionEntries.Length; i++)
        {
            for (int j = i + 1; j < optionEntries.Length; j++)
            {
                if (optionEntries[i].kind != optionEntries[j].kind)
                    continue;

                Debug.LogWarning(
                    $"[DialogueBoxHost] Duplicate entry for OptionsBoxKind.{optionEntries[i].kind}. indices={i}, {j}",
                    this);
            }
        }
    }

    private void ValidateDialogueEntryViews()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            MonoBehaviour behaviour = entries[i].view;

            if (behaviour == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Entry view is null. index={i}, kind={entries[i].kind}",
                    this);

                continue;
            }

            if (behaviour is IPresentationDialogueBoxView)
                continue;

            Debug.LogWarning(
                $"[DialogueBoxHost] Entry view must implement IPresentationDialogueBoxView. index={i}, kind={entries[i].kind}, go={behaviour.name}",
                behaviour);
        }
    }

    private void ValidateOptionsEntryViews()
    {
        for (int i = 0; i < optionEntries.Length; i++)
        {
            MonoBehaviour behaviour = optionEntries[i].view;

            if (behaviour == null)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Options entry view is null. index={i}, kind={optionEntries[i].kind}",
                    this);

                continue;
            }

            if (behaviour is IPresentationOptionsBoxView)
                continue;

            Debug.LogWarning(
                $"[DialogueBoxHost] Options entry view must implement IPresentationOptionsBoxView. index={i}, kind={optionEntries[i].kind}, go={behaviour.name}",
                behaviour);
        }
    }

    #endregion
}
