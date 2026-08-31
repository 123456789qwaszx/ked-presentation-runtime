using System.Collections.Generic;

namespace Ked.Save
{
    // 디스크에 눕는 모양들 (M7). 전부 Newtonsoft가 채우는 단순 데이터 가방이라
    // public 필드로 둔다 — 불변 보장은 이 파일들을 혼자 만지는 쪽(스토어·큐)의 일이다.
    //
    // 시각은 전부 ISO-8601 UTC **문자열**("2026-08-31T07:00:00.0000000Z")이다.
    // DateTime으로 두면 Newtonsoft의 자체 날짜 파싱·Kind 처리에 얽힌다 — 서버 규약이
    // 문자열(D-009, `Z`)이므로 클라도 문자열로 들고 다니고, 만들 때만 UtcNow.ToString("o")를 쓴다.

    // saves/slot{n}.json — 로컬이 진실인 세이브 한 슬롯 (M7-2).
    // 서버 PUT의 snapshot 필드에도 이 객체가 통째로 실린다 — 서버는 열지 않으므로(PLAN 1.4)
    // 모양은 클라 마음이고, 두 모양을 따로 두면 언젠가 갈린다.
    public sealed class LocalSaveFile
    {
        public int SlotNo;
        public string ChapterId;
        public string CurrentEpisodeId;
        public Dictionary<string, int> Stats = new Dictionary<string, int>();
        public int PlaySeconds;
        public string SavedAtUtc;
    }

    // saves/sync_queue.json — 서버로 아직 못 보낸 것 전부 + 서버에 대해 아는 것 전부 (M7-3).
    //
    // NextSeq가 큐와 **같은 파일**에 있는 것이 설계다(M7 계획 C1): seq 발급과 적재가
    // 한 번의 원자적 쓰기라서, 앱이 어느 순간 죽어도 "seq는 나갔는데 큐에 없다"거나
    // 그 반대인 상태가 생기지 않는다. seq가 되돌아가면 서버 UNIQUE(save_slot_id, seq)가
    // 재전송으로 오인한다.
    //
    // BaseRevision·PlaythroughId도 여기다 — 세이브 내용이 아니라 **동기화 상태**라서
    // slot{n}.json이 아니라 이 파일이 맞다. 슬롯이 여럿이 되면 슬롯별로 갈라야 하는 값
    // (NextSeq·BaseRevision·PendingChoices)이 있지만, M7은 슬롯 1 하나만 쓴다 —
    // 갈라야 할 때 이 파일에 슬롯 차원을 넣는 것이 M8 이후의 일이다.
    public sealed class SyncQueueFile
    {
        public int SlotNo = 1;
        public long? PlaythroughId;
        public int NextSeq = 1;

        // 직전 성공 응답의 revision. null이면 서버에 이 슬롯이 아직 없다 → PUT에 0을 보낸다
        // (서버 규칙: 신규 슬롯은 baseRevision 0).
        public long? BaseRevision;

        public List<PendingChoice> PendingChoices = new List<PendingChoice>();
        public List<PendingEvent> PendingEvents = new List<PendingEvent>();
    }

    // 서버 ChoiceUpload와 필드가 1:1이라(camelCase로 내려가면 이름까지 같다)
    // 요청에 이 목록을 그대로 싣는다 — 옮겨 담는 계층을 만들지 않는다.
    public sealed class PendingChoice
    {
        public int Seq;
        public string EpisodeId;   // 선택지가 붙어 있던(출발) 에피소드
        public int OptionIndex;    // 원본 NextOptions 서수
        public string ChosenAt;    // ISO-8601 UTC
    }

    // 서버 EventUpload와 1:1.
    public sealed class PendingEvent
    {
        public string EpisodeId;
        public string OccurredAt;  // ISO-8601 UTC
    }

    // account.json — 게스트 계정 (M7, D-016).
    //
    // 비밀번호가 파일에 있는 것이 이상해 보이지만, 게스트 계정은 **이 설치가 곧 신원**이다 —
    // 사용자가 정한 비밀번호가 아니라 설치 시 생성한 난수이고, 이 파일을 잃으면 계정도
    // 같이 잃는 것이 게스트의 계약이다. 사람이 만든 계정(로그인 UI)은 뒤 M의 일이고,
    // 그때는 비밀번호 대신 토큰만 남는다.
    public sealed class AccountFile
    {
        public string Username;
        public string Password;
        public long? UserId;
        public string Token;
        public string ExpiresAtUtc;
    }
}
