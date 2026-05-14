using System;
using UnityEngine;

public enum CharacterFocusAnchor
{
    Face,
    Bust,
    Body,
    Feet,
    Custom1,
    Custom2
}

public static class CharacterFocusAnchorParser
{
    public static bool TryParse(string raw, out CharacterFocusAnchor anchor)
    {
        anchor = CharacterFocusAnchor.Face;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");

        switch (s)
        {
            case "face":
            case "head":
            case "f":
                anchor = CharacterFocusAnchor.Face;
                return true;

            case "bust":
            case "upper":
            case "upper_body":
            case "chest":
                anchor = CharacterFocusAnchor.Bust;
                return true;

            case "body":
            case "center":
            case "torso":
                anchor = CharacterFocusAnchor.Body;
                return true;

            case "feet":
            case "foot":
            case "bottom":
                anchor = CharacterFocusAnchor.Feet;
                return true;

            case "custom1":
            case "custom_1":
            case "c1":
                anchor = CharacterFocusAnchor.Custom1;
                return true;

            case "custom2":
            case "custom_2":
            case "c2":
                anchor = CharacterFocusAnchor.Custom2;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out anchor);
    }
}