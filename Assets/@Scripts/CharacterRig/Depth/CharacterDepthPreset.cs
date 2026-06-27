using System;

public enum CharacterDepthKey
{
    None = 0,

    Far = 10,
    Back = 16,
    Mid = 20,
    Close = 30,
    Front = 40,

    Exp1 = 100,
    Exp2 = 101,
}

public static class CharacterDepthPresetParser
{
    public static bool TryParse(string raw, out CharacterDepthKey preset)
    {
        preset = CharacterDepthKey.Mid;

        string s = Normalize(raw);

        switch (s)
        {
            case "none":
            case "n":
                preset = CharacterDepthKey.None;
                return true;

            case "far":
            case "f":
                preset = CharacterDepthKey.Far;
                return true;
            
            case "back":
            case "b":
                preset = CharacterDepthKey.Back;
                return true;

            case "mid":
            case "middle":
            case "normal":
            case "default":
            case "m":
                preset = CharacterDepthKey.Mid;
                return true;

            case "front":
            case "fore":
            case "foreground":
                preset = CharacterDepthKey.Front;
                return true;
            
            case "close":
            case "near":
            case "c":
                preset = CharacterDepthKey.Close;
                return true;


            case "exp1":
            case "experimental1":
                preset = CharacterDepthKey.Exp1;
                return true;

            case "exp2":
            case "experimental2":
                preset = CharacterDepthKey.Exp2;
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