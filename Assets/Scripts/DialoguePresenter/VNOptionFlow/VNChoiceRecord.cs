using System;
using System.Collections.Generic;

[Serializable]
public struct VNChoiceRecord
{
    public int anchorHistoryIndex;

    // 장면 시작 이후 몇 번째 선택지 묶음인가 (0부터).
    public int choiceSequence;

    // 고른 선택지를 찾는 키가 둘이다. 순서 = 우선순위:
    //
    // (1) selectedOptionLineId - 위치와 무관.
    // 작가가 선택지 순서를 바꾸거나 사이에 하나를 끼워 넣어도 같은 것을 고른다.
    
    // (2) selectedOptionIndex  - 위치 기반.
    // lineId가 없는 옛 기록이거나 그 라인이 대본에서 사라졌을 때만 쓰는 폴백.
    public string selectedOptionLineId;
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
        this.selectedOptionLineId = selectedOptionLineId;
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