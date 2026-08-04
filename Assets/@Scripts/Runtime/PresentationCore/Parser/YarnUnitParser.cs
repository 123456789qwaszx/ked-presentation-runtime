using System.Globalization;
using UnityEngine;

public static class YarnUnitParser
{
    private const float ReferenceStageWidth = 1920f;
    private const float StageWidthDivisor = 48f;
    private const float UnitPixels = ReferenceStageWidth / StageWidthDivisor; // 40px

    public static float Parse(string token, float fallbackUnits = 1)
    {
        if (string.IsNullOrWhiteSpace(token))
            return fallbackUnits * UnitPixels;

        token = token.Trim().ToLowerInvariant();

        if (token.EndsWith("u"))
            token = token[..^1];

        if (float.TryParse(
                token,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float units))
        {
            return Mathf.Max(0f, units) * UnitPixels;
        }

        Debug.LogWarning(
            $"[YarnUnitParser] Invalid unit token '{token}'. " +
            $"Fallback to {fallbackUnits:0.###}u.");

        return fallbackUnits * UnitPixels;
    }
    public static float ParseAllowNegative(string token, float fallbackUnits = 0f)
    {
        if (string.IsNullOrWhiteSpace(token))
            return fallbackUnits * UnitPixels;

        string originalToken = token;

        token = token.Trim().ToLowerInvariant();

        if (token.EndsWith("u"))
            token = token[..^1];

        if (float.TryParse(
                token,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float units))
        {
            return units * UnitPixels;
        }

        Debug.LogWarning(
            $"[YarnUnitParser] Invalid signed unit token '{originalToken}'. " +
            $"Fallback to {fallbackUnits:0.###}u.");

        return fallbackUnits * UnitPixels;
    }
}