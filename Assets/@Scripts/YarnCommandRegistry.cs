using System;
using System.Collections;
using Yarn.Unity;

public sealed class YarnCommandRegistry
{
    private const string BeatKey = "beat";
    
    private readonly DialogueRunner _dialogueRunner;
    private readonly YarnUIBridge _yarnUIBridge;
    private readonly VnRuntimeBridge _vnRuntimeBridge;
    

    public YarnCommandRegistry(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        VnRuntimeBridge vnRuntimeBridge)
    {
        _dialogueRunner = dialogueRunner;
        _yarnUIBridge = yarnUIBridge;
        _vnRuntimeBridge = vnRuntimeBridge;
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
        
        _dialogueRunner.AddCommandHandler("beat", (Func<IEnumerator>)(() => _vnRuntimeBridge.Beat(BeatKey)));
        _dialogueRunner.AddCommandHandler<string>("WaitSignal", key => _vnRuntimeBridge.WaitSignal(key));
        
        _dialogueRunner.AddCommandHandler("box_speaker", _yarnUIBridge.WithProtagonist);
        _dialogueRunner.AddCommandHandler("box_name", _yarnUIBridge.HasCharNameBox);
        _dialogueRunner.AddCommandHandler("box_letter", _yarnUIBridge.LetterBox);
        _dialogueRunner.AddCommandHandler("box_onlytext", _yarnUIBridge.OnlyText);
        _dialogueRunner.AddCommandHandler("closebox", _yarnUIBridge.CloseAllDialogue);
    }
}