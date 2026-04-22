using System.Collections.Generic;
using System.Text.RegularExpressions;

public sealed class RecipeTextParser
{
    private static readonly Regex CommandRegex =
        new Regex(@"^\s*<<\s*(.+?)\s*>>\s*$", RegexOptions.Compiled);

    public List<RecipeCommandLine> Parse(string text)
    {
        var result = new List<RecipeCommandLine>();

        if (string.IsNullOrWhiteSpace(text))
            return result;

        string[] lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (raw.StartsWith("//"))
                continue;

            Match match = CommandRegex.Match(raw);
            if (!match.Success)
                continue;

            string body = match.Groups[1].Value.Trim();
            List<string> tokens = Tokenize(body);

            if (tokens.Count == 0)
                continue;

            var line = new RecipeCommandLine
            {
                lineNumber = i + 1,
                rawText = raw,
                commandName = tokens[0],
            };

            for (int t = 1; t < tokens.Count; t++)
                line.args.Add(tokens[t]);

            result.Add(line);
        }

        return result;
    }

    private List<string> Tokenize(string body)
    {
        var result = new List<string>();

        if (string.IsNullOrWhiteSpace(body))
            return result;

        string[] split = body.Split(' ');
        for (int i = 0; i < split.Length; i++)
        {
            string token = split[i].Trim();
            if (!string.IsNullOrWhiteSpace(token))
                result.Add(token);
        }

        return result;
    }
}
