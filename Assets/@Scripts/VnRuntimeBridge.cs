using System.Collections;
using Yarn.Unity;


public sealed class VnRuntimeBridge
{
    public interface ICpsSignalBridge
    {
        IEnumerator Beat(string beatKey); 
        IEnumerator WaitSignal(string key);
    }
    

    private readonly DialogueRunner _runner;
    private readonly PresentationSessionEntry _cpsRouteEntry;
    private readonly ICpsSignalBridge _cpsSignalBridge;
    
    public VnRuntimeBridge(
        DialogueRunner runner,
        PresentationSessionEntry cpsRouteEntry,
        ICpsSignalBridge cpsSignalBridge)
    {
        _runner = runner;
        _cpsRouteEntry = cpsRouteEntry;
        _cpsSignalBridge = cpsSignalBridge;
    }

    public IEnumerator Beat(string beatKey)
    {
        if (_cpsSignalBridge == null) yield break;
        
        yield return _cpsSignalBridge.Beat(beatKey);
    }

    public IEnumerator WaitSignal(string key) => _cpsSignalBridge?.WaitSignal(key);
}