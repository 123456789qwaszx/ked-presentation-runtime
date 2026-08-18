using UnityEngine;

public static class PresentationCommandKeyParser
{
    public static bool TryParseStageKey(string key, out PresentationStageKey stage)
    {
        string normalized = Normalize(key);

        switch (normalized)
        {
            case "0":
            case "00":
            case "s0":
            case "s00":
            case "stage0":
            case "stage00":
                stage = PresentationStageKey.Stage00;
                return true;

            case "1":
            case "01":
            case "s1":
            case "s01":
            case "stage1":
            case "stage01":
                stage = PresentationStageKey.Stage01;
                return true;

            case "2":
            case "02":
            case "s2":
            case "s02":
            case "stage2":
            case "stage02":
                stage = PresentationStageKey.Stage02;
                return true;
        }

        if (System.Enum.TryParse(key, true, out stage))
            return true;

        Debug.LogWarning($"[PresentationCommandKeyParser] Unknown stage key: {key}");
        stage = default;
        return false;
    }

    public static bool TryParseDepthLayerKey(string key, out PresentationDepthLayerKey layer)
    {
        string normalized = Normalize(key);

        switch (normalized)
        {
            case "far":
            case "f":
                layer = PresentationDepthLayerKey.Far;
                return true;

            case "back":
            case "b":
                layer = PresentationDepthLayerKey.Back;
                return true;

            case "mid":
            case "middle":
            case "m":
                layer = PresentationDepthLayerKey.Mid;
                return true;

            case "front":
            case "fr":
                layer = PresentationDepthLayerKey.Front;
                return true;

            case "close":
            case "c":
                layer = PresentationDepthLayerKey.Close;
                return true;
        }

        if (System.Enum.TryParse(key, true, out layer))
            return true;

        Debug.LogWarning($"[PresentationCommandKeyParser] Unknown depth layer key: {key}");
        layer = default;
        return false;
    }

    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return key
            .Trim()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
}