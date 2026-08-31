using System.Collections.Generic;
using Ked.Progression;

// 드라이버가 바깥(저장 층)에 알리는 사실들 (M7).
//
// 드라이버 주석의 원칙 그대로 — "순서를 두 곳에 적으면 갈린다". 저장 층은 이 보고를
// 받아 적기만 하고, 진행 순서에는 어떤 식으로도 개입하지 않는다(반환값 없음, 구독 선택).
//
// 보고가 나르는 것은 서버 이력(choice_history·event_log)이 요구하는 딱 그 재료다:
// 선택은 {어느 에피소드에서, 몇 번째 간선을, 언제} — seq와 시각은 저장 층이 매긴다
// (seq는 로컬 영속 값이고 시각은 기록 시점이므로, 진행 코어가 알 일이 아니다).

// 선택 커밋 한 건. Commit이 성공한 직후에 난다 — 스탯 반영과 이동이 끝난 상태다.
public readonly struct ChoiceCommitReport
{
    public readonly string ChapterId;

    // 선택지가 붙어 있던 에피소드 — 서버 choice_history.episode_id가 가리키는 것.
    public readonly string FromEpisodeId;

    // 원본 EpisodeNode.NextOptions에서의 서수 (0부터). 화면에 뜬(걸러진) 목록의 번호가
    // 아니다 — 서버는 option_count(원본 간선 수)로 범위를 검사한다.
    public readonly int OptionIndex;

    public readonly EpisodeOption Chosen;

    // 커밋이 만든 새 상태. CurrentEpisodeId가 곧 도착 에피소드다.
    public readonly ProgressionState NewState;

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

// EventKey가 달린 에피소드의 대사가 끝까지 재생됐다 — 서버 event_log의 재료.
// 판정(Resolve)이나 선택보다 앞, 대사 완료 직후에 난다. "시청 완료 시 트리거"라는
// EventKey의 정의(EpisodeNode 주석)를 그대로 따른 시점이다.
public readonly struct EpisodeWatchReport
{
    public readonly string ChapterId;
    public readonly string EpisodeId;
    public readonly string EventKey;

    public EpisodeWatchReport(string chapterId, string episodeId, string eventKey)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        EventKey = eventKey;
    }
}

// "어디서부터 다시"의 재료 (M7, D-017 — 재개는 에피소드 단위).
// 저장 층이 로컬 세이브에서 읽어 드라이버에 건넨다. 드라이버는 이것으로
// ProgressionState.Restore를 부르고, 그 에피소드의 대사를 처음부터 재생한다 —
// 장면 중간(노드 안 라인) 복원은 뒤 M의 일이다.
public sealed class ProgressionResumePoint
{
    public readonly string ChapterId;
    public readonly string EpisodeId;
    public readonly IReadOnlyDictionary<string, int> Stats;

    public ProgressionResumePoint(
        string chapterId, string episodeId, IReadOnlyDictionary<string, int> stats)
    {
        ChapterId = chapterId;
        EpisodeId = episodeId;
        Stats = stats;
    }
}
