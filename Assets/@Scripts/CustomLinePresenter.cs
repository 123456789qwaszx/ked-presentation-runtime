using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public sealed class CustomLinePresenter : DialoguePresenterBase
{
    [Header("Fade")] public bool useFadeEffect = true;
    public float fadeUpDuration = 0.25f;
    public float fadeDownDuration = 0.1f;

    private DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private IDialogueBoxViewResolver _dialogueBoxResolver;
    private DialogueTextRouter _dialogueTextRouter;
    private EllipsisBreathTypewriter _typewriter;
    private PresentationSessionContext _context;
    private AudioSystem _audioSystem;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;

    private TMP_Text _lineText;
    private TMP_Text _characterNameText;
    private GameObject _characterNameContainer;
    private CanvasGroup _canvasGroup;

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

        // ── 3. 박스 타겟 Resolve / Bind ─────────────────────────────────
        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        DialogueBoxKind boxKind = _lineRoutingPolicy.Resolve(hasCharacterName);

        IDialogueTextTarget box = _dialogueBoxResolver.ResolveTarget(boxKind);
        if (box == null)
        {
            Debug.LogError(
                $"{nameof(CustomLinePresenter)}: failed to resolve dialogue box {boxKind} for line {line.TextID}.");
            return;
        }

        ResetDialogueBoxTransform(box);
        
        _dialogueTextRouter.Bind(box);

        _lineText = _dialogueTextRouter.LineText;
        _canvasGroup = box.CanvasGroup;

        if (_lineText == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: lineText is null. Skipping {line.TextID}.");
            return;
        }

        BindCharacterNameTarget(hasCharacterName);

        // ── 4. 표시할 텍스트 결정 ────────────────────────────────────────
        var text = _characterNameText != null
            ? line.TextWithoutCharacterName
            : line.Text;

        ApplyCharacterName(line);

        // ── 5. 타입라이터 준비 ───────────────────────────────────────────
        _typewriter.SetTextView(_lineText);
        _typewriter.PrepareForContent(text);

        // ── 6. Rollback 복원 중이면 여기서 종료 ──────────────────────────
        if (!ShouldConsumeLineSilently())
        {
            // ── 7. 이제 실제로 박스를 보여준다 ───────────────────────────────
            _dialogueBoxResolver.ShowOnly(box);
            await FadeInAsync(token);
        }

        // ── 8. 타입라이터 실행 ───────────────────────────────────────────
        await _typewriter
            .RunTypewriter(text, token.HurryUpToken)
            .SuppressCancellationThrow();

        // ── 9. 입력 대기 ─────────────────────────────────────────────────
        await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();

        _typewriter.ContentWillDismiss();
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

    private async YarnTask FadeInAsync(LineCancellationToken token)
    {
        if (_canvasGroup == null)
            return;

        if (!useFadeEffect || ShouldConsumeLineSilently())
        {
            _canvasGroup.alpha = 1f;
            return;
        }

        _canvasGroup.alpha = 0f;

        await Effects
            .FadeAlphaAsync(_canvasGroup, 0f, 1f, fadeUpDuration, token.HurryUpToken)
            .SuppressCancellationThrow();
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