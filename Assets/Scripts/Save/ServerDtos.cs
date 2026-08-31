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

// POST /users/{userId}/playthroughs -> 201.
public sealed class PlaythroughCreatedDto
{
    public long PlaythroughId;
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

// '4xx', '5xx'.
public sealed class ErrorResponseDto
{
    public string Code;
    public string Message;
}