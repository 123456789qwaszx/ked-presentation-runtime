using System.Threading.Tasks;
using Yarn.Unity;

public interface IEpisodeNodeRunner
{
    bool IsRunning { get; }

    Task StopAsync();

    Task StartAsync(string nodeName);
}

public sealed class YarnEpisodeNodeRunner : IEpisodeNodeRunner
{
    private readonly DialogueRunner _dialogueRunner;

    public YarnEpisodeNodeRunner(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;
    }

    public bool IsRunning => _dialogueRunner.IsDialogueRunning;

    public async Task StopAsync()
    {
        await _dialogueRunner.Stop();
    }

    public async Task StartAsync(string nodeName)
    {
        await _dialogueRunner.StartDialogue(nodeName);
        await _dialogueRunner.DialogueTask;

        // 스택 탈출용. 지우지 말 것.
        // Yarn의 입력 대기 없이 SeekPassThrough로 Line을 넘겼기에, Yarn내부 순서 꼬인걸 푸는 용도.
        await YarnTask.Yield();
    }
}