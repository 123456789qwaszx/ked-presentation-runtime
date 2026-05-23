using System;

public static class CharacterFocusPresetParser
{
    public static bool TryParse(string raw, out CharacterFocusPreset preset)
    {
        preset = CharacterFocusPreset.Face;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");

        switch (s)
        {
            case "feet":
            case "foot":
            case "base":
            case "bottom":
                preset = CharacterFocusPreset.Feet;
                return true;

            case "body":
            case "torso":
            case "mid":
            case "middle":
                preset = CharacterFocusPreset.Body;
                return true;

            case "bust":
            case "chest":
            case "upper":
                preset = CharacterFocusPreset.Bust;
                return true;

            case "face":
            case "head":
            case "eye":
            case "eyes":
                preset = CharacterFocusPreset.Face;
                return true;

            case "custom":
                preset = CharacterFocusPreset.Custom;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out preset);
    }
}