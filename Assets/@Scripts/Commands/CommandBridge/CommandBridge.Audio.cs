public sealed partial class YarnCommandBridge
{
    private void EnqueuePlayBgmSpec(string clipKey, float fadeDuration = 1f)
    {
        var spec = new PlayBgmCommandSpec
        {
            clipKey = clipKey,
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }

    private void EnqueueStopBgmSpec(float fadeDuration = 1f)
    {
        var spec = new StopBgmCommandSpec
        {
            fadeDuration = fadeDuration
        };

        Collect(spec);
    }
    
    private void EnqueuePlaySfxSpec(string clipKey)
    {
        var spec = new PlaySfxCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void EnqueueStopAllSfxSpec()
    {
        var spec = new StopAllSfxCommandSpec();
        
        Collect(spec);
    }
    
    private void EnqueuePlayVoiceSpec(string clipKey)
    {
        var spec = new PlayVoiceCommandSpec
        {
            clipKey = clipKey
        };

        Collect(spec);
    }

    private void EnqueueStopVoiceSpec()
    {
        var spec = new StopVoiceCommandSpec();
        
        Collect(spec);
    }
}