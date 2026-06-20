using UnityEngine;

public static class PresentationScreenEffectKeyParser
{
    public static ScreenFlashPreset ParseFlashPreset(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ScreenFlashPreset.Default;

        switch (key.Trim().ToLowerInvariant())
        {
            case "default":
            case "white":
            case "pop":
                return ScreenFlashPreset.Default;

            case "soft":
            case "gentle":
                return ScreenFlashPreset.Soft;

            case "hit":
            case "impact":
            case "damage":
                return ScreenFlashPreset.Hit;

            case "camera":
            case "photo":
                return ScreenFlashPreset.Camera;

            default:
                Debug.LogWarning($"[PresentationScreenEffectKeyParser] Unknown screen flash preset '{key}'. Fallback to Default.");
                return ScreenFlashPreset.Default;
        }
    }

    public static ScreenVignettePreset ParseVignettePreset(string key)
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
                Debug.LogWarning($"[PresentationScreenEffectKeyParser] Unknown screen vignette preset '{key}'. Fallback to DefaultFocus.");
                return ScreenVignettePreset.DefaultFocus;
        }
    }

    public static ScreenNoisePreset ParseNoisePreset(string key)
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
                Debug.LogWarning($"[PresentationScreenEffectKeyParser] Unknown screen noise preset '{key}'. Fallback to Default.");
                return ScreenNoisePreset.Default;
        }
    }
}