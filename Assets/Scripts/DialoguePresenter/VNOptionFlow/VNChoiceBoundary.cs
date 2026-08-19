using Yarn.Unity;

public sealed class VNChoiceBoundary
{
    private readonly ChoiceHistory _choiceHistory;
    private readonly RollbackHistory _rollbackController;

    public VNChoiceBoundary(
        ChoiceHistory choiceHistory,
        RollbackHistory rollbackController)
    {
        _choiceHistory = choiceHistory;
        _rollbackController = rollbackController;
    }

    // Reserves this option set's choice index within the node and advances the cursor.
    // The index is stable across replay.
    public int ReserveChoiceIndex()
    {
        int choiceIndexInNode = _choiceHistory.NextChoiceIndex;
        _choiceHistory.NextChoiceIndex++;
        return choiceIndexInNode;
    }

    public bool TryResolveReplayOption(int choiceIndexInNode, DialogueOption[] sourceOptions, out DialogueOption option)
    {
        option = null;

        if (!_choiceHistory.TryGetChoiceRecord(choiceIndexInNode, out VNChoiceRecord record))
            return false;

        if (record.selectedOptionIndex < 0 || record.selectedOptionIndex >= sourceOptions.Length)
            return false;

        DialogueOption candidate = sourceOptions[record.selectedOptionIndex];

        if (candidate == null || !candidate.IsAvailable)
            return false;

        option = candidate;
        return true;
    }

    public void CommitSelection(string nodeName, int choiceIndexInNode, int selectedOptionIndex, string selectedOptionLineId)
    {
        _choiceHistory.AddChoiceRecord(
            _rollbackController.Points,
            nodeName,
            choiceIndexInNode,
            selectedOptionIndex,
            selectedOptionLineId);
    }
}