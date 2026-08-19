using System.Collections.Generic;
using UnityEngine;

public sealed class YarnPlaybackDriver : MonoBehaviour
{
    private CommandExecutor _executor;
    private ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    public void Initialize(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
    {
        _executor = executor;
        _scopeProvider = scopeProvider;
    }
    
    public void Enqueue(CommandSpecBase spec) => _collectedSpecs.Add(spec);
    
    public void PlayCollected()
    {
        var specs = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();

        if (specs.Count == 0)
            CurrentScope?.CleanupStep(CleanupPolicy.Finish);

        _executor.PlaySpecs(specs, CurrentScope);
    }
}