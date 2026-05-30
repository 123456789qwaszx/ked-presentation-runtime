using UnityEngine;
using Yarn.Unity;

public sealed class DialogueBoxTransitionRunner
{
    private readonly DialogueBoxHost _dialogueBoxResolver;

    public DialogueBoxTransitionRunner(DialogueBoxHost dialogueBoxResolver)
    {
        _dialogueBoxResolver = dialogueBoxResolver;
    }

    public void Prepare(DialogueBoxTransitionPlan plan)
    {
        IDialogueTextTarget nextBox = plan.NextBox;

        DialogueBoxHost host = _dialogueBoxResolver;

        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                break;

            case DialogueBoxTransitionKind.Cut:
                if (host != null)
                {
                    host.HideAllExcept(nextBox);
                    host.ShowImmediate(nextBox);
                }
                else
                {
                    HideAll();
                    SetVisibleImmediate(nextBox, true);
                }

                break;

            case DialogueBoxTransitionKind.FadeIn:
                if (host != null)
                {
                    host.HideAllExcept(nextBox);
                    host.PrepareHidden(nextBox);
                }
                else
                {
                    HideAll();
                    PrepareHidden(nextBox);
                }

                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                PrepareHidden(nextBox);
                break;

            case DialogueBoxTransitionKind.Hide:
                break;
        }
    }

    public async YarnTask ApplyAsync(
        DialogueBoxTransitionPlan plan,
        float fadeUpDuration,
        float fadeDownDuration,
        LinePresentationRun run)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (run.IsValid)
                    SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
                if (run.IsValid)
                    ApplyImmediate(plan);
                break;

            case DialogueBoxTransitionKind.FadeIn:
                await FadeInBoxAsync(plan.NextBox, fadeUpDuration, run);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                    await FadeOutBoxAsync(plan.PreviousBox, fadeDownDuration, run);
                
                if (!run.IsValid)
                    break;

                SetVisibleImmediate(plan.PreviousBox, false);
                PrepareHidden(plan.NextBox);

                await FadeInBoxAsync(plan.NextBox, fadeUpDuration, run);
                break;

            case DialogueBoxTransitionKind.Hide:
                if (plan.NextBox != null)
                    await FadeOutBoxAsync(plan.NextBox, fadeDownDuration, run);

                if (run.IsValid)
                    SetVisibleImmediate(plan.NextBox, false);
                break;
        }
    }

    public void ApplyImmediate(DialogueBoxTransitionPlan plan)
    {
        switch (plan.TransitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
            case DialogueBoxTransitionKind.FadeIn:
                HideAllExcept(plan.NextBox);
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (plan.PreviousBox != null && !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                    SetVisibleImmediate(plan.PreviousBox, false);
                
                HideAllExcept(plan.NextBox);
                SetVisibleImmediate(plan.NextBox, true);
                break;

            case DialogueBoxTransitionKind.Hide:
                SetVisibleImmediate(plan.NextBox, false);
                break;
        }
    }

    public void HideAll()
    {
        _dialogueBoxResolver.HideAll();
    }

    public void HideAllExcept(IDialogueTextTarget keep)
    {
        DialogueBoxHost host = _dialogueBoxResolver;
        if (host != null)
        {
            host.HideAllExcept(keep);
            return;
        }

        HideAll();

        if (keep != null)
            SetVisibleImmediate(keep, true);
    }

    public void PrepareHidden(IDialogueTextTarget box)
    {
        if (box == null)
            return;

        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
            view.SetVisible(true);

        if (box.CanvasGroup != null)
        {
            box.CanvasGroup.alpha = 0f;
            box.CanvasGroup.interactable = false;
            box.CanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetVisibleImmediate(IDialogueTextTarget box, bool visible)
    {
        if (box == null)
            return;

        IPresentationDialogueBoxView view = box as IPresentationDialogueBoxView;
        if (view != null)
        {
            view.SetVisible(visible);

            if (view.CanvasGroup != null)
            {
                view.CanvasGroup.alpha = visible ? 1f : 0f;
                view.CanvasGroup.interactable = visible;
                view.CanvasGroup.blocksRaycasts = visible;
            }

            return;
        }

        if (box.CanvasGroup != null)
        {
            box.CanvasGroup.alpha = visible ? 1f : 0f;
            box.CanvasGroup.interactable = visible;
            box.CanvasGroup.blocksRaycasts = visible;
        }
    }

    public void ResetBoxTransform(IDialogueTextTarget box)
    {
        if (box == null) 
            return;

        MonoBehaviour behaviour = box as MonoBehaviour;
        if (behaviour == null) 
            return;

        RectTransform rect = behaviour.transform as RectTransform;
        if (rect != null)
        {
            rect.localPosition = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
            return;
        }

        behaviour.transform.localPosition = Vector3.zero;
    }

    private async YarnTask FadeInBoxAsync(
        IDialogueTextTarget box,
        float duration,
        LinePresentationRun run)
    {
        if (box == null || box.CanvasGroup == null) return;
        if (run == null || !run.IsValid) return;
        

        CanvasGroup cg = box.CanvasGroup;

        SetVisibleImmediate(box, true);
        cg.alpha = 0f;

        await Effects
            .FadeAlphaAsync(cg, 0f, 1f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;
        

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

    }

    private async YarnTask FadeOutBoxAsync(IDialogueTextTarget box, float duration, LinePresentationRun run)
    {
        if (box == null || box.CanvasGroup == null) return;
        if (run == null || !run.IsValid) return;
        
        CanvasGroup cg = box.CanvasGroup;
        float fromAlpha = cg.alpha;

        await Effects
            .FadeAlphaAsync(cg, fromAlpha, 0f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
            return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}