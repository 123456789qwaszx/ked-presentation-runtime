using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnDialogueBoxAutoRouterPresenter : DialoguePresenterBase
{
    private YarnUIBridge _yarnUIBridge;
    private DialogueBoxRouteState _routeState;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;
    private AudioSystem _audioSystem;
    private InlineEmojiHost _inlineEmojiHost;

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        DialogueBoxRouteState routeState,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null,
        AudioSystem audioSystem = null,
        InlineEmojiHost inlineEmojiHost = null)
    {
        _yarnUIBridge = yarnUIBridge;
        _routeState = routeState;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _audioSystem = audioSystem;
        _inlineEmojiHost = inlineEmojiHost;

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
        _audioSystem?.Voice.Stop();
        
        //Debug.Log($"[Voice] lineId={line.TextID}, asset={line.Asset}");
        if (line.Asset is AudioClip clip)
        {
            _audioSystem?.Voice.Play(clip);
        }
        
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();

        bool hasCharacterName = string.IsNullOrWhiteSpace(line.CharacterName) == false;
        DialogueBoxKind kind = _routeState.Resolve(hasCharacterName);

       _yarnUIBridge.BindAuto(kind, hasCharacterName);
        return YarnTask.CompletedTask;
    }
}