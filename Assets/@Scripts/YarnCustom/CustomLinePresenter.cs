using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

public interface ILinePresentationAborter
{
    void AbortCurrentLinePresentationForRollback();
}

public sealed class CustomLinePresenter : DialoguePresenterBase, ILinePresentationAborter
{
    [Header("Fade")] public bool useFadeEffect = true;
    public float fadeUpDuration = 0.25f;
    public float fadeDownDuration = 0.1f;

    private DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private IDialogueBoxViewResolver _dialogueBoxResolver;
    private DialogueTextRouter _dialogueTextRouter;
    private EllipsisBreathTypewriter _typewriter;
    private PresentationSessionContext _context;
    private LinePresentationAdvanceState _lineAdvanceState;

    private readonly DialogueBoxTransitionPolicy _boxTransitionPolicy = new();
    private readonly DialogueBoxCurrentState _boxState = new();
    

    private int _presenterGeneration;
    private CancellationTokenSource _presenterLifetimeCts = new CancellationTokenSource(); // 외부 시스템이 이 Presenter의 실행을 무효화하는 신호

    public void Initialize(
        DialogueRunner dialogueRunner,
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        IDialogueBoxViewResolver dialogueBoxResolver,
        DialogueTextRouter dialogueTextRouter,
        EllipsisBreathTypewriter typewriter,
        PresentationSessionContext context,
        LinePresentationAdvanceState lineAdvanceState)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueTextRouter = dialogueTextRouter;
        _typewriter = typewriter;
        _context = context;
        _lineAdvanceState = lineAdvanceState;

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
        CloseAll();
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
        _lineAdvanceState.MarkLineEntered();

        int myGeneration = _presenterGeneration;

        bool IsStale()
        {
            return myGeneration != _presenterGeneration;
        }

        bool isRollbackTargetLine = _lineAdvanceState.IsRollbackTargetLine(line.TextID);
        bool isRollbackSeekingLine = _lineAdvanceState.IsRollbackSeeking && !isRollbackTargetLine;
        
        // Skip visual presentation for lines passed during rollback seek.
        // Keep the Yarn line lifecycle alive until the runner requests next content.
        if (isRollbackSeekingLine)
        {
            HideBoxDuringRollbackSeek();

            _lineAdvanceState.MarkTransitionFinished();
            _lineAdvanceState.MarkLineDisplayCompleted();

            await WaitForLineAdvanceAsync(token);
            return;
        }

        bool hasCharacterName = !string.IsNullOrWhiteSpace(line.CharacterName);
        bool shouldFastForwardLine = !isRollbackTargetLine && ShouldFastForwardLine();


        IDialogueTextTarget currentBox = _boxState.Box;
        DialogueBoxKind? currentBoxKind = _boxState.BoxKind;
        bool currentBoxIsVisible = _boxState.IsVisible;
        
        DialogueBoxCurrentState currentBoxState = _boxState;
        
        DialogueBoxKind nextBoxKind = _lineRoutingPolicy.Resolve(line.Metadata, hasCharacterName);
        IDialogueTextTarget nextBox = _dialogueBoxResolver.ResolveTarget(nextBoxKind);
        ResetBoxTransform(nextBox);
        
        DialogueBoxTransitionKind transitionKind =
            _boxTransitionPolicy.Resolve(
                currentBoxKind,
                currentBoxIsVisible,
                nextBoxKind,
                line.Metadata,
                shouldFastForwardLine);

        PrimeTextTarget(nextBox, line);

        
        PrepareBoxForTransition(nextBox, transitionKind);

        if (isRollbackTargetLine)
        {
            // Target line은 다시 화면에 보여줄 현재 line이다.
            // Transition만 즉시 적용하고, Typewriter는 아래에서 정상 실행한다.
            _lineAdvanceState.ConsumeRollbackTargetLine(line.TextID);
            ApplyBoxTransitionImmediate(currentBox, nextBox, transitionKind);
        }
        else
        {
            await ApplyBoxTransitionAsync(
                currentBox,
                nextBox,
                transitionKind,
                token,
                IsStale);
        }

        if (IsStale())
        {
            CleanupStaleLinePresentation(currentBox, nextBox);
            await WaitForLineAdvanceAsync(token);
            return;
        }

        _lineAdvanceState.MarkTransitionFinished();
        
        _boxState.Commit(nextBoxKind, nextBox, transitionKind); // 여기서 부터 nextBox가 _boxState로 커밋.
        _dialogueTextRouter.Bind(_boxState);
        
        if (hasCharacterName && _dialogueTextRouter.HasName)
            _dialogueTextRouter.NameText.text = line.CharacterName;
        
        if (_dialogueTextRouter.LineText != null)
        {
            _typewriter.SetTextView(_dialogueTextRouter.LineText);
            MarkupParseResult text = line.TextWithoutCharacterName;

            _typewriter.PrepareForContent(text);
            _lineAdvanceState.MarkTypewriterStarted();

            if (!IsStale())
                _lineAdvanceState.ClearRollbackSeek();
            
            await _typewriter
                .RunTypewriter(text, token.HurryUpToken)
                .SuppressCancellationThrow();

            if (!IsStale())
            {
                _lineAdvanceState.MarkLineDisplayCompleted();
                _typewriter.ContentWillDismiss();
            }
        }
        else
        {
            if (!IsStale())
                _lineAdvanceState.MarkLineDisplayCompleted();
        }


        await WaitForLineAdvanceAsync(token);
    }
    
    private void HideBoxDuringRollbackSeek()
    {
        CloseAll();
    }

    private void PrimeTextTarget(IDialogueTextTarget nextBox, LocalizedLine line)
    {
        if (nextBox == null)
            return;

        TMP_Text lineText = nextBox.LineText;
        if (lineText != null)
        {
            lineText.text = line.TextWithoutCharacterName.Text;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
        }

        TMP_Text nameText = nextBox.NameText;
        if (nameText != null)
        {
            bool showName = !string.IsNullOrWhiteSpace(line.CharacterName);

            nameText.text = showName
                ? line.CharacterName
                : string.Empty;

            nameText.gameObject.SetActive(showName);
        }
    }

    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource lineWaitCts = null;

        try
        {
            lineWaitCts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask
                .WaitUntilCanceled(lineWaitCts.Token)
                .SuppressCancellationThrow();
        }
        finally
        {
            if (lineWaitCts != null)
                lineWaitCts.Dispose();
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
        if (!useFadeEffect || ShouldFastForwardLine())
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
    
    private void CleanupStaleLinePresentation(IDialogueTextTarget previousBox, IDialogueTextTarget nextBox)
    {
        // Stale 실행본은 더 이상 현재 TextRouter/Typewriter의 주인이 아니다.
        // 여기서 _typewriter.SetTextView(null) 또는 _dialogueTextRouter.Clear()를 호출하면,
        // 새 rollback target line이 이미 바인딩한 TMP_Text를 뒤늦게 비워버릴 수 있다.
        //
        // 따라서 stale cleanup은 "이 실행본이 transition 중 임시로 건드렸을 수 있는 box"만 정리한다.

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

    private bool ShouldFastForwardLine()
    {
        if (_context == null)
            return false;

        return _lineAdvanceState.IsRollbackSeeking || _context.IsSpeedUpMode;
    }

    private void CloseAll()
    {
        _dialogueBoxResolver?.HideAll();
        _dialogueTextRouter?.Clear();

        _typewriter?.SetTextView(null);

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

    private static void ResetBoxTransform(IDialogueTextTarget box)
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