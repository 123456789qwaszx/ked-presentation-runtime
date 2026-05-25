using System;

[Serializable]
public sealed class VNStoryNextLink
{
    public string linkKey;
    public string toNodeId;

    public string labelKey;
    public string conditionKey;
    public string unlockConditionKey;

    public bool HasTarget
    {
        get { return !string.IsNullOrWhiteSpace(toNodeId); }
    }
}