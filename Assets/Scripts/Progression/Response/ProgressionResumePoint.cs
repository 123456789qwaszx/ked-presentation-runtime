using System.Collections.Generic;

// 재개 지점
public sealed class ProgressionResumePoint
{
    public string ChapterId { get; }
    public string EpisodeId { get; }
    public IReadOnlyDictionary<string, int> Stats { get; }
    public YarnVariableSnapshot Variables { get; }   // 없으면 null(구세이브)
    public IReadOnlyList<DialogueLogEntry> Backlog { get; } // 이전 장면들의 백로그. 없으면 null(구세이브).
    public SavedLoadPlan LoadPlan { get; }           // 첫 장면에서 표적 라인까지 달리는 계획. 없으면 루트에서.
    public bool ChapterCompleted { get; }

    public ProgressionResumePoint(
        string chapterId,
        string episodeId,
        IReadOnlyDictionary<string, int> stats,
        YarnVariableSnapshot variables,
        IReadOnlyList<DialogueLogEntry> backlog,
        SavedLoadPlan loadPlan,
        bool chapterCompleted)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        Stats = stats;
        Variables = variables;
        Backlog = backlog;
        LoadPlan = loadPlan;
        ChapterCompleted = chapterCompleted;
    }
}