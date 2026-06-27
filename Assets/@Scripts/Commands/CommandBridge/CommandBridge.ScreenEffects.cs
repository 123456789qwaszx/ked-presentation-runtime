public sealed partial class YarnCommandBridge
{
    private void EnqueueScreenFlashPresetSpec(string presetKey, float intensity = 1f)
        => Collect(new ScreenFlashCommandSpec
        {
            presetKey = ScreenFlashPresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
        });

    private void EnqueueScreenFlashClearSpec()
        => Collect(new ScreenFlashCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
        });

    private void EnqueueScreenVignettePresetSpec(
        string presetKey,
        float intensity = 1f,
        string durationToken = "0.35s")
        => Collect(new ScreenVignetteCommandSpec
        {
            presetKey = ScreenVignettePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenVignetteClearSpec(string durationToken = "0.35s")
        => Collect(new ScreenVignetteCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoisePresetSpec(
        string presetKey = ScreenNoisePresetDBSO.DefaultPresetKey,
        float intensity = 1f,
        string durationToken = "0.35s")
        => Collect(new ScreenNoiseCommandSpec
        {
            presetKey = ScreenNoisePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoiseClearSpec(string durationToken = "0.35s")
        => Collect(new ScreenNoiseCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}