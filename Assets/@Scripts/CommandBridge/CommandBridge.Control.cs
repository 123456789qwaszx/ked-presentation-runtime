using System.Collections;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void EnqueueWaitSpec(float duration)
    {
        var spec = new WaitCommandSpec()
        {
            seconds = duration,
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
}