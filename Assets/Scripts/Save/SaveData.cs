using System.Collections.Generic;

// 디스크에 눕는 모양들 (M7). Newtonsoft가 채우는 데이터 가방이라 public 필드.
// 시각은 전부 ISO-8601 UTC 문자열 — 서버 규약(D-009)과 같고, 만드는 자리는 UtcNow.ToString("o") 하나다.

// saves/slot{n}.json — 세이브 한 슬롯. 로컬이 진실이다.
// 서버 PUT의 snapshot에도 이 객체가 통째로 실린다 — 서버는 열지 않는다(PLAN 1.4).
public sealed class LocalSaveFile
{
    public int SlotNo;
    public string ChapterId;
    public string CurrentEpisodeId;
    public Dictionary<string, int> Stats = new Dictionary<string, int>();
    public int PlaySeconds;
    public string SavedAtUtc;
}

// saves/sync_queue.json — 서버로 아직 못 보낸 것과, 서버에 대해 아는 것.
//
// NextSeq가 큐와 같은 파일에 있는 것이 설계다(M7 계획 C1): 발급과 적재가 한 번의 쓰기라
// 앱이 어느 순간 죽어도 둘이 어긋나지 않는다.
// M7은 슬롯 1 하나만 쓴다 — 슬롯이 늘면 NextSeq·BaseRevision·PendingChoices에 슬롯 차원이 필요하다.
public sealed class SyncQueueFile
{
    public int SlotNo = 1;
    public long? PlaythroughId;
    public int NextSeq = 1;

    // 직전 성공 응답의 revision. null이면 서버에 이 슬롯이 아직 없다(→ PUT에 0).
    public long? BaseRevision;

    public List<PendingChoice> PendingChoices = new List<PendingChoice>();
    public List<PendingEvent> PendingEvents = new List<PendingEvent>();
}

// 서버 ChoiceUpload와 1:1 — camelCase로 내려가면 이름까지 같아 요청에 그대로 싣는다.
public sealed class PendingChoice
{
    public int Seq;
    public string EpisodeId;   // 선택지가 붙어 있던 에피소드
    public int OptionIndex;    // 원본 NextOptions 서수
    public string ChosenAt;
}

// 서버 EventUpload와 1:1.
public sealed class PendingEvent
{
    public string EpisodeId;
    public string OccurredAt;
}

// account.json — 게스트 계정 (D-016). 설치가 곧 신원이라 비밀번호(설치 시 생성한 난수)도 여기 있다.
public sealed class AccountFile
{
    public string Username;
    public string Password;
    public long UserId;
    public string Token;
    public string ExpiresAtUtc;
}
