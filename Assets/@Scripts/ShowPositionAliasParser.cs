public static class ShowPositionAliasParser
{
    public static string Parse(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "center";

        string s = Normalize(token);

        return s switch
        {
            "solo" => "center",
            "single" => "center",
            "center" => "center",
            "c" => "center",

            "left" => "left",
            "l" => "left",

            "right" => "right",
            "r" => "right",

            "duoleft" => "duoleft",
            "duo_left" => "duoleft",
            "dl" => "duoleft",

            "duoright" => "duoright",
            "duo_right" => "duoright",
            "dr" => "duoright",

            "trioleft" => "left",
            "trio_left" => "left",
            "tl" => "left",

            "trioright" => "right",
            "trio_right" => "right",
            "tr" => "right",

            _ => token.Trim()
        };
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