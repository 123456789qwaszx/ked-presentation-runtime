using UnityEngine;

public static class BgRigDirectionParser
{
    public static CharRigDirection Parse(string value, CharRigDirection fallback)
    {
        string normalized = (value ?? "").Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "left":
            case "l":
                return CharRigDirection.Left;

            case "right":
            case "r":
                return CharRigDirection.Right;

            case "up":
            case "u":
            case "top":
                return CharRigDirection.Up;

            case "down":
            case "d":
            case "bottom":
                return CharRigDirection.Down;

            default:
                Debug.LogWarning(
                    $"[CharRigDirectionParser] Unknown direction '{value}'. " +
                    $"Fallback to '{fallback}'.");
                return fallback;
        }
    }
}