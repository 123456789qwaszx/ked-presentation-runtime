using System.Collections;
using UnityEngine;

public interface IInlineSignalHost
{
    void RaiseSignal(string key);
}

public sealed class PresentationSessionBridge : VnRuntimeBridge.ICpsSignalBridge, IInlineSignalHost
{
    private readonly UnitySignalBus _unitySignalBus;
    private readonly PresentationSession _session;
    
    public PresentationSessionBridge(PresentationSession session,
        UnitySignalBus unitySignalBus)
    {
        _session = session;
        _unitySignalBus = unitySignalBus;
    }
    
    // private void Update()
    // {
    //     if (Session != null)
    //         Session.Tick();
    //
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         _unityInputSource.PulseAdvancePressed();
    //     }
    // }
    
    
    public IEnumerator Beat(string beatKey)
    {
        if (_session == null || !_session.IsRunning) yield break;

        _unitySignalBus.Raise(beatKey);

        while (_session.IsNodeBusy())
            yield return null;
        
        Debug.Log($"[Command] {beatKey}");
    }
    
    /// <summary>
    /// 최대 10초 대기
    /// </summary>
    public IEnumerator WaitSignal(string key)
    {
        if (string.IsNullOrEmpty(key)) yield break;
        if (_unitySignalBus == null) yield break;

        bool received = false;
        float timeoutSeconds = 10f;
        float elapsedTime = 0f;

        void Handler(string raisedKey)
        {
            if (raisedKey == key)
                received = true;
        }

        _unitySignalBus.OnSignal += Handler;

        try
        {
            while (!received && elapsedTime < timeoutSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!received)
            {
                Debug.LogError($"[WaitSignal] Timeout after {timeoutSeconds}s waiting for signal: '{key}'");
            }
        }
        finally
        {
            _unitySignalBus.OnSignal -= Handler;
        }
    }
    
    public void RaiseSignal(string key)
    {
        if (_unitySignalBus == null) return;
        
        _unitySignalBus.Raise(key);
        
        Debug.Log($"[Markup] {key}");
    }
}