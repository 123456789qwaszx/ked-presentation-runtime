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
    private AudioSystem _audioSystem;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;

    private readonly DialogueBoxTransitionPolicy _boxTransitionPolicy = new();

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
        PresentationSessionContext context,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null,
        AudioSystem audioSystem = null)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueTextRouter = dialogueTextRouter;
        _typewriter = typewriter;
        _context = context;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _audioSystem = audioSystem;

        if (dialogueRunner == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: dialogueRunner is null.");
            return;
        }

        List<DialoguePresenterBase> presenters = new(dialogueRunner.DialoguePresenters);
        presenters.Remove(this);

        int insertIndex = presenters.FindIndex(x => x is LinePresenter);
        if (insertIndex < 0)
            insertIndex = presenters.Count;

        presenters.Insert(insertIndex, this);
        dialogueRunner.DialoguePresenters = presenters;
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
        if (IsReadyForLine(line) == false)
            return;

        // ── 1. 오디오 ────────────────────────────────────────────────────
        if (!ShouldConsumeLineSilently())
        {
            _audioSystem?.Voice.Stop();

            if (line.Asset is AudioClip clip)
                _audioSystem?.Voice.Play(clip);
        }

        // ── 2. 재생 드라이버 ─────────────────────────────────────────────
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();

        // ── 3. 박스 종류 결정 ─────────────────────────────────────────────
        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;

        DialogueBoxKind nextBoxKind = TryResolveBoxKindFromMetadata(
            line.Metadata,
            out DialogueBoxKind metadataBoxKind)
                ? metadataBoxKind
                : _lineRoutingPolicy.Resolve(hasCharacterName);

        DialogueBoxTransitionKind transitionKind = _boxTransitionPolicy.Resolve(
            _currentBoxKind,
            _isBoxVisible,
            nextBoxKind,
            line.Metadata,
            ShouldConsumeLineSilently());

        IDialogueTextTarget nextBox = _dialogueBoxResolver.ResolveTarget(nextBoxKind);
        if (nextBox == null)
        {
            Debug.LogError(
                $"{nameof(CustomLinePresenter)}: failed to resolve dialogue box {nextBoxKind} for line {line.TextID}.");
            return;
        }

        ResetDialogueBoxTransform(nextBox);

        // ── 4. Transition 준비 ───────────────────────────────────────────
        PrepareBoxForTransition(nextBox, transitionKind);

        // ── 5. 박스 Bind ─────────────────────────────────────────────────
        _dialogueTextRouter.Bind(nextBox);

        _lineText = _dialogueTextRouter.LineText;
        _canvasGroup = nextBox.CanvasGroup;

        if (_lineText == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: lineText is null. Skipping {line.TextID}.");
            return;
        }

        BindCharacterNameTarget(hasCharacterName);

        // ── 6. 표시할 텍스트 결정 ────────────────────────────────────────
        var text = line.TextWithoutCharacterName;

        ApplyCharacterName(line);

        // ── 7. 타입라이터 준비 ───────────────────────────────────────────
        _typewriter.SetTextView(_lineText);
        _typewriter.PrepareForContent(text);

        // ── 8. DialogueBox 전환 실행 ─────────────────────────────────────
        await ApplyBoxTransitionAsync(
            _currentBox,
            nextBox,
            transitionKind,
            token);

        _currentBoxKind = nextBoxKind;
        _currentBox = nextBox;
        _isBoxVisible = transitionKind != DialogueBoxTransitionKind.Hide;

        // ── 9. 타입라이터 실행 ───────────────────────────────────────────
        await _typewriter
            .RunTypewriter(text, token.HurryUpToken)
            .SuppressCancellationThrow();

        // ── 10. 입력 대기 ────────────────────────────────────────────────
        await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();

        _typewriter.ContentWillDismiss();
    }

    private void PrepareBoxForTransition(
        IDialogueTextTarget nextBox,
        DialogueBoxTransitionKind transitionKind)
    {
        DialogueBoxHost host = _dialogueBoxResolver as DialogueBoxHost;

        switch (transitionKind)
        {
            case DialogueBoxTransitionKind.Keep:
                // 같은 박스 유지. 아무것도 숨기지 않는다.
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
                // 이전 박스는 ApplyBoxTransitionAsync에서 FadeOut한다.
                // 여기서는 새 박스를 아직 보이지 않게 준비만 한다.
                PrepareBoxHidden(nextBox);
                break;

            case DialogueBoxTransitionKind.Hide:
                // 이 라인을 숨김 처리하는 것은 특수 케이스.
                // line 표시 흐름에서는 거의 쓰지 않는 것을 권장.
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

        // Fallback.
        // 현재 resolver가 Host가 아니면 전체 Hide 후 keep만 다시 켜는 식으로 처리.
        _dialogueBoxResolver.HideAll();

        if (keep != null)
            SetBoxVisibleImmediate(keep, true);
    }

    private bool IsReadyForLine(LocalizedLine line)
    {
        if (_lineRoutingPolicy == null ||
            _dialogueBoxResolver == null ||
            _dialogueTextRouter == null ||
            _typewriter == null)
        {
            string lineId = line != null ? line.TextID : "(null line)";
            Debug.LogError($"{nameof(CustomLinePresenter)} is not initialized correctly. Skipping {lineId}.");
            return false;
        }

        return true;
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

    private void ApplyCharacterName(LocalizedLine line)
    {
        if (_characterNameContainer == null)
            return;

        bool showName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        _characterNameContainer.SetActive(showName);

        if (showName && _characterNameText != null)
            _characterNameText.text = line.CharacterName;
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

    private static bool TryResolveBoxKindFromMetadata(
        string[] metadata,
        out DialogueBoxKind kind)
    {
        kind = default;

        if (metadata == null || metadata.Length == 0)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            string tag = metadata[i];

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            tag = tag.Trim().ToLowerInvariant();

            switch (tag)
            {
                case "portrait":
                case "box:portrait":
                case "box=portrait":
                    kind = DialogueBoxKind.Portrait;
                    return true;

                case "speaker":
                case "box:speaker":
                case "box=speaker":
                    kind = DialogueBoxKind.Speaker;
                    return true;

                case "letterbox":
                case "letter_box":
                case "box:letterbox":
                case "box=letterbox":
                    kind = DialogueBoxKind.LetterBox;
                    return true;

                case "onlytext":
                case "only_text":
                case "box:onlytext":
                case "box=onlytext":
                    kind = DialogueBoxKind.OnlyText;
                    return true;
            }
        }

        return false;
    }
}