public static class CharScalePresetParser
{
    public static CharScalePreset Parse(string value)
    {
        if (TryParse(value, out CharScalePreset preset))
            return preset;

        return CharScalePreset.Normal;
    }

    public static bool TryParse(string value, out CharScalePreset preset)
    {
        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "normal":
            case "n":
                preset = CharScalePreset.Normal;
                return true;

            case "small":
            case "s":
                preset = CharScalePreset.Small;
                return true;

            case "large":
            case "l":
                preset = CharScalePreset.Large;
                return true;

            case "far":
            case "f":
                preset = CharScalePreset.Far;
                return true;

            case "close":
            case "c":
                preset = CharScalePreset.Close;
                return true;

            case "exp1":
            case "e1":
                preset = CharScalePreset.Exp1;
                return true;

            case "exp2":
            case "e2":
                preset = CharScalePreset.Exp2;
                return true;

            default:
                preset = CharScalePreset.Normal;
                return false;
        }
    }
}