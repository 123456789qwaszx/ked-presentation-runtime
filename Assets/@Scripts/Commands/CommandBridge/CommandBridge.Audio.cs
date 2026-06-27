public sealed partial class YarnCommandBridge
{
    private void EnqueuePlayBgmSpec(string clipKey, float fadeDuration = 1f)
        => Collect(new PlayBgmCommandSpec { clipKey = clipKey, fadeDuration = fadeDuration });

    private void EnqueueStopBgmSpec(float fadeDuration = 1f)
        => Collect(new StopBgmCommandSpec { fadeDuration = fadeDuration });

    private void EnqueuePlaySfxSpec(string clipKey)
        => Collect(new PlaySfxCommandSpec { clipKey = clipKey });

    private void EnqueueStopAllSfxSpec()
        => Collect(new StopAllSfxCommandSpec());

    private void EnqueuePlayVoiceSpec(string clipKey)
        => Collect(new PlayVoiceCommandSpec { clipKey = clipKey });

    private void EnqueueStopVoiceSpec()
        => Collect(new StopVoiceCommandSpec());
}