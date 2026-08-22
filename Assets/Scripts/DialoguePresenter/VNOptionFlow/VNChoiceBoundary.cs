using System;
using UnityEngine;
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

        bool hasLineId = !string.IsNullOrEmpty(record.selectedOptionLineId);

        if (hasLineId && TryFindByLineId(sourceOptions, record.selectedOptionLineId, out option))
            return true;

        if (!TryFindByIndex(sourceOptions, record.selectedOptionIndex, out option))
            return false;

        // lineId를 적어 뒀는데 그 라인이 지금 대본에 없다 -
        // 대본이 바뀐 것이고, 위치로 기반으로 고른 것.
        if (hasLineId)
        {
            Debug.LogWarning(
                $"[VNChoiceBoundary] 기록된 선택지 라인이 대본에 없어 위치로 복원했다. " +
                $"choiceSequence={choiceSequence}, lineId='{record.selectedOptionLineId}', " +
                $"index={record.selectedOptionIndex}. 리플레이가 다른 선택지를 고를 수 있다.");
        }

        return true;
    }

    private static bool TryFindByLineId(
        DialogueOption[] sourceOptions,
        string lineId,
        out DialogueOption option)
    {
        for (int i = 0; i < sourceOptions.Length; i++)
        {
            DialogueOption candidate = sourceOptions[i];

            if (!candidate.IsAvailable)
                continue;

            if (!string.Equals(candidate.Line.TextID, lineId, StringComparison.Ordinal))
                continue;

            option = candidate;
            return true;
        }

        option = null;
        return false;
    }

    private static bool TryFindByIndex(
        DialogueOption[] sourceOptions,
        int index,
        out DialogueOption option)
    {
        option = null;

        if (index < 0 || index >= sourceOptions.Length)
            return false;

        DialogueOption candidate = sourceOptions[index];

        if (!candidate.IsAvailable)
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