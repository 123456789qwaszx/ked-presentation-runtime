using System.Globalization;

public static class YarnNumberParser
{
    public static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(
            (value ?? "").Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }
}