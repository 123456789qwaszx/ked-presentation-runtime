using Yarn.Unity;
using System.Collections.Generic;

// Line 단위 런타임 훅 계층.
// DialoguePresenterBase를 상속해 Yarn line lifecycle에 직접 연결.
// ResetImmediateWaitForNewLine 같은 line 단위 잡일을 처리.
// 나중에 사운드/카메라/세이브 등 line 단위 후처리도 이 층에 추가.
public sealed class YarnLineRuntimePresenter : DialoguePresenterBase
{
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;

    public void Initialize(
        DialogueRunner dialogueRunner,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver)
    {
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;

        List<DialoguePresenterBase> presenters = new(dialogueRunner.DialoguePresenters);
        if (!presenters.Contains(this))
            presenters.Add(this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();
        
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
    public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;
}