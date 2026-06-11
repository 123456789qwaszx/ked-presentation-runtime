using UnityEngine;

public static class CharAnchorPresetParser
{
    public static CharAnchorPreset Parse(string value)
    {
        if (TryParse(value, out CharAnchorPreset preset))
            return preset;

        Debug.LogWarning(
            $"[CharAnchorPresetParser] Unknown anchor preset '{value}'. Fallback to '{CharAnchorPreset.Center}'.");

        return CharAnchorPreset.Center;
    }

    public static bool TryParse(string value, out CharAnchorPreset preset)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                preset = CharAnchorPreset.Left;
                return true;

            case "center":
            case "centre":
            case "c":
                preset = CharAnchorPreset.Center;
                return true;

            case "right":
            case "r":
                preset = CharAnchorPreset.Right;
                return true;

            case "duo_left":
            case "duoleft":
            case "duo-l":
            case "dl":
                preset = CharAnchorPreset.DuoLeft;
                return true;

            case "duo_right":
            case "duoright":
            case "duo-r":
            case "dr":
                preset = CharAnchorPreset.DuoRight;
                return true;

            case "boxside":
            case "box_side":
            case "box":
                preset = CharAnchorPreset.BoxSide;
                return true;

            case "exp1":
            case "e1":
                preset = CharAnchorPreset.Exp1;
                return true;

            case "exp2":
            case "e2":
                preset = CharAnchorPreset.Exp2;
                return true;

            case "none":
                preset = CharAnchorPreset.None;
                return true;

            default:
                preset = CharAnchorPreset.Center;
                return false;
        }
    }
}