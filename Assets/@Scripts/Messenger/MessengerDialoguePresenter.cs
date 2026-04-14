using System;
using UnityEngine;
using Yarn.Unity;

public sealed class MessengerDialoguePresenter : DialoguePresenterBase
{
    [Header("Refs")]
    [SerializeField] private DmThreadPanel _panel;

    [Header("Prefabs")]
    [SerializeField] private SerializableDictionary<string, MessengerBubbleView> _characters = new();
    [SerializeField] private MessengerBubbleView _defaultBubblePrefab;
    [SerializeField] private MessengerOptionButtonView _optionsButtonPrefab;

    [Header("Timing")]
    [SerializeField] private float _delayAfterLine = 1f;
    [SerializeField] private float _minimumTypingDelay = 0.5f;
    [SerializeField] private float _maximumTypingDelay = 3f;
    [SerializeField] private float _typingDelayPerCharacter = 0.05f;
    [SerializeField] private bool _showTypingIndicators = true;
    
    [Header("Policy")]
    [SerializeField] private bool _clearOnStart = true;

    public override YarnTask OnDialogueStartedAsync()
    {
        if (_panel == null)
        {
            Debug.LogWarning("[MessengerDialoguePresenter] DmThreadPanel is null.", this);
            return YarnTask.CompletedTask;
        }

        if (_clearOnStart)
            _panel.ClearBubbles();

        _panel.ClearOptions();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        if (_panel == null)
        {
            Debug.LogWarning("[MessengerDialoguePresenter] DmThreadPanel is null.", this);
            return YarnTask.CompletedTask;
        }

        _panel.ClearOptions();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        MessengerBubbleView prefab = ResolveBubblePrefab(line.CharacterName);

        string speaker = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text ?? "";

        if (_showTypingIndicators && prefab.HasIndicator)
        {
            MessengerBubbleView typingBubble = _panel?.AppendTypingBubble(prefab, speaker);
            _panel?.ScrollToBottom();

            float typingDelay = Mathf.Clamp(
                text.Length * _typingDelayPerCharacter,
                _minimumTypingDelay,
                _maximumTypingDelay);

            await YarnTask.Delay(
                TimeSpan.FromSeconds(typingDelay),
                token.HurryUpToken).SuppressCancellationThrow();

            _panel?.RemoveBubble(typingBubble);
        }

        _panel?.AppendBubble(prefab, speaker, text);
        _panel?.ScrollToBottom();

        await YarnTask.Delay(
            TimeSpan.FromSeconds(_delayAfterLine),
            token.NextContentToken).SuppressCancellationThrow();
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        _panel.ClearOptions();

        var completionSource = new YarnTaskCompletionSource<DialogueOption>();

        foreach (DialogueOption option in dialogueOptions)
        {
            _panel.AppendOption(
                _optionsButtonPrefab,
                option.Line.TextWithoutCharacterName.Text,
                () =>
                {
                    _panel.LockOptions();
                    completionSource.TrySetResult(option);
                });
        }

        _panel.ScrollToBottom();

        DialogueOption selectedOption = await completionSource.Task;

        _panel.ClearOptions();
        return selectedOption;
    }

    private MessengerBubbleView ResolveBubblePrefab(string characterName)
    {
        if (!string.IsNullOrEmpty(characterName) &&
            _characters != null &&
            _characters.TryGetValue(characterName, out MessengerBubbleView characterBubble) &&
            characterBubble != null)
        {
            return characterBubble;
        }

        return _defaultBubblePrefab;
    }
}