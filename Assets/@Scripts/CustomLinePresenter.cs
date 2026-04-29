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
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null,
        AudioSystem audioSystem = null)
    {
        _lineRoutingPolicy = lineRoutingPolicy;
        _dialogueBoxResolver = dialogueBoxResolver;
        _dialogueTextRouter = dialogueTextRouter;
        _typewriter = typewriter;
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
        _audioSystem?.Voice.Stop();

        if (line.Asset is AudioClip clip)
            _audioSystem?.Voice.Play(clip);

        // ── 2. 재생 드라이버 ─────────────────────────────────────────────
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();

        // ── 3. 박스 바인딩 ───────────────────────────────────────────────
        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        DialogueBoxKind boxKind = _lineRoutingPolicy.Resolve(hasCharacterName);

        IDialogueTextTarget box = _dialogueBoxResolver.Activate(boxKind);
        if (box == null)
        {
            Debug.LogError($"{nameof(CustomLinePresenter)}: failed to activate dialogue box {boxKind} for line {line.TextID}.");
            return;
        }

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

        // ── 5. 타입라이터 바인딩 ─────────────────────────────────────────
        _typewriter.SetTextView(_lineText);
        _typewriter.PrepareForContent(text);

        // ── 6. 페이드 인 ─────────────────────────────────────────────────
        // if (_canvasGroup != null)
        // {
        //     if (useFadeEffect)
        //         await Effects.FadeAlphaAsync(_canvasGroup, 0, 1, fadeUpDuration, token.HurryUpToken).SuppressCancellationThrow();
        //     else
        //         _canvasGroup.alpha = 1;
        // }

        // ── 7. 타입라이터 실행 ───────────────────────────────────────────
        await _typewriter
            .RunTypewriter(text, token.HurryUpToken)
            .SuppressCancellationThrow();

        // ── 8. 자동 진행 or 입력 대기 ────────────────────────────────────
        // if (autoAdvance)
        //     await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextContentToken).SuppressCancellationThrow();
        // else
            await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();

        // Yarn IAsyncTypewriter 계약상, 실제로 숨기기 직전에 호출한다.
        _typewriter.ContentWillDismiss();

        // ── 9. 페이드 아웃 ───────────────────────────────────────────────
        // if (_canvasGroup != null)
        // {
        //     if (useFadeEffect)
        //         await Effects.FadeAlphaAsync(_canvasGroup, 1, 0, fadeDownDuration, token.HurryUpToken).SuppressCancellationThrow();
        //     else
        //         _canvasGroup.alpha = 0;
        // }
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
}