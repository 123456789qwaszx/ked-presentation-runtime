using System;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnLineIdPresenter : DialoguePresenterBase
{
    public event Action<string> OnLineIdReceived;
    public event Action<string> OnCharacterKeyReceived;
    public event Action<LocalizedLine> LineEntered;

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string lineId = line != null ? line.TextID : string.Empty;
        OnLineIdReceived?.Invoke(lineId);

        string characterKey = ResolveCharacterKey(line);
        OnCharacterKeyReceived?.Invoke(characterKey);

        LineEntered?.Invoke(line);

        return YarnTask.CompletedTask;
    }

    private string ResolveCharacterKey(LocalizedLine line)
    {
        if (line == null)
            return string.Empty;

        return line.CharacterName ?? string.Empty;
    }
}