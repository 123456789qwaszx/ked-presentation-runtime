using System.Collections.Generic;

// saves/slot{n}.json — 세이브 한 슬롯. 로컬이 진실.
public sealed class LocalSaveFile
{
    public int SlotNo;
    public string ChapterId;
    public string CurrentEpisodeId;
    public Dictionary<string, int> Stats = new();
    public int PlaySeconds;
    public string SavedAtUtc;
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