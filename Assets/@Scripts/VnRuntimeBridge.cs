using System.Collections;
using UnityEngine;
using Yarn.Unity;


public sealed class VnRuntimeBridge
{
    public interface ICpsSignalBridge
    {
        IEnumerator Beat(string beatKey); 
        IEnumerator WaitSignal(string key);
    }
    
    private readonly ICpsSignalBridge _cps;

    // 추가: 강제 종료를 위해 (둘 다 MonoBehaviour면 SerializeField로 넣어도 되고, 생성자 주입도 OK)
    private readonly DialogueRunner _runner;
    private readonly PresentationRouteEntry _cpsRouteEntry;


    public VnRuntimeBridge(
        ICpsSignalBridge cps,
        DialogueRunner runner,
        PresentationRouteEntry cpsRouteEntry)
    {
        _cps = cps;
        _runner = runner;
        _cpsRouteEntry = cpsRouteEntry;
    }

    public IEnumerator Beat(string beatKey)
    {
        if (_cps == null) yield break;
        
        yield return _cps.Beat(beatKey);
    }

    public IEnumerator WaitSignal(string key) => _cps?.WaitSignal(key);
}