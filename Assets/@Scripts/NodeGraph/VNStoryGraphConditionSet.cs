using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VNStoryGraphConditionSet
{
    public List<string> trueConditionKeys = new List<string>();

    public bool Evaluate(string conditionKey)
    {
        if (string.IsNullOrWhiteSpace(conditionKey))
            return true;

        if (trueConditionKeys == null)
            return false;

        for (int i = 0; i < trueConditionKeys.Count; i++)
        {
            if (trueConditionKeys[i] == conditionKey)
                return true;
        }

        return false;
    }

    public bool Contains(string conditionKey)
    {
        return Evaluate(conditionKey);
    }
}