using System;

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