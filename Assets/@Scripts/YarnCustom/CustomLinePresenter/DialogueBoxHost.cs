using System;
using UnityEngine;

public interface IDialogueBoxViewResolver
{
    IDialogueTextTarget ResolveTarget(DialogueBoxKind kind);
    void ShowOnly(IDialogueTextTarget target);
    void HideAll();
}

public enum DialogueBoxKind
{
    Portrait = 0,
    Speaker = 1,
    LetterBox = 2,
    OnlyText = 3,
    BlackBook= 4
}

[Serializable]
public struct DialogueBoxHostEntry
{
    public DialogueBoxKind kind;

    [Tooltip("IPresentationDialogueBoxView 구현체")]
    public MonoBehaviour view;
}

public sealed class DialogueBoxHost : MonoBehaviour, IDialogueBoxViewResolver
{
    [Header("Dialogue Box Entries")]
    [SerializeField] private DialogueBoxHostEntry[] entries;
    
    public IDialogueTextTarget ResolveTarget(DialogueBoxKind kind)
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

            if (entries[i].view == null)
            {
                Debug.LogWarning($"[DialogueBoxHost] View is null. kind={entries[i].kind}", this);

                return null;
            }

            IPresentationDialogueBoxView view = entries[i].view as IPresentationDialogueBoxView;
            if (view == null)
            {
                Debug.LogWarning($"[DialogueBoxHost] View must implement IPresentationDialogueBoxView. kind={entries[i].kind}, go={entries[i].view.name}", entries[i].view);

                return null;
            }

            //view.Validate();
            return view;
        }

        Debug.LogWarning($"[DialogueBoxHost] Entry not found. kind={kind}", this);
        return null;
    }

    public void ShowOnly(IDialogueTextTarget target)
    {
        HideAll();

        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        view?.SetVisible(true);
    }

    
    public void HideAll()
    {
        if (entries == null)
            return;
        
        for (int i = 0; i < entries.Length; i++)
        {
            // Unity-destroyed views can remain as C# references.
            // Check as MonoBehaviour before casting to an interface.
            MonoBehaviour behaviour = entries[i].view;
            if (!behaviour) continue;

            IPresentationDialogueBoxView view = behaviour as IPresentationDialogueBoxView;
            if (view == null) 
                continue;

            view.SetVisible(false);
        }
    }
    
    public IPresentationDialogueBoxView AsView(IDialogueTextTarget target)
    {
        return target as IPresentationDialogueBoxView;
    }

    public void ShowImmediate(IDialogueTextTarget target)
    {
        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view == null)
            return;

        view.SetVisible(true);

        if (view.CanvasGroup != null)
            view.CanvasGroup.alpha = 1f;
    }

    public void HideImmediate(IDialogueTextTarget target)
    {
        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view == null)
            return;

        view.SetVisible(false);

        if (view.CanvasGroup != null)
            view.CanvasGroup.alpha = 0f;
    }

    public void PrepareHidden(IDialogueTextTarget target)
    {
        IPresentationDialogueBoxView view = target as IPresentationDialogueBoxView;
        if (view == null)
            return;

        view.SetVisible(true);

        if (view.CanvasGroup != null)
        {
            view.CanvasGroup.alpha = 0f;
            view.CanvasGroup.interactable = false;
            view.CanvasGroup.blocksRaycasts = false;
        }
    }

    public void HideAllExcept(IDialogueTextTarget target)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            // Unity-destroyed views can remain as C# references.
            // Check as MonoBehaviour before casting to an interface.
            MonoBehaviour behaviour = entries[i].view;
            if (!behaviour) continue;

            IPresentationDialogueBoxView view = behaviour as IPresentationDialogueBoxView;
            if (view == null)
                continue;

            if (ReferenceEquals(view, target))
                continue;

            view.SetVisible(false);
        }
    }
    
    #region Validate
    
    private void OnValidate()
    {
        if (entries == null)
        {
            Debug.LogWarning(
                "[DialogueBoxHost] entries is null.",
                this);

            return;
        }

        if (entries.Length == 0)
        {
            Debug.LogWarning(
                "[DialogueBoxHost] entries is empty. At least one dialogue box view should be assigned.",
                this);

            return;
        }

        ValidateAllKindsAssigned();
        ValidateDuplicateKinds();
        ValidateEntryViews();
    }

    private void ValidateAllKindsAssigned()
    {
        Array values = Enum.GetValues(typeof(DialogueBoxKind));

        for (int i = 0; i < values.Length; i++)
        {
            DialogueBoxKind kind = (DialogueBoxKind)values.GetValue(i);

            bool found = false;

            for (int j = 0; j < entries.Length; j++)
            {
                if (entries[j].kind == kind)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning(
                    $"[DialogueBoxHost] Missing entry for DialogueBoxKind.{kind}.",
                    this);
            }
        }
    }
    
    private void ValidateDuplicateKinds()
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
    
    private void ValidateEntryViews()
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
    
    #endregion
}