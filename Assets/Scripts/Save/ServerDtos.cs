using System.Collections.Generic;

namespace Ked.Save
{
    // 서버(spring-prepare)의 요청·응답 모양 (M7-6). 서버 레포의 record와 1:1이고,
    // SaveJson(camelCase)으로 내려가면 필드 이름까지 같아진다.
    //
    // 시각 필드는 전부 문자열이다 — SaveData.cs 머리 주석과 같은 이유.

    // POST /auth/login → 200. 실패는 원인 구분 없는 401 (M6).
    public sealed class LoginResponseDto
    {
        public string Token;
        public long UserId;
        public string ExpiresAt;   // UTC, 발급 후 24h
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
        public string Checksum;    // 업로드 원본 바이트의 SHA-256 hex — D-015가 대조하는 값
    }

    // PUT /playthroughs/{pid}/saves/{slotNo} 요청 본문.
    public sealed class SaveUploadRequestDto
    {
        public string ChapterId;
        public int ChapterVersion;
        public string CurrentEpisodeId;

        // 서버는 열지 않는다(PLAN 1.4) — LocalSaveFile 객체를 그대로 싣는다.
        public object Snapshot;

        public int PlaySeconds;
        public string DeviceKey;

        // "내가 알던 서버 상태". 신규 슬롯은 0 (서버 규칙 — null이면 400).
        public long BaseRevision;

        public List<PendingChoice> Choices;
        public List<PendingEvent> Events;
    }

    // PUT → 200.
    public sealed class SaveUploadResponseDto
    {
        public long Revision;
        public string UpdatedAt;

        // "흡수 후" 수치다 (M6-2b·D-011). 보낸 수보다 작아도 정상 —
        // 큐를 비우는 판정에 이 값을 **쓰지 않는다**.
        public int AcceptedChoices;
        public int AcceptedEvents;
        public bool Replayed;
    }

    // 모든 4xx/5xx의 공통 모양 (D-004). 409의 current 등 나머지 필드는
    // RawBody로 보존된다 — M8의 충돌 UI가 그때 제대로 파싱한다.
    public sealed class ErrorResponseDto
    {
        public string Code;
        public string Message;
    }
}
