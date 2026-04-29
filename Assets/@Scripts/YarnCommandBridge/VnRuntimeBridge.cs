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
    private PresentationSessionEntry _presentationSessionEntry;
    private IPresentationSignalBridge _cpsSignalBridge;

    public void Initialize(
        DialogueRunner runner,
        PresentationSessionEntry cpsRouteEntry,
        IPresentationSignalBridge cpsSignalBridge)
    {
        _runner = runner;
        _presentationSessionEntry = cpsRouteEntry;
        _cpsSignalBridge = cpsSignalBridge;
    }
    
    public IEnumerator Beat(string beatKey) => _cpsSignalBridge.Beat(beatKey);
    public IEnumerator WaitSignal(string key) => _cpsSignalBridge?.WaitSignal(key);
    
    public void ForceCompleteEpisodeNow(string episodeId)
    {
        if (_runner.IsDialogueRunning)
            _runner.Stop();
        
        _presentationSessionEntry.RequestEnd();
    }
}