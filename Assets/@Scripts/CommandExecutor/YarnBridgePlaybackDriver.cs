using System.Collections.Generic;
using UnityEngine;

public sealed class YarnBridgePlaybackDriver : MonoBehaviour
{
    private YarnCommandBridge _bridge;
    private CommandExecutor _executor;
    private CommandRunScope _scope;

    public void Initialize(
        YarnCommandBridge bridge,
        CommandExecutor executor,
        PresentationPlaybackSettings settings)
    {
        _bridge = bridge;
        _executor = executor;
        PresentationSessionContext context = new (settings);
        _scope = new CommandRunScope(context);
    }

    // Bridge 버퍼를 소비해서 즉시 재생.
    // Yarn command 처리 직후 또는 line 시작 시점에 호출.
    public void PlayCollected()
    {
        List<CommandSpecBase> specs = _bridge.ConsumeCollectedSpecs();

        _executor.PlaySpecs(specs, _scope, "yarn-bridge");
    }

    public void ClearCollected()
    {
        _bridge.ClearCollectedSpecs();
    }
}