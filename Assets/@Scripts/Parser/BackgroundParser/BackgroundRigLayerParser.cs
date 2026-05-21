using UnityEngine;

public static class BackgroundRigLayerParser
{
    public static BackgroundRigTarget ParseImageTarget(string value, BackgroundRigTarget fallback)
    {
        string normalized = (value ?? "").Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "":
            case "back":
            case "b":
            case "bg":
            case "background":
                return BackgroundRigTarget.Background_BackLayer_Image;

            case "front":
            case "f":
            case "fg":
            case "foreground":
                return BackgroundRigTarget.Background_FrontLayer_Image;

            default:
                Debug.LogWarning(
                    $"[BackgroundRigLayerParser] Unknown background image layer '{value}'. " +
                    $"Fallback to '{fallback}'.");
                return fallback;
        }
    }
}