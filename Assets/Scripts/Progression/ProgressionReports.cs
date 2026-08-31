using System.Collections.Generic;
using Ked.Progression;

// 드라이버 ↔ 저장 층 사이를 오가는 값들 (M7). 진행 순서에는 어느 것도 개입하지 않는다.

// 선택 커밋 한 건.
public readonly struct ChoiceCommitReport
{
    public string ChapterId { get; }

    // 선택지가 붙어 있던 에피소드.
    public string FromEpisodeId { get; }

    // 원본 NextOptions에서의 서수 (ResolvedOption.SourceIndex).
    public int OptionIndex { get; }

    public EpisodeOption Chosen { get; }

    // 커밋이 만든 새 상태. CurrentEpisodeId가 도착 에피소드다.
    public ProgressionState NewState { get; }

    public ChoiceCommitReport(
        string chapterId, string fromEpisodeId, int optionIndex,
        EpisodeOption chosen, ProgressionState newState)
    {
        ChapterId = chapterId;
        FromEpisodeId = fromEpisodeId;
        OptionIndex = optionIndex;
        Chosen = chosen;
        NewState = newState;
    }
}

// EventKey 에피소드 완주 한 건.
public readonly struct EpisodeWatchReport
{
    public string ChapterId { get; }
    public string EpisodeId { get; }
    public string EventKey { get; }

    public EpisodeWatchReport(string chapterId, string episodeId, string eventKey)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        EventKey = eventKey;
    }
}

// 저장에서 읽은 "어디서부터" (D-017 — 에피소드 단위). 콘텐츠와의 대조는 런처가 한다.
public sealed class ProgressionResumePoint
{
    public string ChapterId { get; }
    public string EpisodeId { get; }
    public IReadOnlyDictionary<string, int> Stats { get; }

    public ProgressionResumePoint(
        string chapterId, string episodeId, IReadOnlyDictionary<string, int> stats)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        Stats = stats;
    }
}
