using System.Collections;
using Yarn.Unity;

// 노드 하나를 처음부터 끝까지 실행하고 멈추는 단발 실행기.
// command-only 노드를 돌려 커맨드를 모은 뒤, 한 번 flush하고 entry까지 (메인을) 블로킹한다.
public sealed class OneShotPresentationLane
{
    private readonly DialogueRunner _oneShotDialogueRunner;
    private readonly YarnBridgePlaybackDriver _oneShotPlaybackDriver;

    public OneShotPresentationLane(DialogueRunner oneShotDliaogueRunner, YarnBridgePlaybackDriver oneShotPlaybackDriver)
    {
        _oneShotDialogueRunner = oneShotDliaogueRunner;
        _oneShotPlaybackDriver = oneShotPlaybackDriver;
    }

    public IEnumerator RunNodeCoroutine(string nodeName)
    {
        if (_oneShotDialogueRunner == null || string.IsNullOrEmpty(nodeName))
            yield break;

        if (_oneShotDialogueRunner.IsDialogueRunning)               // 직전 one-shot 잔여 정리
        {
            YarnTask stopTask = _oneShotDialogueRunner.Stop();
            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        _oneShotPlaybackDriver.Clear();                              // 잔여 spec 방어 제거

        YarnTask startTask = _oneShotDialogueRunner.StartDialogue(nodeName);
        while (!startTask.IsCompletedSuccessfully())
            yield return null;

        // 노드의 모든 <<command>>가 수집될 때까지(=노드 종료) 대기.
        // command-only라 RunLineAsync로 멈추지 않으므로 곧바로 끝난다.
        while (_oneShotDialogueRunner.IsDialogueRunning)
            yield return null;

        // 한 번에 flush → one-shot executor 실행. entry 적용까지 대기(메인 블로킹).
        CommandRunTicket ticket = _oneShotPlaybackDriver.PlayCollected();
        while (ticket != null && !ticket.EntryClosed)
            yield return null;
    }
}