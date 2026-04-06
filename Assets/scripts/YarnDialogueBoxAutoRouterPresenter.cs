using System.Collections.Generic;
using Yarn.Unity;

public sealed class YarnDialogueBoxAutoRouterPresenter : DialoguePresenterBase
{
    private YarnUIBridge _yarnUIBridge;
    private DialogueBoxRouteState _routeState;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        DialogueBoxRouteState routeState,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null)
    {
        _yarnUIBridge = yarnUIBridge;
        _routeState = routeState;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;

        List<DialoguePresenterBase> presenters = new(dialogueRunner.DialoguePresenters);
        presenters.Remove(this);

        int linePresenterIndex = presenters.FindIndex(x => x is LinePresenter);
        if (linePresenterIndex < 0)
            linePresenterIndex = presenters.Count;

        presenters.Insert(linePresenterIndex, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();

        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        DialogueBoxKind kind = _routeState.Resolve(hasCharacterName);

        _yarnUIBridge.BindAuto(kind, hasCharacterName);
        return YarnTask.CompletedTask;
    }
}