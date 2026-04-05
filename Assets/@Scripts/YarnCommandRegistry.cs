using System;
using System.Collections;
using Yarn.Unity;

public sealed class YarnCommandRegistry
{
    private const string BeatKey = "beat";
    
    private readonly DialogueRunner _dialogueRunner;
    private readonly YarnUIBridge _yarnUIBridge;
    private readonly VnRuntimeBridge _vnRuntimeBridge;
    private readonly DialogueBoxRouteState _routeState;
    

    public YarnCommandRegistry(
        DialogueRunner dialogueRunner,
        YarnUIBridge yarnUIBridge,
        VnRuntimeBridge vnRuntimeBridge,
        DialogueBoxRouteState routeState)
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
        _dialogueRunner.AddCommandHandler<string>("set_narration_box", SetNarrationBox);
        _dialogueRunner.AddCommandHandler("closebox", _yarnUIBridge.CloseAllDialogue);
    }
    
    private void SetNamedBox(string key)
    {
        if (TryParseKind(key, out DialogueBoxKind kind))
            _routeState.SetNamedBox(kind);
    }

    private void SetNarrationBox(string key)
    {
        if (TryParseKind(key, out DialogueBoxKind kind))
            _routeState.SetNarrationBox(kind);
    }

    private bool TryParseKind(string key, out DialogueBoxKind kind)
    {
        return System.Enum.TryParse(key, true, out kind);
    }
}