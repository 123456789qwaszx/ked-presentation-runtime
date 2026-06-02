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

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(nodeName)
               && choiceIndexInNode >= 0
               && selectedOptionIndex >= 0;
    }
}

public class ChoiceHistory
{
    private const int MaxOptionCount = 5;
    
    private string _currentChoiceNodeName;
    private int _nextChoiceIndexInNode;
    
    private readonly List<VNChoiceRecord> _choices = new();
    
    public List<VNChoiceRecord> CreateChoiceSnapshot() => new (_choices);
    
    public void NotifyNodeStarted(string nodeName)
    {
        _currentChoiceNodeName = nodeName;
        _nextChoiceIndexInNode = 0;
    }
    
    public int ConsumeChoiceIndexInCurrentNode(string nodeName)
    {
        if (!string.Equals(_currentChoiceNodeName, nodeName, StringComparison.Ordinal))
            NotifyNodeStarted(nodeName);

        int index = _nextChoiceIndexInNode;
        _nextChoiceIndexInNode++;

        return index;
    }

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

    public bool TryGetChoiceRecord(string nodeName, int choiceIndexInNode, out VNChoiceRecord record)
    {
        record = default;

        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (!string.Equals(choice.nodeName, nodeName, StringComparison.Ordinal))
                continue;

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

        if (choices == null)
            return;

        for (int i = 0; i < choices.Count; i++)
        {
            VNChoiceRecord choice = choices[i];

            if (!choice.IsValid())
                continue;

            if (choice.selectedOptionIndex < 0 ||
                choice.selectedOptionIndex >= MaxOptionCount)
                continue;

            _choices.Add(choice);
        }
    }
    
    
    public void ClearAll()
    {
        _choices.Clear();

        _currentChoiceNodeName = "";
        _nextChoiceIndexInNode = 0;
    }

    public void ClearRollbackPointsForSeekRebuild()
    {
        _currentChoiceNodeName = "";
        _nextChoiceIndexInNode = 0;
    }
}