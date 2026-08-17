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

    public async Task StopAsync() => await _dialogueRunner.Stop();

    public async Task StartAsync(string nodeName) => await _dialogueRunner.StartDialogue(nodeName);
}
