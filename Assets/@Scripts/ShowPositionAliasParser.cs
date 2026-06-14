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

            // 현재 CharAnchorPreset에 Trio 전용 값이 없으므로
            // trioleft/trioright는 저작 alias로 left/right에 매핑한다.
            // 나중에 trio 전용 간격이 필요해지면 CharAnchorPreset에 TrioLeft/TrioRight를 추가하면 됨.
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