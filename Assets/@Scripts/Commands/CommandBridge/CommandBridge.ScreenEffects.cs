using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueScreenFlashSpec(float amount = 1f, float duration = 0.16f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            mode = ScreenFlashMode.Custom,
            color = Color.white,
            amount = Mathf.Clamp01(amount),
            attackDuration = 0.02f,
            holdDuration = 0.01f,
            releaseDuration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashRgbSpec(
        float r,
        float g,
        float b,
        float amount = 1f,
        float duration = 0.16f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            mode = ScreenFlashMode.Custom,
            color = new Color(r, g, b, 1f),
            amount = Mathf.Clamp01(amount),
            attackDuration = 0.02f,
            holdDuration = 0.01f,
            releaseDuration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashHitSpec()
    {
        var spec = new ScreenFlashCommandSpec
        {
            mode = ScreenFlashMode.Preset,
            preset = ScreenFlashPreset.Hit,
            intensity = 1f,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashPresetSpec(string presetKey, float intensity = 1f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            mode = ScreenFlashMode.Preset,
            preset = PresentationScreenEffectKeyParser.ParseFlashPreset(presetKey),
            intensity = intensity,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignettePresetSpec(
        string presetKey,
        float intensity = 1f,
        float duration = 55f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            mode = ScreenVignetteMode.Preset,
            preset = PresentationScreenEffectKeyParser.ParseVignettePreset(presetKey),
            intensity = intensity,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignetteClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            mode = ScreenVignetteMode.Clear,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenLetterBoxSpec(
        float amount = 0.5f,
        float duration = 0.35f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            mode = ScreenVignetteMode.LetterBox,
            letterBoxAmount = Mathf.Clamp01(amount),
            letterBoxSoftness = 0.025f,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignetteCustomSpec(
        float amount,
        float radius,
        float softness,
        float aspect,
        float r,
        float g,
        float b)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            mode = ScreenVignetteMode.Custom,
            amount = amount,
            radius = radius,
            softness = softness,
            aspect = aspect,
            color = new Color(r, g, b, 1f),
            duration = 0.35f,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoisePresetSpec(
        string presetKey = "default",
        float intensity = 1f,
        float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            mode = ScreenNoiseMode.Preset,
            preset = PresentationScreenEffectKeyParser.ParseNoisePreset(presetKey),
            intensity = intensity,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoiseClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            mode = ScreenNoiseMode.Clear,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoiseCustomSpec(
        float amount,
        float scale,
        float speedX,
        float speedY,
        float contrast,
        float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            mode = ScreenNoiseMode.Custom,
            amount = amount,
            color = Color.white,
            scale = scale,
            speedX = speedX,
            speedY = speedY,
            contrast = contrast,
            duration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }
}