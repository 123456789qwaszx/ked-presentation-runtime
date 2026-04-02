using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// YarnCommandBridge가 수집한 Spec을 CommandExecutor로 넘기는 드라이버.
/// Bridge는 수집만, Executor는 실행만 — 이 클래스가 둘을 연결.
/// </summary>
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

    /// <summary>
    /// Bridge 버퍼를 소비해서 즉시 재생.
    /// Yarn command 처리 직후 또는 line 시작 시점에 호출.
    /// </summary>
    public void PlayCollected()
    {
        List<CommandSpecBase> specs = _bridge.ConsumeCollectedSpecs();
        if (specs == null || specs.Count == 0)
            return;

        _executor.PlaySpecs(specs, _scope, "yarn-bridge");
    }
}