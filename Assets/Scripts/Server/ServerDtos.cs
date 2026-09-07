using System.Collections.Generic;

// 서버(spring-prepare)의 요청,응답 모양. 서버 record와 1:1 대응.
// - POST /auth/login -> 200.
// - 실패는 원인 구분 없는 401.
public sealed class LoginResponseDto
{
    public string Token;
    public long UserId;
    public string ExpiresAt;
}

// POST /users -> 201.
public sealed class UserResponseDto
{
    public long Id;
    public string Username;
}

// POST /users/{userId}/playthroughs 본문. 멱등 키는 회차 파일의 로컬 guid.
public sealed class PlaythroughCreateRequestDto
{
    public string ClientPlaythroughId;
    public ForkOriginDto ForkedFrom; // 갈래일 때만. 새 게임은 null.
}

// 서버 id는 보내지 않는다 — 서버가 같은 사용자 안에서 클라 id로 부모를 찾는다.
public sealed class ForkOriginDto
{
    public string ClientPlaythroughId;
    public int SceneIndex;
}

// POST -> 201(새로 만듦) / 200(이미 있었음). 둘 다 성공, 본문 같음.
public sealed class PlaythroughCreatedDto
{
    public long PlaythroughId;
    public string ClientPlaythroughId;
}

// GET /content/chapters/{chapterId}/versions -> 200, 배열.
public sealed class ChapterVersionInfoDto
{
    public int Version;
    public string ImportedAt;
    public string Checksum; // 업로드 원본 바이트의 SHA-256 hex
}

// PUT /playthroughs/{pid}/saves/{slotNo} 본문.
public sealed class SaveUploadRequestDto
{
    public string ChapterId;
    public int ChapterVersion;
    public string CurrentEpisodeId;
    public object Snapshot; // LocalSaveFile 통째
    public int PlaySeconds;
    public string DeviceKey;
    public long BaseRevision; // 신규 슬롯은 0
    public List<PendingChoice> Choices;
    public List<PendingEvent> Events;

    // 목록·통계가 스냅샷을 열지 않고 그리는 값. PlaySeconds는 여전히 둘의 합.
    public int InheritedPlaySeconds;
    public int OwnPlaySeconds;
    public bool ChapterCompleted;
}

// PUT -> 200.
// accepted*는 dedup 이후의 수치.
public sealed class SaveUploadResponseDto
{
    public long Revision;
    public string UpdatedAt;
    public int AcceptedChoices;
    public int AcceptedEvents;
    public bool Replayed;
}

// GET /users/{uid}/playthroughs -> 200, 배열. 슬롯 1의 값은 슬롯이 없으면 null(키는 있다).
public sealed class PlaythroughSummaryDto
{
    public long Id;
    public string ClientPlaythroughId; // 옛 회차(F6 전)는 null
    public ForkOriginResponseDto ForkedFrom;
    public string StartedAt;
    public string EndedAt;
    public int SlotCount;
    public string ChapterId;
    public int? ChapterVersion;
    public string CurrentEpisodeId;
    public bool? ChapterCompleted;
    public int? InheritedPlaySeconds;
    public int? OwnPlaySeconds;
    public int? PlaySeconds;
    public int BookmarkCount;
    public string LastSavedAt;
}

public sealed class ForkOriginResponseDto
{
    public long? PlaythroughId; // null이면 부모가 아직 서버에 없다
    public string ClientPlaythroughId;
    public int SceneIndex;
}

// GET /playthroughs/{pid}/saves/{slotNo} -> 200. snapshot은 우리가 올린 것 그대로(LocalSaveFile).
public sealed class SaveSlotDetailDto
{
    public int SlotNo;
    public string ChapterId;
    public int ChapterVersion;
    public string CurrentEpisodeId;
    public long Revision;
    public int PlaySeconds;
    public int InheritedPlaySeconds;
    public int OwnPlaySeconds;
    public bool ChapterCompleted;
    public string UpdatedAt;
    public string Device;
    public Newtonsoft.Json.Linq.JToken Snapshot;
}

// GET /playthroughs/{pid}/saves/{slotNo}/choices?afterSeq=N -> 200, 배열(seq 순).
public sealed class ChoiceHistoryItemDto
{
    public int Seq;
    public string EpisodeId;
    public int OptionIndex;
    public string ChosenAt;
}

// PUT /users/{uid}/bookmarks/{clientBookmarkId} 본문. snapshot은 Bookmark 통째 — 서버는 열지 않는다.
public sealed class BookmarkUpsertRequestDto
{
    public string Label;
    public string Preview;
    public string ChapterId;
    public int ChapterVersion;
    public string PlaythroughClientId;
    public int SceneIndex;
    public string CreatedAt;
    public object Snapshot;
}

// PUT -> 201(신규) / 200(갱신·부활).
public sealed class BookmarkUpsertResponseDto
{
    public string ClientBookmarkId;
    public long? PlaythroughId;
    public string UpdatedAt;
}

// GET 목록의 한 줄(snapshot 없음) / GET 단건(snapshot 있음).
public sealed class BookmarkDetailDto
{
    public string ClientBookmarkId;
    public string Label;
    public string Preview;
    public string ChapterId;
    public int ChapterVersion;
    public string PlaythroughClientId;
    public long? PlaythroughId;
    public int SceneIndex;
    public string CreatedAt;
    public string UpdatedAt;
    public Newtonsoft.Json.Linq.JToken Snapshot;
}

// '4xx', '5xx'.
public sealed class ErrorResponseDto
{
    public string Code;
    public string Message;
}