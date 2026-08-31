using System.Collections.Generic;

// 서버(spring-prepare)의 요청·응답 모양 (M7). 서버 record와 1:1 — SaveJson으로 내려가면 이름까지 같다.

// POST /auth/login → 200. 실패는 원인 구분 없는 401.
public sealed class LoginResponseDto
{
    public string Token;
    public long UserId;
    public string ExpiresAt;
}

// POST /users → 201.
public sealed class UserResponseDto
{
    public long Id;
    public string Username;
}

// POST /users/{userId}/playthroughs → 201.
public sealed class PlaythroughCreatedDto
{
    public long PlaythroughId;
}

// GET /content/chapters/{chapterId}/versions → 200, 배열.
public sealed class ChapterVersionInfoDto
{
    public int Version;
    public string ImportedAt;
    public string Checksum;    // 업로드 원본 바이트의 SHA-256 hex (D-015가 대조하는 값)
}

// PUT /playthroughs/{pid}/saves/{slotNo} 본문.
public sealed class SaveUploadRequestDto
{
    public string ChapterId;
    public int ChapterVersion;
    public string CurrentEpisodeId;
    public object Snapshot;            // LocalSaveFile 통째 — 서버는 열지 않는다
    public int PlaySeconds;
    public string DeviceKey;
    public long BaseRevision;          // 신규 슬롯은 0
    public List<PendingChoice> Choices;
    public List<PendingEvent> Events;
}

// PUT → 200. accepted*는 "흡수 후" 수치(D-011) — 큐를 비우는 판정에 쓰지 않는다.
public sealed class SaveUploadResponseDto
{
    public long Revision;
    public string UpdatedAt;
    public int AcceptedChoices;
    public int AcceptedEvents;
    public bool Replayed;
}

// 모든 4xx/5xx의 공통 모양 (D-004).
public sealed class ErrorResponseDto
{
    public string Code;
    public string Message;
}
