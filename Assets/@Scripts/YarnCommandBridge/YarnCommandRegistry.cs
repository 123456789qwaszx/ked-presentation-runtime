using System;
using System.Collections;
using Yarn.Unity;

public sealed class YarnCommandRegistry
{
    private const string BeatKey = "beat";
    
    private readonly DialogueRunner _dialogueRunner;
    private readonly YarnUIBridge _yarnUIBridge;
    private readonly VnRuntimeBridge _vnRuntimeBridge;
    private readonly DialogueBoxLineRoutingPolicy _routeState;
    

    public YarnCommandRegistry(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        VnRuntimeBridge vnRuntimeBridge,
        DialogueBoxLineRoutingPolicy routeState)
    {
        _dialogueRunner = dialogueRunner;
        _yarnUIBridge = yarnUIBridge;
        _vnRuntimeBridge = vnRuntimeBridge;
        _routeState = routeState;
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
        
        _dialogueRunner.AddCommandHandler<string>("set_named_box", SetNamedBox);
        _dialogueRunner.AddCommandHandler<string>("set_narration_box", SetProtagonistBox);
        _dialogueRunner.AddCommandHandler("closebox", _yarnUIBridge.CloseAllDialogue);
    }
    
    private void SetNamedBox(string key)
    {
        if (TryParseKind(key, out DialogueBoxKind kind))
            _routeState.SetNamedLineBoxKind(kind);
    }

    private void SetProtagonistBox(string key)
    {
        if (TryParseKind(key, out DialogueBoxKind kind))
            _routeState.SetProtagonistLineBoxKind(kind);
    }

    private bool TryParseKind(string key, out DialogueBoxKind kind)
    {
        return Enum.TryParse(key, true, out kind);
    }
}