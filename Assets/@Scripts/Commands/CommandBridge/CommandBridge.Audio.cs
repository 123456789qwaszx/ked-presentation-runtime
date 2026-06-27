public sealed partial class YarnCommandBridge
{
    private void EnqueuePlayBgmSpec(string clipKey, string fadeDurationToken = "1s")
        => Collect(new PlayBgmCommandSpec
        {
            clipKey = clipKey,
            fadeDuration = YarnDurationParser.Parse(fadeDurationToken)
        });

    private void EnqueueStopBgmSpec(string fadeDurationToken = "1s")
        => Collect(new StopBgmCommandSpec
        {
            fadeDuration = YarnDurationParser.Parse(fadeDurationToken)
        });

    private void EnqueuePlaySfxSpec(string clipKey)
        => Collect(new PlaySfxCommandSpec { clipKey = clipKey });

    private void EnqueueStopAllSfxSpec()
        => Collect(new StopAllSfxCommandSpec());

    private void EnqueuePlayVoiceSpec(string clipKey)
        => Collect(new PlayVoiceCommandSpec { clipKey = clipKey });

    private void EnqueueStopVoiceSpec()
        => Collect(new StopVoiceCommandSpec());
}