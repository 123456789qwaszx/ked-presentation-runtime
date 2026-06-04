using System.Collections;
using Yarn.Unity;

// 노드 하나를 처음부터 끝까지 실행하고 멈추는 단발 실행기.
// command-only 노드를 돌려 커맨드를 모은 뒤, 한 번 flush하고 entry까지 (메인을) 블로킹한다.
public sealed class OneShotPresentationLane
{
    private readonly DialogueRunner _runner;
    private readonly YarnBridgePlaybackDriver _driver;

    public OneShotPresentationLane(DialogueRunner runner, YarnBridgePlaybackDriver driver)
    {
        _runner = runner;
        _driver = driver;
    }

    public IEnumerator RunNodeCoroutine(string nodeName)
    {
        if (_runner == null || string.IsNullOrEmpty(nodeName))
            yield break;

        if (_runner.IsDialogueRunning)               // 직전 one-shot 잔여 정리
        {
            YarnTask stopTask = _runner.Stop();
            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        _driver.Clear();                              // 잔여 spec 방어 제거

        YarnTask startTask = _runner.StartDialogue(nodeName);
        while (!startTask.IsCompletedSuccessfully())
            yield return null;

        // 노드의 모든 <<command>>가 수집될 때까지(=노드 종료) 대기.
        // command-only라 RunLineAsync로 멈추지 않으므로 곧바로 끝난다.
        while (_runner.IsDialogueRunning)
            yield return null;

        // 한 번에 flush → one-shot executor 실행. entry 적용까지 대기(메인 블로킹).
        CommandRunTicket ticket = _driver.PlayCollected();
        while (ticket != null && !ticket.EntryClosed)
            yield return null;
    }
}