using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueScreenFlashPresetSpec(
        string presetKey,
        float intensity = 1f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            presetKey = ScreenFlashPresetDBSO.NormalizeKey(presetKey),
            intensity = Mathf.Clamp01(intensity),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashClearSpec()
    {
        var spec = new ScreenFlashCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignettePresetSpec(
        string presetKey,
        float intensity = 1f,
        float duration = 0.35f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            presetKey = ScreenVignettePresetDBSO.NormalizeKey(presetKey),
            intensity = Mathf.Clamp01(intensity),
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignetteClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoisePresetSpec(
        string presetKey = ScreenNoisePresetDBSO.DefaultPresetKey,
        float intensity = 1f,
        float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            presetKey = ScreenNoisePresetDBSO.NormalizeKey(presetKey),
            intensity = Mathf.Clamp01(intensity),
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoiseClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }
}