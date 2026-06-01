using System;

[Serializable]
public struct VNOptionEffectPreview
{
    public string StatKey { get; private set; }
    public int MinValue { get; private set; }
    public int MaxValue { get; private set; }

    public bool HasRange
    {
        get { return MinValue != MaxValue; }
    }

    public VNOptionEffectPreview(string statKey, int minValue, int maxValue)
    {
        StatKey = statKey ?? string.Empty;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public string ToDisplayText()
    {
        if (string.IsNullOrEmpty(StatKey))
            return string.Empty;

        string displayName = ResolveDisplayName(StatKey);

        if (HasRange)
            return string.Format("{0} {1:+#;-#;0}~{2:+#;-#;0}", displayName, MinValue, MaxValue);

        return string.Format("{0} {1:+#;-#;0}", displayName, MinValue);
    }

    private static string ResolveDisplayName(string statKey)
    {
        switch (statKey)
        {
            case "fatigue":
                return "피로";

            case "rare_ingredient":
                return "희귀재료";

            case "common_ingredient":
                return "일반재료";

            case "risk":
                return "위험";

            case "trust":
                return "신뢰";

            case "anger":
                return "분노";
        }

        return statKey;
    }
}