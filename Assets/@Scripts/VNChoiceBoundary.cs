using Yarn.Unity;

// Owns reads and writes against the choice history and rollback history for one option set.
// The options flow goes through this boundary instead of touching ChoiceHistory / RollbackController
// directly. Unlike VNYarnLineBoundary there is no meta carrier: an option set only touches the
// choice history, so the boundary exposes those reads and writes directly.
public sealed class VNChoiceBoundary
{
    private readonly ChoiceHistory _choiceHistory;
    private readonly RollbackController _rollbackController;

    public VNChoiceBoundary(
        ChoiceHistory choiceHistory,
        RollbackController rollbackController)
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

    // Resolves a recorded choice into a usable option against the current option set.
    // Fails when no record exists, the record points outside the set, or the option is unavailable.
    public bool TryResolveReplayOption(int choiceIndexInNode, DialogueOption[] sourceOptions, out DialogueOption option)
    {
        option = null;

        if (sourceOptions == null)
            return false;

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