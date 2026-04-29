using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnLineSetupPresenter : DialoguePresenterBase
{
    private YarnUIBridge _yarnUIBridge;
    private DialogueBoxLineRoutingPolicy _lineRoutingPolicy;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;
    private AudioSystem _audioSystem;
    private InlineEmojiHost _inlineEmojiHost;

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        DialogueBoxLineRoutingPolicy lineRoutingPolicy,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null,
        AudioSystem audioSystem = null,
        InlineEmojiHost inlineEmojiHost = null)
    {
        _yarnUIBridge = yarnUIBridge;
        _lineRoutingPolicy = lineRoutingPolicy;
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

        DialogueBoxKind boxKind = _lineRoutingPolicy.ResolveBoxKind(line, 
            out bool isNamedLine);

        _yarnUIBridge.BindAuto(boxKind, isNamedLine);
        return YarnTask.CompletedTask;
    }
}