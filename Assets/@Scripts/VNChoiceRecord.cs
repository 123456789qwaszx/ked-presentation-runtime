using System;
using System.Collections.Generic;

[Serializable]
public struct VNChoiceRecord
{
    public int anchorHistoryIndex;
    public string nodeName;
    public int choiceIndexInNode;
    public int selectedOptionIndex;

    public VNChoiceRecord(
        int anchorHistoryIndex,
        string nodeName,
        int choiceIndexInNode,
        int selectedOptionIndex)
    {
        this.anchorHistoryIndex = anchorHistoryIndex;
        this.nodeName = nodeName ?? "";
        this.choiceIndexInNode = choiceIndexInNode;
        this.selectedOptionIndex = selectedOptionIndex;
    }
}

public class ChoiceHistory
{
    public int NextChoiceIndex { get; set; }

    private readonly List<VNChoiceRecord> _choices = new();
    
    public List<VNChoiceRecord> CreateChoiceSnapshot() => new (_choices);
    
    public void ResetIndex() => NextChoiceIndex = 0;

    public void RemoveChoiceAnchorAfterRollbackPoint(RollbackPoint target)
    {
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            if (_choices[i].anchorHistoryIndex > target.historyIndex)
                _choices.RemoveAt(i);
        }
    }
    
    public void AddChoiceRecord(IReadOnlyList<RollbackPoint> rollbackPoints, string nodeName, int choiceIndexInNode, int selectedOptionIndex)
    {
        if (rollbackPoints.Count <= 0)
            return;
        
        int anchorHistoryIndex = rollbackPoints[^1].historyIndex;

        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (!string.Equals(choice.nodeName, nodeName, StringComparison.Ordinal))
                continue;

            if (choice.choiceIndexInNode != choiceIndexInNode)
                continue;

            _choices.RemoveAt(i);
        }

        _choices.Add(new VNChoiceRecord(
            anchorHistoryIndex,
            nodeName,
            choiceIndexInNode,
            selectedOptionIndex));
    }

    public bool TryGetChoiceRecord(int choiceIndexInNode, out VNChoiceRecord record)
    {
        record = default;

        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (choice.choiceIndexInNode != choiceIndexInNode)
                continue;

            record = choice;
            return true;
        }

        return false;
    }
    
    
    public void RestoreChoiceSnapshot(IReadOnlyList<VNChoiceRecord> choices)
    {
        _choices.Clear();
        NextChoiceIndex = 0;

        for (int i = 0; i < choices.Count; i++)
        {
            _choices.Add(choices[i]);
        }
    }
    
    
    public void Clear()
    {
        _choices.Clear();
        NextChoiceIndex = 0;
    }
}