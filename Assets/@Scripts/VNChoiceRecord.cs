using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct VNChoiceRecord
{
    public int anchorHistoryIndex;
    public string nodeName;
    public int choiceIndexInNode;
    public int selectedOptionIndex;

    // Line ID of the selected option. Recorded alongside the index so a selection can still be
    // identified if option ordering changes between authoring sessions. Index remains the primary
    // replay key today; lineId is the more durable fallback to migrate toward.
    public string selectedOptionLineId;

    public VNChoiceRecord(
        int anchorHistoryIndex,
        string nodeName,
        int choiceIndexInNode,
        int selectedOptionIndex,
        string selectedOptionLineId)
    {
        this.anchorHistoryIndex = anchorHistoryIndex;
        this.nodeName = nodeName ?? "";
        this.choiceIndexInNode = choiceIndexInNode;
        this.selectedOptionIndex = selectedOptionIndex;
        this.selectedOptionLineId = selectedOptionLineId ?? "";
    }
}

public class ChoiceHistory
{
    public int NextChoiceIndex { get; set; }

    private readonly List<VNChoiceRecord> _choices = new();

    public List<VNChoiceRecord> CreateChoiceSnapshot() => new (_choices);


    public void AddChoiceRecord(IReadOnlyList<RollbackPoint> rollbackPoints, string nodeName, int choiceIndexInNode, int selectedOptionIndex, string selectedOptionLineId)
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
            selectedOptionIndex,
            selectedOptionLineId));
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
        for (int i = 0; i < choices.Count; i++)
        {
            _choices.Add(choices[i]);
        }
    }
    
    public void ClearChoiceRecords()
    {
        _choices.Clear();
        NextChoiceIndex = 0;
    }

    public void RemoveChoiceAnchorAfterRollbackPoint(RollbackPoint target)
    {
        NextChoiceIndex = 0;
        
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            if (_choices[i].anchorHistoryIndex > target.historyIndex)
                _choices.RemoveAt(i);
        }
    }
}