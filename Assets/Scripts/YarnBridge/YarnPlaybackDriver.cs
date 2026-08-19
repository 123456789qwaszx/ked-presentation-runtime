using System.Collections.Generic;

public sealed class YarnPlaybackDriver
{
    private CommandExecutor _executor;
    private ICommandRunScopeProvider _scopeProvider;

    private readonly List<CommandSpecBase> _collectedSpecs = new();

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    public YarnPlaybackDriver(CommandExecutor executor, ICommandRunScopeProvider scopeProvider)
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