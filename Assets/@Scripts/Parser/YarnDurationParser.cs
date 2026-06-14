using System.Globalization;
using UnityEngine;

public static class YarnDurationParser
{
    private const float FramesPerSecond = 24f;

    public static float Parse(string token, float fallbackSeconds = 8f)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Mathf.Max(0f, fallbackSeconds);

        string originalToken = token;
        token = token.Trim().ToLowerInvariant();

        if (token.EndsWith("fr"))
        {
            string frameText = token[..^2];

            if (float.TryParse(
                    frameText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float frames))
            {
                return Mathf.Max(0f, frames / FramesPerSecond);
            }

            WarnInvalid(originalToken, fallbackSeconds);
            return Mathf.Max(0f, fallbackSeconds);
        }

        if (token.EndsWith("s"))
        {
            string secondText = token[..^1];

            if (float.TryParse(
                    secondText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float seconds))
            {
                return Mathf.Max(0f, seconds);
            }

            WarnInvalid(originalToken, fallbackSeconds);
            return Mathf.Max(0f, fallbackSeconds);
        }

        // Backward compatibility:
        // bare number is treated as seconds.
        // Example: "0.4" => 0.4 seconds.
        if (float.TryParse(
                token,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float rawSeconds))
        {
            return Mathf.Max(0f, rawSeconds);
        }

        WarnInvalid(originalToken, fallbackSeconds);
        return Mathf.Max(0f, fallbackSeconds);
    }

    public static float FramesToSeconds(float frames)
    {
        return Mathf.Max(0f, frames / FramesPerSecond);
    }

    private static void WarnInvalid(string token, float fallbackSeconds)
    {
        Debug.LogWarning(
            $"[YarnDurationParser] Invalid duration token '{token}'. " +
            $"Expected format: 4fr, 12fr, 0.4s, or 0.4. " +
            $"Fallback to {fallbackSeconds:0.###} seconds.");
    }
}