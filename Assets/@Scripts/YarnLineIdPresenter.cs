using System;
using Yarn.Unity;

// presenter that captures the current line's TextID
// Used by YarnLineLifecycleBridge, and (when autoRegisterPresenter is enabled)
// automatically inserted at the front of DialogueRunner.DialoguePresenters.
public sealed class YarnLineIdPresenter : DialoguePresenterBase
{
    public event Action<string> OnLineIdReceived;

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string lineId = line.TextID;
        OnLineIdReceived?.Invoke(lineId);
        return YarnTask.CompletedTask;
    }
}