using Yarn.Unity;

public sealed class YarnCommandRegistry
{
    private readonly DialogueRunner _dialogueRunner;
    private readonly YarnUIBridge _yarnUIBridge;

    public YarnCommandRegistry(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge)
    {
        _dialogueRunner = dialogueRunner;
        _yarnUIBridge = yarnUIBridge;
    }

    private bool _init;

    public void Initialize()
    {
        if (_init) return;
        _init = true;

        RegisterYarnCommands();
    }

    private void RegisterYarnCommands()
    {
        _dialogueRunner.AddCommandHandler("protagobox", _yarnUIBridge.WithProtagonist);
        _dialogueRunner.AddCommandHandler("charbox", _yarnUIBridge.HasCharNameBox);
        _dialogueRunner.AddCommandHandler("letterbox", _yarnUIBridge.LetterBox);
        _dialogueRunner.AddCommandHandler("closeallbox", _yarnUIBridge.CloseAllDialogue);
    }
}