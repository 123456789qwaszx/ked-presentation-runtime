using System.Collections;

public sealed partial class YarnCommandBridge
{
    // Marks the next N collected commands as wait=true.
    // This only affects Presentation/Executor playback.
    private void AwaitFor(int count = 1)
    {
        _playbackDriver.WaitNextImmediateCommands(count);
    }
    
    private void BeginHold()
    {
        _playbackDriver.BeginHold();
    }

    // Closes the active hold block and blocks Yarn until held wait=true commands finish.
    private IEnumerator PlayHeldCommands()
    {
        yield return _playbackDriver.EndHoldBlocking();
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
}