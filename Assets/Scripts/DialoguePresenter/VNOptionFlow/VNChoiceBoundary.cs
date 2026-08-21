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

    // 이 선택지 묶음의 장면 내 순번을 예약하고 커서를 전진.
    public int ReserveChoiceSequence()
    {
        int choiceSequence = _choiceHistory.NextChoiceSequence;
        _choiceHistory.NextChoiceSequence++;
        return choiceSequence;
    }

    public bool TryResolveReplayOption(int choiceSequence, DialogueOption[] sourceOptions, out DialogueOption option)
    {
        option = null;

        if (!_choiceHistory.TryGetChoiceRecord(choiceSequence, out VNChoiceRecord record))
            return false;

        if (record.selectedOptionIndex < 0 || record.selectedOptionIndex >= sourceOptions.Length)
            return false;

        DialogueOption candidate = sourceOptions[record.selectedOptionIndex];

        if (candidate == null || !candidate.IsAvailable)
            return false;

        option = candidate;
        return true;
    }

    public void CommitSelection(string nodeName, int choiceSequence, int selectedOptionIndex, string selectedOptionLineId)
    {
        _choiceHistory.AddChoiceRecord(
            _rollbackController.Points,
            nodeName,
            choiceSequence,
            selectedOptionIndex,
            selectedOptionLineId);
    }
}