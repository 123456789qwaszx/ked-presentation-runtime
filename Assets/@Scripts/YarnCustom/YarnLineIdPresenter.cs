using System;
using UnityEngine;
using Yarn.Unity;

// presenter that captures the current line's TextID
// Used by YarnLineLifecycleBridge, and (when autoRegisterPresenter is enabled)
// automatically inserted at the front of DialogueRunner.DialoguePresenters.
public sealed class YarnLineIdPresenter : DialoguePresenterBase
{
    public event Action<string> OnLineIdReceived;
    public event Action<string> OnCharacterKeyReceived;

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;
    
    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string lineId = line.TextID;
        OnLineIdReceived?.Invoke(lineId);

        string characterKey = ResolveCharacterKey(line);
        OnCharacterKeyReceived?.Invoke(characterKey);

        return YarnTask.CompletedTask;
    }

    private string ResolveCharacterKey(LocalizedLine line)
    {
        if (line == null)
            return string.Empty;

        string key = line.CharacterName;
        return key;
    }
}