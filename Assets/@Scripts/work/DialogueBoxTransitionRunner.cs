using UnityEngine;
using Yarn.Unity;

public sealed class DialogueBoxTransitionRunner
{
    private readonly IDialogueBoxViewResolver _dialogueBoxResolver;
    private readonly VNTraceStream _trace;

    public DialogueBoxTransitionRunner(
        IDialogueBoxViewResolver dialogueBoxResolver,
        VNTraceStream trace = null)
    {
        _dialogueBoxResolver = dialogueBoxResolver;
        _trace = trace;
    }

    public void Prepare(DialogueBoxTransitionPlan plan)
    {
        if (plan == null)
            return;

        IDialogueTextTarget nextBox = plan.NextBox;

        Trace(
            "Prepare",
            $"transition={plan.TransitionKind}, next={GetBoxName(nextBox)}");

        DialogueBoxHost host = _dialogueBoxResolver as DialogueBoxHost;

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
        if (plan == null)
            return;

        if (run == null || !run.IsValid)
        {
            Trace("ApplyAsyncSkipped", "reason=InvalidRun");
            return;
        }

        Trace(
            "ApplyAsync",
            $"transition={plan.TransitionKind}, previous={GetBoxName(plan.PreviousBox)}, next={GetBoxName(plan.NextBox)}");

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
                if (plan.PreviousBox != null &&
                    !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                {
                    await FadeOutBoxAsync(plan.PreviousBox, fadeDownDuration, run);
                }

                if (!run.IsValid)
                {
                    Trace("ApplyAsyncCanceled", "phase=AfterFadeOut");
                    break;
                }

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
        if (plan == null)
            return;

        Trace(
            "ApplyImmediate",
            $"transition={plan.TransitionKind}, previous={GetBoxName(plan.PreviousBox)}, next={GetBoxName(plan.NextBox)}");

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
                if (plan.PreviousBox != null &&
                    !ReferenceEquals(plan.PreviousBox, plan.NextBox))
                {
                    SetVisibleImmediate(plan.PreviousBox, false);
                }

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
        if (_dialogueBoxResolver != null)
            _dialogueBoxResolver.HideAll();
    }

    public void HideAllExcept(IDialogueTextTarget keep)
    {
        DialogueBoxHost host = _dialogueBoxResolver as DialogueBoxHost;
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
        if (box == null || box.CanvasGroup == null)
            return;

        if (run == null || !run.IsValid)
        {
            Trace("FadeInSkipped", $"box={GetBoxName(box)}, reason=InvalidRun");
            return;
        }

        CanvasGroup cg = box.CanvasGroup;

        Trace("FadeInStart", $"box={GetBoxName(box)}, duration={duration}");

        SetVisibleImmediate(box, true);
        cg.alpha = 0f;

        await Effects
            .FadeAlphaAsync(cg, 0f, 1f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
        {
            Trace("FadeInCanceled", $"box={GetBoxName(box)}");
            return;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        Trace("FadeInComplete", $"box={GetBoxName(box)}");
    }

    private async YarnTask FadeOutBoxAsync(
        IDialogueTextTarget box,
        float duration,
        LinePresentationRun run)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        if (run == null || !run.IsValid)
        {
            Trace("FadeOutSkipped", $"box={GetBoxName(box)}, reason=InvalidRun");
            return;
        }

        CanvasGroup cg = box.CanvasGroup;
        float fromAlpha = cg.alpha;

        Trace("FadeOutStart", $"box={GetBoxName(box)}, from={fromAlpha}, duration={duration}");

        await Effects
            .FadeAlphaAsync(cg, fromAlpha, 0f, duration, run.VisualToken)
            .SuppressCancellationThrow();

        if (!run.IsValid)
        {
            Trace("FadeOutCanceled", $"box={GetBoxName(box)}");
            return;
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Trace("FadeOutComplete", $"box={GetBoxName(box)}");
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(
            "DialogueBoxTransition",
            evt,
            null,
            note);
    }

    private static string GetBoxName(IDialogueTextTarget box)
    {
        if (box == null)
            return "null";

        MonoBehaviour behaviour = box as MonoBehaviour;
        if (behaviour != null)
            return behaviour.name;

        return box.GetType().Name;
    }
}