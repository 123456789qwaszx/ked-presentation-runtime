using System.Collections.Generic;
using Ked.Progression;

// (장면 종료 시 커밋할 저장 단위)
// - State는 fold 결과 = 다음 장면의 진입 스냅샷(CurrentEpisodeId가 다음 장면 루트).
// - ChapterCompleted면 다음 장면이 없음 - State는 기록으로 남기되 재개지점으로는 사용하지 않음.
public sealed class SceneCommitReport
{
    public string ChapterId { get; }
    public IReadOnlyList<CommittedChoice> Choices { get; }      // 확정 순서 = 큐 Seq 순서 = 장면 기록의 경로.
    public IReadOnlyList<VNChoiceRecord> YarnChoices { get; }   // 장면 안 Yarn 인라인 선택 기록.
    public IReadOnlyList<string> WatchedEpisodeIds { get; }     // EventKey가 달린 에피소드를 다 본 것.
    public ProgressionState State { get; }
    public YarnVariableSnapshot Variables { get; }              // [3] 통덤프. 장면 끝 시점.

    // 지금까지의 백로그 전부 — 다음 장면 입장에서는 "이전 장면들"이다.
    public IReadOnlyList<DialogueLogEntry> Backlog { get; }

    // 다음 장면의 첫 라인이 받을 백로그 순번 (= 이 장면의 BacklogSerialEnd).
    public int BacklogSerialStart { get; }

    public bool ChapterCompleted { get; }

    public SceneCommitReport(
        string chapterId,
        IReadOnlyList<CommittedChoice> choices,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        IReadOnlyList<string> watchedEpisodeIds,
        ProgressionState state,
        YarnVariableSnapshot variables,
        IReadOnlyList<DialogueLogEntry> backlog,
        int backlogSerialStart,
        bool chapterCompleted)
    {
        ChapterId = chapterId;
        Choices = choices;
        YarnChoices = yarnChoices;
        WatchedEpisodeIds = watchedEpisodeIds;
        State = state;
        Variables = variables;
        Backlog = backlog;
        BacklogSerialStart = backlogSerialStart;
        ChapterCompleted = chapterCompleted;
    }
}