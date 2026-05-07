using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public interface ILinePresentationAborter
{
    void AbortCurrentLinePresentationForRollback();
}

public sealed class CustomLinePresenter : DialoguePresenterBase, ILinePresentationAborter
{
    [Header("Fade")]
    public bool useFadeEffect = true;
    public float fadeUpDuration = 0.25f;
    public float fadeDownDuration = 0.1f;

    private DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private IDialogueBoxViewResolver _dialogueBoxResolver;
    private DialogueTextRouter _dialogueTextRouter;
    private EllipsisBreathTypewriter _typewriter;
    private PresentationSessionContext _context;

    private readonly DialogueBoxTransitionPolicy _boxTransitionPolicy = new DialogueBoxTransitionPolicy();
    private readonly DialogueBoxCurrentState _boxState = new DialogueBoxCurrentState();

    private TMP_Text _lineText;
    private TMP_Text _characterNameText;
    private GameObject _characterNameContainer;
    private CanvasGroup _canvasGroup;

    private int _presenterGeneration;
    private CancellationTokenSource _presenterLifetimeCts = new CancellationTokenSource();

    public void Initialize(
        DialogueRunner dialogueRunner,
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        IDialogueBoxViewResolver dialogueBoxResolver,
        DialogueTextRouter dialogueTextRouter,
        EllipsisBreathTypewriter typewriter,
        PresentationSessionContext context)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueTextRouter = dialogueTextRouter;
        _typewriter = typewriter;
        _context = context;

        if (dialogueRunner == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: dialogueRunner is null.");
            return;
        }

        RegisterBeforeDefaultLinePresenter(dialogueRunner);
    }

    public void AbortCurrentLinePresentationForRollback()
    {
        _presenterGeneration++;

        if (_typewriter != null)
            _typewriter.SetTextView(null);
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        CloseAll();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CancelPresenterLifetimeWaiters();
        CloseAll();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        int myGeneration = _presenterGeneration;

        bool IsStale()
        {
            return myGeneration != _presenterGeneration;
        }

        DialogueBoxKind nextBoxKind = ResolveNextBoxKind(line);
        IDialogueTextTarget nextBox = _dialogueBoxResolver.ResolveTarget(nextBoxKind);

        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(line, nextBoxKind);

        IDialogueTextTarget previousBox = _boxState.Box;

        ResetDialogueBoxTransform(nextBox);
        PrepareIncomingTextTarget(nextBox, line);
        PrepareBoxForTransition(nextBox, transitionKind);

        await ApplyBoxTransitionAsync(
            previousBox,
            nextBox,
            transitionKind,
            token,
            IsStale);

        if (IsStale())
        {
            CleanupStaleLinePresentation(previousBox, nextBox);
            await WaitForLineAdvanceAsync(token);
            return;
        }

        _boxState.Commit(nextBoxKind, nextBox, transitionKind);

        BindTextTargets(nextBox, line);

        if (_lineText != null)
        {
            var text = line.TextWithoutCharacterName;

            _typewriter.SetTextView(_lineText);
            _typewriter.PrepareForContent(text);

            await _typewriter
                .RunTypewriter(text, token.HurryUpToken)
                .SuppressCancellationThrow();

            if (!IsStale())
                _typewriter.ContentWillDismiss();
        }

        await WaitForLineAdvanceAsync(token);
    }
    
    private void PrepareIncomingTextTarget(
        IDialogueTextTarget nextBox,
        LocalizedLine line)
    {
        if (nextBox == null)
            return;

        TMP_Text lineText = nextBox.LineText;
        if (lineText != null)
        {
            string bodyText = line != null
                ? line.TextWithoutCharacterName.Text
                : string.Empty;

            lineText.text = bodyText;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
        }

        TMP_Text nameText = nextBox.NameText;
        if (nameText != null)
        {
            bool showName = line != null &&
                            string.IsNullOrWhiteSpace(line.CharacterName) == false;

            nameText.text = showName ? line.CharacterName : string.Empty;
            nameText.gameObject.SetActive(showName);
        }
    }

    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource linkedCts = null;

        try
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask
                .WaitUntilCanceled(linkedCts.Token)
                .SuppressCancellationThrow();
        }
        finally
        {
            if (linkedCts != null)
                linkedCts.Dispose();
        }
    }

    private void CancelPresenterLifetimeWaiters()
    {
        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
        }

        _presenterLifetimeCts = new CancellationTokenSource();
    }

    private DialogueBoxKind ResolveNextBoxKind(LocalizedLine line)
    {
        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;

        if (_lineRoutingPolicy.TryResolveBoxKindFromMetadata(line.Metadata, out DialogueBoxKind metadataBoxKind))
            return metadataBoxKind;

        return _lineRoutingPolicy.Resolve(hasCharacterName);
    }

    private DialogueBoxTransitionKind ResolveTransitionKind(
        LocalizedLine line,
        DialogueBoxKind nextBoxKind)
    {
        return _boxTransitionPolicy.Resolve(
            _boxState.BoxKind,
            _boxState.IsVisible,
            nextBoxKind,
            line.Metadata,
            ShouldConsumeLineSilently());
    }

    private void BindTextTargets(IDialogueTextTarget nextBox, LocalizedLine line)
    {
        if (nextBox == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: nextBox is null. Skipping {line.TextID}.");
            return;
        }

        _dialogueTextRouter.Bind(nextBox);

        _lineText = _dialogueTextRouter.LineText;
        _canvasGroup = nextBox.CanvasGroup;

        if (_lineText == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: lineText is null. Skipping {line.TextID}.");
            return;
        }

        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;

        BindCharacterNameTarget(hasCharacterName);

        if (_characterNameContainer == null)
            return;

        bool showName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        _characterNameContainer.SetActive(showName);

        if (showName && _characterNameText != null)
            _characterNameText.text = line.CharacterName;
    }

    private void PrepareBoxForTransition(
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind)
    {
        DialogueBoxHost host = _dialogueBoxResolver as DialogueBoxHost;

        switch (transitionKind)
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
                    _dialogueBoxResolver.HideAll();
                    SetBoxVisibleImmediate(nextBox, true);
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
                    _dialogueBoxResolver.HideAll();
                    PrepareBoxHidden(nextBox);
                }
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                PrepareBoxHidden(nextBox);
                break;

            case DialogueBoxTransitionKind.Hide:
                break;
        }
    }

    private async YarnTask ApplyBoxTransitionAsync(
        IDialogueTextTarget previousBox,
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind,
        LineCancellationToken token,
        Func<bool> isStale)
    {
        if (!useFadeEffect || ShouldConsumeLineSilently())
        {
            if (!isStale())
                ApplyBoxTransitionImmediate(previousBox, nextBox, transitionKind);

            return;
        }

        switch (transitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                if (!isStale())
                    SetBoxVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
                if (!isStale())
                    ApplyBoxTransitionImmediate(previousBox, nextBox, transitionKind);
                break;

            case DialogueBoxTransitionKind.FadeIn:
                await FadeInBoxAsync(nextBox, token, isStale);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                    await FadeOutBoxAsync(previousBox, token, isStale);

                if (isStale())
                    break;

                SetBoxVisibleImmediate(previousBox, false);
                PrepareBoxHidden(nextBox);

                await FadeInBoxAsync(nextBox, token, isStale);
                break;

            case DialogueBoxTransitionKind.Hide:
                if (nextBox != null)
                    await FadeOutBoxAsync(nextBox, token, isStale);

                if (!isStale())
                    SetBoxVisibleImmediate(nextBox, false);
                break;
        }
    }

    private void ApplyBoxTransitionImmediate(
        IDialogueTextTarget previousBox,
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind)
    {
        switch (transitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                SetBoxVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
            case DialogueBoxTransitionKind.FadeIn:
                HideAllExcept(nextBox);
                SetBoxVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                    SetBoxVisibleImmediate(previousBox, false);

                HideAllExcept(nextBox);
                SetBoxVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.Hide:
                SetBoxVisibleImmediate(nextBox, false);
                break;
        }
    }

    private async YarnTask FadeInBoxAsync(
        IDialogueTextTarget box,
        LineCancellationToken token,
        Func<bool> isStale)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        CanvasGroup cg = box.CanvasGroup;

        if (!isStale())
        {
            SetBoxVisibleImmediate(box, true);
            cg.alpha = 0f;
        }

        await Effects
            .FadeAlphaAsync(cg, 0f, 1f, fadeUpDuration, token.HurryUpToken)
            .SuppressCancellationThrow();

        if (isStale())
            return;

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private async YarnTask FadeOutBoxAsync(
        IDialogueTextTarget box,
        LineCancellationToken token,
        Func<bool> isStale)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        CanvasGroup cg = box.CanvasGroup;
        float fromAlpha = cg.alpha;

        await Effects
            .FadeAlphaAsync(cg, fromAlpha, 0f, fadeDownDuration, token.HurryUpToken)
            .SuppressCancellationThrow();

        if (isStale())
            return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private void CleanupStaleLinePresentation(
        IDialogueTextTarget previousBox,
        IDialogueTextTarget nextBox)
    {
        if (_typewriter != null)
            _typewriter.SetTextView(null);

        if (_dialogueTextRouter != null)
            _dialogueTextRouter.Clear();

        _lineText = null;
        _characterNameText = null;
        _characterNameContainer = null;
        _canvasGroup = null;

        if (nextBox != null && !ReferenceEquals(nextBox, _boxState.Box))
            SetBoxVisibleImmediate(nextBox, false);

        if (previousBox != null &&
            !ReferenceEquals(previousBox, _boxState.Box) &&
            !ReferenceEquals(previousBox, nextBox))
        {
            SetBoxVisibleImmediate(previousBox, false);
        }

        if (_boxState.IsVisible && _boxState.Box != null)
            SetBoxVisibleImmediate(_boxState.Box, true);
    }

    private void PrepareBoxHidden(IDialogueTextTarget box)
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

    private void SetBoxVisibleImmediate(IDialogueTextTarget box, bool visible)
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

    private void HideAllExcept(IDialogueTextTarget keep)
    {
        DialogueBoxHost host = _dialogueBoxResolver as DialogueBoxHost;
        if (host != null)
        {
            host.HideAllExcept(keep);
            return;
        }

        _dialogueBoxResolver.HideAll();

        if (keep != null)
            SetBoxVisibleImmediate(keep, true);
    }

    private bool ShouldConsumeLineSilently()
    {
        if (_context == null)
            return false;

        return _context.IsRollbackSeeking ||
               _context.IsSkipping;
    }

    private void BindCharacterNameTarget(bool hasCharacterName)
    {
        if (hasCharacterName && _dialogueTextRouter.HasName && _dialogueTextRouter.NameText != null)
        {
            _characterNameText = _dialogueTextRouter.NameText;
            _characterNameContainer = _dialogueTextRouter.NameText.gameObject;
        }
        else
        {
            _characterNameText = null;
            _characterNameContainer = null;
        }
    }

    private void CloseAll()
    {
        _dialogueBoxResolver?.HideAll();
        _dialogueTextRouter?.Clear();

        _typewriter?.SetTextView(null);

        _lineText = null;
        _characterNameText = null;
        _characterNameContainer = null;
        _canvasGroup = null;

        _boxState.Reset();
    }

    private void RegisterBeforeDefaultLinePresenter(DialogueRunner dialogueRunner)
    {
        List<DialoguePresenterBase> presenters = new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);
        presenters.Remove(this);

        int insertIndex = presenters.FindIndex(x => x is LinePresenter);
        if (insertIndex < 0)
            insertIndex = presenters.Count;

        presenters.Insert(insertIndex, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    private static void ResetDialogueBoxTransform(IDialogueTextTarget box)
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

    private void OnDestroy()
    {
        if (_presenterLifetimeCts != null)
        {
            _presenterLifetimeCts.Cancel();
            _presenterLifetimeCts.Dispose();
            _presenterLifetimeCts = null;
        }
    }
}