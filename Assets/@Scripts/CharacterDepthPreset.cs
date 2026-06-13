using System;

public enum CharacterDepthPreset
{
    None = 0,

    Far = 10,
    Mid = 20,
    Close = 30,
    Front = 40,

    Exp1 = 100,
    Exp2 = 101,
}

public static class CharacterDepthPresetParser
{
    public static bool TryParse(string raw, out CharacterDepthPreset preset)
    {
        preset = CharacterDepthPreset.Mid;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = Normalize(raw);

        switch (s)
        {
            case "none":
            case "n":
                preset = CharacterDepthPreset.None;
                return true;

            case "far":
            case "f":
            case "back":
            case "b":
                preset = CharacterDepthPreset.Far;
                return true;

            case "mid":
            case "middle":
            case "normal":
            case "default":
            case "m":
                preset = CharacterDepthPreset.Mid;
                return true;

            case "close":
            case "near":
            case "c":
                preset = CharacterDepthPreset.Close;
                return true;

            case "front":
            case "fore":
            case "foreground":
                preset = CharacterDepthPreset.Front;
                return true;

            case "exp1":
            case "experimental1":
                preset = CharacterDepthPreset.Exp1;
                return true;

            case "exp2":
            case "experimental2":
                preset = CharacterDepthPreset.Exp2;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out preset);
    }

    private static string Normalize(string raw)
    {
        string s = raw.Trim().ToLowerInvariant();

        s = s.Replace("-", "_");
        s = s.Replace(".", "_");
        s = s.Replace(" ", "_");

        return s;
    }
}

// public static class CharacterFocusPresetParser2
// {
//     public static bool TryParse(string raw, out CharacterFocusPreset preset)
//     {
//         preset = CharacterFocusPreset.Bust;
//
//         if (string.IsNullOrWhiteSpace(raw))
//             return false;
//
//         string s = Normalize(raw);
//
//         switch (s)
//         {
//             case "feet":
//             case "foot":
//             case "base":
//             case "bottom":
//                 preset = CharacterFocusPreset.Feet;
//                 return true;
//
//             case "body":
//             case "center":
//             case "center_body":
//                 preset = CharacterFocusPreset.Body;
//                 return true;
//
//             case "bust":
//             case "chest":
//             case "upper":
//             case "upper_body":
//                 preset = CharacterFocusPreset.Bust;
//                 return true;
//
//             case "face":
//             case "head":
//             case "eye":
//             case "eyes":
//                 preset = CharacterFocusPreset.Face;
//                 return true;
//
//             case "hand_left":
//             case "left_hand":
//             case "lh":
//                 preset = CharacterFocusPreset.HandLeft;
//                 return true;
//
//             case "hand_right":
//             case "right_hand":
//             case "rh":
//                 preset = CharacterFocusPreset.HandRight;
//                 return true;
//
//             case "custom":
//                 preset = CharacterFocusPreset.Custom;
//                 return true;
//         }
//
//         return Enum.TryParse(raw.Trim(), true, out preset);
//     }
//
//     private static string Normalize(string raw)
//     {
//         string s = raw.Trim().ToLowerInvariant();
//
//         s = s.Replace("-", "_");
//         s = s.Replace(".", "_");
//         s = s.Replace(" ", "_");
//
//         return s;
//     }
// }