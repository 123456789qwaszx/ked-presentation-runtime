using System;

[Serializable]
public struct VNOptionEffectPreview
{
    public string statKey;
    public int minValue;
    public int maxValue;

    public bool HasRange
    {
        get { return minValue != maxValue; }
    }

    public VNOptionEffectPreview(string statKey, int minValue, int maxValue)
    {
        this.statKey = statKey;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public string ToDisplayText()
    {
        if (string.IsNullOrEmpty(statKey))
            return string.Empty;

        if (HasRange)
            return string.Format("{0} {1:+#;-#;0}~{2:+#;-#;0}", statKey, minValue, maxValue);

        return string.Format("{0} {1:+#;-#;0}", statKey, minValue);
    }
}