using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindScreenEffects(DialogueRunner runner)
    {
        runner.AddCommandHandler<float, float>("screen_flash", EnqueueScreenFlashSpec);
        runner.AddCommandHandler<float, float, float, float, float>("screen_flash_rgb", EnqueueScreenFlashRgbSpec);
        runner.AddCommandHandler("screen_flash_hit", EnqueueScreenFlashHitSpec);

        runner.AddCommandHandler<string, float, float>("screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<float>("screen_vignette_clear", EnqueueScreenVignetteClearSpec);
        runner.AddCommandHandler<float, float>("screen_letterbox", EnqueueScreenLetterBoxSpec);
        runner.AddCommandHandler<float, float, float, float, float, float, float>("screen_vignette_custom", EnqueueScreenVignetteCustomSpec);

        runner.AddCommandHandler<string, float, float>("screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<float>("screen_noise_clear", EnqueueScreenNoiseClearSpec);
        runner.AddCommandHandler<float, float, float, float, float, float>("screen_noise_custom", EnqueueScreenNoiseCustomSpec);
    }

    private void EnqueueScreenFlashSpec(float amount = 1f, float duration = 0.16f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            color = Color.white,
            amount = amount,
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
            color = new Color(r, g, b, 1f),
            amount = amount,
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
            color = new Color(1f, 0.16f, 0.10f, 1f),
            amount = 0.45f,
            attackDuration = 0.015f,
            holdDuration = 0.015f,
            releaseDuration = 0.18f,
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
            mode = ScreenVignetteMode.Preset,
            preset = ParseVignettePreset(presetKey),
            intensity = intensity,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenVignetteClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenVignetteCommandSpec
        {
            mode = ScreenVignetteMode.Clear,
            duration = duration,
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
            letterBoxAmount = amount,
            letterBoxSoftness = 0.025f,
            duration = duration,
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

    private ScreenVignettePreset ParseVignettePreset(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ScreenVignettePreset.DefaultFocus;

        switch (key.Trim().ToLowerInvariant())
        {
            case "focus":
            case "default":
            case "default_focus":
                return ScreenVignettePreset.DefaultFocus;

            case "tension":
            case "tense":
                return ScreenVignettePreset.Tension;

            case "horror":
            case "fear":
                return ScreenVignettePreset.Horror;

            case "danger":
            case "warning":
            case "red":
                return ScreenVignettePreset.Danger;

            case "memory":
            case "recall":
            case "flashback":
                return ScreenVignettePreset.Memory;

            case "dream":
            case "dreamy":
                return ScreenVignettePreset.Dream;

            default:
                Debug.LogWarning(
                    $"[YarnCommandBridge] Unknown screen vignette preset '{key}'. " +
                    $"Fallback to DefaultFocus.");
                return ScreenVignettePreset.DefaultFocus;
        }
    }

    private void EnqueueScreenNoisePresetSpec(
        string presetKey,
        float intensity = 1f,
        float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            mode = ScreenNoiseMode.Preset,
            preset = ParseNoisePreset(presetKey),
            intensity = intensity,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenNoiseClearSpec(float duration = 0.35f)
    {
        var spec = new ScreenNoiseCommandSpec
        {
            mode = ScreenNoiseMode.Clear,
            duration = duration,
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
            duration = duration,
            wait = false
        };

        Collect(spec);
    }

    private ScreenNoisePreset ParseNoisePreset(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ScreenNoisePreset.Default;

        switch (key.Trim().ToLowerInvariant())
        {
            case "default":
            case "normal":
                return ScreenNoisePreset.Default;

            case "memory":
            case "recall":
            case "flashback":
                return ScreenNoisePreset.Memory;

            case "horror":
            case "fear":
                return ScreenNoisePreset.Horror;

            case "broadcast":
            case "stream":
            case "tv":
            case "monitor":
                return ScreenNoisePreset.Broadcast;

            case "dream":
            case "dreamy":
                return ScreenNoisePreset.Dream;

            case "rain":
            case "rain_mood":
            case "rainy":
                return ScreenNoisePreset.RainMood;

            default:
                Debug.LogWarning(
                    $"[YarnCommandBridge] Unknown screen noise preset '{key}'. " +
                    $"Fallback to Default.");
                return ScreenNoisePreset.Default;
        }
    }
}