using System.Collections.Generic;

// saves/slot{n}.json — 회차 파일. 로컬이 진실. (회차 폴더 구조는 F2에서.)
//
// 최상위 필드의 뜻은 둘 중 하나다: 장면 진입 스냅샷(CurrentEpisodeId = 장면 루트, Stats·Variables = 그 시점)
// 또는 챕터 완료(ChapterCompleted). 장면 중간을 가리키는 세이브는 만들지 않는다.
// 그 아래에 회차의 이력(Scenes)이 쌓인다 — 즐겨찾기와 갈라지기의 재료(save-plan.md v2).
public sealed class LocalSaveFile
{
    public int SlotNo;

    // 회차 id(로컬 guid). 서버 id는 큐 파일이 든다. 갈라진 회차면 출처가 ForkedFrom에.
    public string PlaythroughId;
    public ForkOrigin ForkedFrom;

    public string ChapterId;
    public string CurrentEpisodeId;
    public Dictionary<string, int> Stats = new();

    // [3] 연출 변수 통덤프. 없으면(구세이브) 덮지 않는다.
    public YarnVariableSnapshot Variables;

    // 챕터를 끝낸 세이브 — 이어갈 장면이 없다.
    public bool ChapterCompleted;

    // 이 회차가 지나온 장면 기록의 이력(현재 챕터 안). 갈라진 회차는 물려받은 것으로 시작한다.
    // 없으면(구세이브) 빈 목록.
    public List<SceneRecord> Scenes = new();

    // 현재 장면 이전의 백로그 항목들. 현재 장면의 것은 싣지 않는다 — 로드 뒤 재실행이 다시 적으며
    // 순번을 롤백 포인트와 나란히 세운다.
    public List<DialogueLogEntry> Backlog = new();

    // 갈라진 회차가 첫 장면에서 소비하는 로드 계획 — 장면 루트에서 표적 라인까지 경로대로 달린다.
    // 첫 장면이 끝나 저장되면 사라진다(소비됨). null이면 장면 루트에서 시작.
    public SavedLoadPlan PendingLoad;

    // 시간은 둘로 센다 — 갈라진 지점까지 물려받은 이야기상의 시간과, 이 회차에서 새로 플레이한 시간.
    public int InheritedPlaySeconds;
    public int OwnPlaySeconds;

    // 둘의 합. 서버 DTO와 구세이브가 이 이름을 쓴다.
    public int PlaySeconds;

    public string SavedAtUtc;
}

// 갈라진 출처 — 어느 회차의 어느 장면 기록, 어느 라인에서.
public sealed class ForkOrigin
{
    public string PlaythroughId;
    public int SceneIndex;
    public SaveLineTarget Target; // null이면 장면 루트.
}

// 라인 좌표 — 시크 표적과 같은 좌표계(노드·라인ID·장면 안 등장 순번).
public sealed class SaveLineTarget
{
    public string NodeName;
    public string LineId;
    public int Occurrence;
}

// 장면 진입 스냅샷. 로드가 재개할 수 있는 자리는 이것뿐이다.
public sealed class SceneCheckpoint
{
    public string ChapterId;
    public string EpisodeId;   // 장면 루트.
    public Dictionary<string, int> Stats = new();
    public YarnVariableSnapshot Variables;

    // 이 장면의 첫 라인이 받을 백로그 순번.
    public int BacklogSerialStart;

    // 이 장면에 들어설 때까지 큐에 적힌 마지막 선택 seq. 없으면 0.
    public int LastChoiceSeq;

    // 들어설 때의 누적 플레이 시간(계승 + 자체). 갈라지기가 물려받는 값.
    public int PlaySecondsAtEntry;

    public string EnteredAtUtc;
}

// 장면 기록 하나 = 진입 스냅샷 + 그 장면 안에서 지나온 경로. 이 넷이면 장면 안 어느 라인이든 좌표가 선다.
public sealed class SceneRecord
{
    public SceneCheckpoint Checkpoint;

    // 장면 안에서 확정된 진행 선택.
    public List<SavedChoice> Path = new();

    // 장면 안 Yarn 인라인 선택(리플레이가 자동 응답하는 기록). 처음부터 싣는다 — 회고적 즐겨찾기의 재료.
    public List<VNChoiceRecord> YarnChoices = new();

    // 이 장면의 마지막 라인 순번 + 1 (= 다음 장면의 BacklogSerialStart).
    public int BacklogSerialEnd;
}

public sealed class SavedChoice
{
    public string FromEpisodeId;
    public int OptionIndex;
}

// 장면 루트에서 표적 라인까지 달리는 계획. 장면 기록의 경로 + Yarn 선택 + 표적.
// 경로가 표적 뒤까지 이어져 있어도 된다 — 표적에 닿아 시크가 꺼지면 나머지는 버려진다.
public sealed class SavedLoadPlan
{
    public List<SavedChoice> Path = new();
    public List<VNChoiceRecord> YarnChoices = new();
    public SaveLineTarget Target;
}

// saves/sync_queue.json - 서버로 아직 못 보낸 것과, 서버에 대해 아는 것.
//
// NextSeq가 큐와 같은 파일에 있는 것: 발급과 적재가 한 번의 쓰기.
// 아직 단일 슬롯.
// 슬롯을 늘릴 때, NextSeq/BaseRevision/PendingChoices에 슬롯 계층 추가.
public sealed class SyncQueueFile
{
    public int SlotNo = 1;
    public long? PlaythroughId;
    public int NextSeq = 1;

    public long? BaseRevision;

    public List<PendingChoice> PendingChoices = new();
    public List<PendingEvent> PendingEvents = new();
}

// 서버 ChoiceUpload와 대응.
public sealed class PendingChoice
{
    public int Seq;
    public string EpisodeId; // 선택지가 붙어 있던 에피소드
    public int OptionIndex;  // 원본 NextOptions 서수
    public string ChosenAt;
}

// 서버 EventUpload와 대응.
public sealed class PendingEvent
{
    public string EpisodeId;
    public string OccurredAt;
}

// account.json - (게스트 계정)
public sealed class AccountFile
{
    public string Username;
    public string Password;
    public long UserId;
    public string Token;
    public string ExpiresAtUtc;
}
// saves/bookmarks.json — 즐겨찾기 목록. 수 제한 없음.
public sealed class BookmarkFile
{
    public List<Bookmark> Items = new();
}

// 이력 위의 한 점 — 스스로 완결된 사본. 출처 회차 파일이 없어도 로드된다.
// 로드 = 이 점을 물려받아 새 회차로 갈라지기(루트에서 표적까지 달린다).
public sealed class Bookmark
{
    public string Id;
    public string Label;
    public string Preview;      // 라인 텍스트.
    public string CreatedAtUtc;

    // 출처. SceneIndex는 그 회차 이력에서 이 장면이 갖는(가질) 자리 — 앞의 기록을 물려받는 데 쓴다.
    public string PlaythroughId;
    public int SceneIndex;

    public string ChapterId;
    public SceneCheckpoint Checkpoint;   // 그 장면의 진입 스냅샷.
    public SavedLoadPlan Load;           // 그 장면 안 경로·Yarn 선택·표적 — 찍은 순간까지.
    public List<DialogueLogEntry> Backlog = new(); // 그 장면 이전의 항목들.

    // 찍은 순간까지의 누적 시간(계승 + 자체). 갈라진 회차가 물려받는다.
    public int PlaySecondsAtBookmark;
}

// 이력 화면이 회차 하나를 그리는 데 필요한 것. 파일을 열지 않고 목록을 그리려고 요약만 뽑는다.
public sealed class PlaythroughSummary
{
    public string PlaythroughId;
    public bool IsActive;
    public ForkOrigin ForkedFrom;     // null이면 새 게임으로 시작한 회차.
    public string ChapterId;
    public string CurrentEpisodeId;
    public bool ChapterCompleted;
    public int SceneCount;
    public int BookmarkCount;         // 0이고 활성이 아니면 UI가 접는다(기본).
    public int InheritedPlaySeconds;
    public int OwnPlaySeconds;
    public string SavedAtUtc;
}
