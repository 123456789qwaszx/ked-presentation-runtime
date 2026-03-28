using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed class VnRuntimeBridge : MonoBehaviour
{
    public interface IPresentationSignalBridge
    {
        IEnumerator Beat(string beatKey); 
        IEnumerator WaitSignal(string key);
    }
    
    private DialogueRunner _runner;
    private PresentationSessionEntry _cpsRouteEntry;
    private IPresentationSignalBridge _cpsSignalBridge;

    public void Initialize(
        DialogueRunner runner,
        PresentationSessionEntry cpsRouteEntry,
        IPresentationSignalBridge cpsSignalBridge)
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