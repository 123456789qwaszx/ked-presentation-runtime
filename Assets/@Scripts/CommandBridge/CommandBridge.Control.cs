using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BeginBlockCapture()
    {
        _playbackDriver.BeginBlockCapture();
    }
    
    // Plays the currently collected command block at this point in Yarn flow.
    // Yarn waits until playback finishes
    private IEnumerator PlayCapturedBlock(float waitTime = 1f)
    {
        EnqueueWaitSpec(waitTime);
        
        yield return _playbackDriver.PlayCapturedBlock();
    }
    
    private void EnqueueWaitSpec(float duration)
    {
        var spec = new WaitCommandSpec()
        {
            seconds = duration,
        };
        
        Collect(spec);
    }
    
    private void EnqueueHideDialogueBoxSpec(float duration = 0f)
    {
        var spec = new HideDialogueBoxCommandSpec
        {
            hideAll = true,
            duration = duration,
            wait = true
        };

        Collect(spec);
    }
    
    private void EnqueueUIPatchSpec(string themeId = "default")
    {
        var spec = new UIPatchCommandSpec
        {
            themeId = themeId,
        };

        Collect(spec);
    }
    
    
    private void LogImmediate(string message)
    {
        Debug.Log($"[YarnCommandBridge] {message}");
    }
    
    private void LogYarnState(string label)
    {
        VariableStorageBehaviour storage = _dialogueRunner.VariableStorage;

        storage.TryGetValue("$favor", out float favor);
        storage.TryGetValue("$laru_patience", out float patience);
        storage.TryGetValue("$willow_debt", out float debt);
        storage.TryGetValue("$requested_fee", out float requestedFee);
        storage.TryGetValue("$paid_fee", out float paidFee);
        storage.TryGetValue("$trust", out float trust);
        storage.TryGetValue("$anger", out float anger);
        storage.TryGetValue("$contract_signed", out bool contractSigned);

        Debug.Log(
            $"[YarnState] {label} | " +
            $"favor={favor}, patience={patience}, debt={debt}, " +
            $"requested={requestedFee}, paid={paidFee}, trust={trust}, " +
            $"anger={anger}, contract={contractSigned}"
        );
    }
}