using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public sealed class CustomLinePresenter : DialoguePresenterBase
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

    private TMP_Text _lineText;
    private TMP_Text _characterNameText;
    private GameObject _characterNameContainer;
    private CanvasGroup _canvasGroup;

    private DialogueBoxKind? _currentBoxKind;
    private IDialogueTextTarget _currentBox;
    private bool _isBoxVisible;

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

    public override YarnTask OnDialogueStartedAsync()
    {
        CloseAll();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CloseAll();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        DialogueBoxKind nextBoxKind = ResolveNextBoxKind(line);
        IDialogueTextTarget nextBox = _dialogueBoxResolver.ResolveTarget(nextBoxKind);

        DialogueBoxTransitionKind transitionKind = ResolveTransitionKind(line, nextBoxKind);
// transition이 있는지 없는지를 확인한다.
// 롤백버튼을 누를 시, transition을 무시하고, _typewriter까지 진행 한 후, rollback이 일어난다.
// 롤백버튼이 눌러진 중에는, ApplyBoxTransitionAsync에서 transition을 Immediate로만 적용한다.
        
// 그리고... rollback이나 skip이 가능한지를 여기서 플래그로 걸어야 할까?
        
        ResetDialogueBoxTransform(nextBox); // resetDialogueBox는 여기 있으면 안되고, Box의 주인이 직접 다뤄야해.
        PrepareBoxForTransition(nextBox, transitionKind);

        BindTextTargets(nextBox, line);

        var text = line.TextWithoutCharacterName;

        _typewriter.SetTextView(_lineText);
        _typewriter.PrepareForContent(text);

        await ApplyBoxTransitionAsync(
            _currentBox,
            nextBox,
            transitionKind,
            token);

        CommitCurrentBox(nextBoxKind, nextBox, transitionKind);

        await _typewriter
            .RunTypewriter(text, token.HurryUpToken)
            .SuppressCancellationThrow();

        await YarnTask
            .WaitUntilCanceled(token.NextContentToken)
            .SuppressCancellationThrow();

        _typewriter.ContentWillDismiss();
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
            _currentBoxKind,
            _isBoxVisible,
            nextBoxKind,
            line.Metadata,
            ShouldConsumeLineSilently());
    }

    private void BindTextTargets(IDialogueTextTarget nextBox, LocalizedLine line)
    {
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

    private void CommitCurrentBox(
        DialogueBoxKind nextBoxKind,
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind)
    {
        _currentBoxKind = nextBoxKind;
        _currentBox = nextBox;
        _isBoxVisible = transitionKind != DialogueBoxTransitionKind.Hide;
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
        LineCancellationToken token)
    {
        if (!useFadeEffect || ShouldConsumeLineSilently())
        {
            ApplyBoxTransitionImmediate(previousBox, nextBox, transitionKind);
            return;
        }

        switch (transitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                SetBoxVisibleImmediate(nextBox, true);
                break;

            case DialogueBoxTransitionKind.Cut:
                ApplyBoxTransitionImmediate(previousBox, nextBox, transitionKind);
                break;

            case DialogueBoxTransitionKind.FadeIn:
                await FadeInBoxAsync(nextBox, token);
                break;

            case DialogueBoxTransitionKind.FadeOutIn:
                if (previousBox != null && !ReferenceEquals(previousBox, nextBox))
                    await FadeOutBoxAsync(previousBox, token);

                SetBoxVisibleImmediate(previousBox, false);

                PrepareBoxHidden(nextBox);
                await FadeInBoxAsync(nextBox, token);
                break;

            case DialogueBoxTransitionKind.Hide:
                if (nextBox != null)
                    await FadeOutBoxAsync(nextBox, token);

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
        LineCancellationToken token)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        CanvasGroup cg = box.CanvasGroup;

        SetBoxVisibleImmediate(box, true);
        cg.alpha = 0f;

        await Effects
            .FadeAlphaAsync(cg, 0f, 1f, fadeUpDuration, token.HurryUpToken)
            .SuppressCancellationThrow();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private async YarnTask FadeOutBoxAsync(
        IDialogueTextTarget box,
        LineCancellationToken token)
    {
        if (box == null || box.CanvasGroup == null)
            return;

        CanvasGroup cg = box.CanvasGroup;

        await Effects
            .FadeAlphaAsync(cg, cg.alpha, 0f, fadeDownDuration, token.HurryUpToken)
            .SuppressCancellationThrow();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
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

        _currentBoxKind = null;
        _currentBox = null;
        _isBoxVisible = false;
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

}