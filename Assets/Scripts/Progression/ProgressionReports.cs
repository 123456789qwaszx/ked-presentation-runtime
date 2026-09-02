using System.Collections.Generic;
using Ked.Progression;

// 드라이버 ↔ 저장 층 사이를 오가는 값들. 진행 순서에는 어느 것도 개입하지 않는다.

// 장면 안에서 확정된 선택 하나. 서버 큐의 ChoiceUpload와 대응.
public readonly struct CommittedChoice
{
    public string FromEpisodeId { get; } // 선택지가 붙어 있던 에피소드.
    public int OptionIndex { get; }      // 원본 NextOptions에서의 서수.

    public CommittedChoice(string fromEpisodeId, int optionIndex)
    {
        FromEpisodeId = fromEpisodeId;
        OptionIndex = optionIndex;
    }
}

// 장면에 들어섰다 — 진입 스냅샷. 장면 기록의 앞부분이고, 장면이 끝나면 경로가 뒤에 붙는다.
public sealed class SceneEntryReport
{
    public string ChapterId { get; }
    public ProgressionState State { get; }          // 진입 상태. CurrentEpisodeId가 장면 루트.
    public YarnVariableSnapshot Variables { get; }  // 진입 시점 [3].
    public int BacklogSerialStart { get; }          // 이 장면의 첫 라인이 받을 순번.

    public SceneEntryReport(
        string chapterId, ProgressionState state, YarnVariableSnapshot variables, int backlogSerialStart)
    {
        ChapterId = chapterId;
        State = state;
        Variables = variables;
        BacklogSerialStart = backlogSerialStart;
    }
}

// 장면 하나가 끝났다 — 여기가 커밋이고 저장 단위다.
//
// State는 fold 결과 = 다음 장면의 진입 스냅샷(CurrentEpisodeId가 다음 장면 루트).
// ChapterCompleted면 다음 장면이 없다 — State는 기록으로 남고 재개 지점은 아니다.
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

// 저장에서 읽은 "어디서부터". 콘텐츠와의 대조·루트 검사는 런처가 한다.
public sealed class ProgressionResumePoint
{
    public string ChapterId { get; }
    public string EpisodeId { get; }
    public IReadOnlyDictionary<string, int> Stats { get; }
    public YarnVariableSnapshot Variables { get; }   // 없으면 null(구세이브) — 덮지 않는다.
    public IReadOnlyList<DialogueLogEntry> Backlog { get; } // 이전 장면들의 백로그. 없으면 null(구세이브).
    public bool ChapterCompleted { get; }

    public ProgressionResumePoint(
        string chapterId,
        string episodeId,
        IReadOnlyDictionary<string, int> stats,
        YarnVariableSnapshot variables,
        IReadOnlyList<DialogueLogEntry> backlog,
        bool chapterCompleted)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        Stats = stats;
        Variables = variables;
        Backlog = backlog;
        ChapterCompleted = chapterCompleted;
    }
}
