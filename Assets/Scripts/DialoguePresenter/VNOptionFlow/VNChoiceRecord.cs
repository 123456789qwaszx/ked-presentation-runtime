using System;
using System.Collections.Generic;

[Serializable]
public struct VNChoiceRecord
{
    public int anchorHistoryIndex;

    // 장면 시작 이후 몇 번째 선택지 묶음인가 (0부터).
    public int choiceSequence;
    public int selectedOptionIndex;

    public VNChoiceRecord(
        int anchorHistoryIndex,
        int choiceSequence,
        int selectedOptionIndex,
        string selectedOptionLineId)
    {
        this.anchorHistoryIndex = anchorHistoryIndex;
        this.choiceSequence = choiceSequence;
        this.selectedOptionIndex = selectedOptionIndex;
    }
}

public class ChoiceHistory
{
    public int NextChoiceSequence { get; set; }

    private readonly List<VNChoiceRecord> _choices = new();

    public List<VNChoiceRecord> CreateChoiceSnapshot() => new (_choices);


    public void AddChoiceRecord(IReadOnlyList<RollbackPoint> rollbackPoints, string nodeName, int choiceSequence, int selectedOptionIndex, string selectedOptionLineId)
    {
        if (rollbackPoints.Count <= 0)
            return;

        int anchorHistoryIndex = rollbackPoints[^1].historyIndex;

        // 같은 시퀀스의 옛 기록(롤백 전의 선택)을 덮는다.
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            if (_choices[i].choiceSequence == choiceSequence)
                _choices.RemoveAt(i);
        }

        _choices.Add(new VNChoiceRecord(
            anchorHistoryIndex,
            choiceSequence,
            selectedOptionIndex,
            selectedOptionLineId));
    }

    public bool TryGetChoiceRecord(int choiceSequence, out VNChoiceRecord record)
    {
        record = default;

        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            VNChoiceRecord choice = _choices[i];

            if (choice.choiceSequence != choiceSequence)
                continue;

            record = choice;
            return true;
        }

        return false;
    }

    public void RestoreChoices(IReadOnlyList<VNChoiceRecord> choices)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            _choices.Add(choices[i]);
        }
    }
    
    /// <summary>
    /// 장면 진입에서만 부름(EpisodePlayer.StartGameAsync).
    /// 롤백, 로드 시작 시 초기화 금지 - 리플레이는 이 기록으로 선택지를 자동 응답하며 복원함.
    /// </summary>
    public void ClearChoiceRecords()
    {
        _choices.Clear();
        NextChoiceSequence = 0;
    }

    public void RemoveChoiceAnchorAfterRollbackPoint(RollbackPoint target)
    {
        NextChoiceSequence = 0;
        
        for (int i = _choices.Count - 1; i >= 0; i--)
        {
            if (_choices[i].anchorHistoryIndex > target.historyIndex)
                _choices.RemoveAt(i);
        }
    }
}