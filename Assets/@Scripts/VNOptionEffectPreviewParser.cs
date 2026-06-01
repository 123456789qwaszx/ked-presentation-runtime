using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class VNOptionEffectPreviewParser
{
    private static readonly Regex EffectRegex = new Regex(
        @"#(?<key>[A-Za-z_][A-Za-z0-9_]*):(?<min>[+-]?\d+)(?:~(?<max>[+-]?\d+))?",
        RegexOptions.Compiled);

    public static string Parse(
        string rawText,
        out List<VNOptionEffectPreview> effects)
    {
        effects = new List<VNOptionEffectPreview>();

        if (string.IsNullOrEmpty(rawText))
            return string.Empty;

        MatchCollection matches = EffectRegex.Matches(rawText);

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];

            string key = match.Groups["key"].Value;
            int min = ParseIntSafe(match.Groups["min"].Value);
            int max = match.Groups["max"].Success
                ? ParseIntSafe(match.Groups["max"].Value)
                : min;

            effects.Add(new VNOptionEffectPreview(key, min, max));
        }

        string label = EffectRegex.Replace(rawText, string.Empty);
        return NormalizeSpaces(label);
    }

    private static int ParseIntSafe(string value)
    {
        int result;
        if (int.TryParse(value, out result))
            return result;

        return 0;
    }

    private static string NormalizeSpaces(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}