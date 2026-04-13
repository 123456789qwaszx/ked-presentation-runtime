using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public sealed class MessengerDialoguePresenter : DialoguePresenterBase
{
    [Header("Prefabs")]
    [SerializeField] private SerializableDictionary<string, MessengerBubbleView> characters = new();
    [SerializeField] private MessengerBubbleView defaultBubblePrefab;
    [SerializeField] private MessengerOptionButtonView optionsButtonPrefab;

    [Header("Containers")]
    [SerializeField] private RectTransform bubbleContainer;
    [SerializeField] private RectTransform optionsContainer;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Timing")]
    [SerializeField] private float delayAfterLine = 1f;
    [SerializeField] private float minimumTypingDelay = 0.5f;
    [SerializeField] private float maximumTypingDelay = 3f;
    [SerializeField] private float typingDelayPerCharacter = 0.05f;
    [SerializeField] private bool showTypingIndicators = true;

    private readonly List<GameObject> _spawnedBubbles = new();
    private readonly List<GameObject> _spawnedOptions = new();

    public override YarnTask OnDialogueStartedAsync()
    {
        ClearBubbles();
        ClearOptions();

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        ClearOptions();

        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (bubbleContainer == null)
        {
            Debug.LogWarning($"[MessengerDialoguePresenter] Can't show line '{line.Text.Text}': bubbleContainer is null.", this);
            return;
        }

        MessengerBubbleView prefab = ResolveBubblePrefab(line.CharacterName);
        if (prefab == null)
        {
            Debug.LogWarning($"[MessengerDialoguePresenter] Can't show line '{line.Text.Text}': no bubble prefab found.", this);
            return;
        }

        int siblingIndex = GetBubbleInsertIndex();

        string speaker = line.CharacterName ?? "";
        string text = line.TextWithoutCharacterName.Text ?? "";

        if (showTypingIndicators && prefab.HasIndicator)
        {
            MessengerBubbleView typingBubble = Instantiate(prefab, bubbleContainer);
            typingBubble.transform.SetSiblingIndex(siblingIndex);
            typingBubble.ShowTyping(speaker);

            _spawnedBubbles.Add(typingBubble.gameObject);
            ScrollToBottom();

            float typingDelay = Mathf.Clamp(
                text.Length * typingDelayPerCharacter,
                minimumTypingDelay,
                maximumTypingDelay);

            await YarnTask.Delay(
                TimeSpan.FromSeconds(typingDelay),
                token.HurryUpToken).SuppressCancellationThrow();

            RemoveBubble(typingBubble);
        }

        MessengerBubbleView bubble = Instantiate(prefab, bubbleContainer);
        bubble.transform.SetSiblingIndex(siblingIndex);
        bubble.ShowText(speaker, text);

        _spawnedBubbles.Add(bubble.gameObject);
        ScrollToBottom();

        await YarnTask.Delay(
            TimeSpan.FromSeconds(delayAfterLine),
            token.HurryUpToken).SuppressCancellationThrow();
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken)
    {
        if (optionsContainer == null)
        {
            Debug.LogWarning("[MessengerDialoguePresenter] Can't show options: optionsContainer is null.", this);
            return null;
        }

        if (optionsButtonPrefab == null)
        {
            Debug.LogWarning("[MessengerDialoguePresenter] Can't show options: optionsButtonPrefab is null.", this);
            return null;
        }

        ClearOptions();

        var completionSource = new YarnTaskCompletionSource<DialogueOption>();

        foreach (DialogueOption option in dialogueOptions)
        {
            MessengerOptionButtonView button = Instantiate(optionsButtonPrefab, optionsContainer);
            button.SetText(option.Line.TextWithoutCharacterName.Text);
            button.SetOnClick(() =>
            {
                LockOptions();
                completionSource.TrySetResult(option);
            });

            _spawnedOptions.Add(button.gameObject);
        }

        ScrollToBottom();

        DialogueOption selectedOption = await completionSource.Task;

        ClearOptions();
        return selectedOption;
    }

    private MessengerBubbleView ResolveBubblePrefab(string characterName)
    {
        if (!string.IsNullOrEmpty(characterName) &&
            characters != null &&
            characters.TryGetValue(characterName, out MessengerBubbleView characterBubble) &&
            characterBubble != null)
        {
            return characterBubble;
        }

        return defaultBubblePrefab;
    }

    private int GetBubbleInsertIndex()
    {
        if (optionsContainer != null)
            return optionsContainer.GetSiblingIndex();

        if (bubbleContainer == null)
            return 0;

        return bubbleContainer.childCount;
    }

    private void ClearBubbles()
    {
        for (int i = 0; i < _spawnedBubbles.Count; i++)
        {
            GameObject go = _spawnedBubbles[i];
            if (go != null)
                Destroy(go);
        }

        _spawnedBubbles.Clear();
        ScrollToBottom();
    }

    private void ClearOptions()
    {
        for (int i = 0; i < _spawnedOptions.Count; i++)
        {
            GameObject go = _spawnedOptions[i];
            if (go != null)
                Destroy(go);
        }

        _spawnedOptions.Clear();
        ScrollToBottom();
    }

    private void LockOptions()
    {
        for (int i = 0; i < _spawnedOptions.Count; i++)
        {
            GameObject go = _spawnedOptions[i];
            if (go == null)
                continue;

            MessengerOptionButtonView button = go.GetComponent<MessengerOptionButtonView>();
            if (button != null)
                button.SetInteractable(false);
        }
    }

    private void RemoveBubble(MessengerBubbleView bubble)
    {
        if (bubble == null)
            return;

        _spawnedBubbles.Remove(bubble.gameObject);
        Destroy(bubble.gameObject);
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}