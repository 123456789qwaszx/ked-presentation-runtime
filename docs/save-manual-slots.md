# 수동 저장·이어하기·회차 보존

브랜치: `server_DB_Test`. 회차 envelope/전환 설계는 [save-lifecycle-v3.md](save-lifecycle-v3.md).

## 이번 결정

기존 Bookmark가 VN의 수동 세이브 슬롯이다. 별도의 즐겨찾기와 슬롯 모델을 만들지 않는다.
현재까지 도달한 체크포인트·선택 경로·대사 위치·완료 장면 기록을 캡처한다.
이후 진행은 슬롯을 변경하지 않고, 불러오기는 슬롯 내용에서 새 회차를 만든다.
임의의 미래 지점을 UI에서 선택해 저장하는 기능은 없다. 캡처 API는 런타임이 공급한 경로와 target을 신뢰한다.

- 수동 슬롯: 개수/기간 제한 없이 보존. 사용자의 삭제 또는 같은 ID 덮어쓰기만 기존 저장 내용을 제거한다.
- 이어하기: active 회차의 기존 자동 저장 지점 한 개. 현재 구현은 첫 장면 진입/장면 커밋 단위이며 매 대사 자동 저장은 아니다.
- 내부 회차: 수동 슬롯이나 이어하기가 참조하는 데이터, 미완료 동기화 작업을 보존한다. 영구적인 사용자 저장 슬롯이 아니다.
- 마이그레이션은 추가하지 않는다. 이전 파일을 새 형식으로 변환하는 작업은 범위 밖이다.
- 기존 작업 1·2·3은 사용자 판정으로 통과. 4(저장 상태 UI)는 보류. 이번 구현은 5·6·7에 해당한다.
  이전 Unity 테스트 통과 보고와 별개로, 이번 변경의 Unity/실제 서버 일괄 검증은 아직 필요하다.

## 생명주기와 실제 삭제 시점

상태 하나를 enum으로 저장하지 않고 참조와 전송 상태로 판정한다. 아래 보호 조건은 중첩될 수 있다.

| 대상/조건 | 처리 |
|---|---|
| active 회차 | 이어하기용으로 보존 |
| 남아 있는 수동 슬롯의 원본 회차 | 슬롯이 참조하는 동안 보존 |
| 미전송 커밋/pending 또는 InFlight | 서버 사용 시 업로드 완료 전까지 보존 |
| 해소되지 않은 충돌/영구 오류 | 자동 삭제하지 않음 |
| 미완료 conflict-transfer journal | 정리 전체를 미룸 |
| 북마크 요약 복구가 아직 진행 중 | 미발견 참조 보호를 위해 회차 정리를 미룸 |
| 위 조건이 없는 비활성 회차 | 유지보수 tick에서 full JSON 제거 |
| 수동 슬롯 삭제 | index에서 제거 + 삭제 ID 기록을 원자적으로 확정한 직후 snapshot 파일 정리 |
| 수동 슬롯 덮어쓰기 | 새 snapshot 파일 확보 → index 교체 성공 → 이전 snapshot 파일 정리 |

유지보수는 Unity Update에서 약 5초마다 기회를 얻는다. 진행 중인 네트워크 작업/복구가 있으면 정리가 늦어질 수 있다.
파일 삭제가 실패하면 다음 유지보수에서 재시도한다. 날짜나 최근 N개 기준은 사용하지 않는다.
서버 없이 실행하는 설정에서는 전송할 서버가 없으므로 미참조 회차의 dirty 상태만으로 보존하지 않는다.
단, 기존 InFlight와 충돌/영구 오류는 이 경우에도 남긴다.

북마크를 삭제해도 같은 회차를 다른 슬롯이나 이어하기가 참조하면 회차 JSON은 남는다.
북마크 출처와 달리 단순 ForkedFrom은 계보 식별자다. 새 회차는 재생 데이터를 자체 보유하므로 조상 파일 전체를 영원히 붙잡지 않는다.
409의 작업 이전은 destination을 먼저 영속화하고 journal 완료 후 source를 정리할 수 있다.
서버의 옛 playthrough 행/분석 이력 전체 삭제는 이번 로컬 정책에 포함하지 않는다. 서버 복구에서 이들을 자동으로 되살리지는 않는다.

## 파일 및 저장 원자성

`saves/` 아래:

| 파일 | 내용 |
|---|---|
| `bookmarks.json` | 슬롯 요약, snapshot 키, LocalVersion/SyncedVersion, 동기화 상태, PendingDeletes/DeletedIds |
| `bookmark-snapshots/{key}.json` | 특정 슬롯 버전의 체크포인트·재생 계획·Backlog·Scenes 전체 |
| `playthroughs-v3/{id}.json` | 회차 snapshot과 outbox |
| `active.json` | 이어하기 ID, SelectionVersion, SelectionScopeId |
| `playthrough-catalog.json` | 회차 목록의 파생 요약 캐시 |
| `restore-progress.json` | 이어하기 완료 여부, 북마크 목록 cursor/완료 여부 |

슬롯 본문은 새 키에 먼저 기록한다. index 쓰기 실패 시 이전 index/본문은 유지된다.
그 사이 종료로 남은 미참조 본문은 다음 정상 저장 또는 유지보수에서 수거한다.
삭제 ID는 본문 없이 남기는 재등장 방지 기록이며, 이 기록 자체의 만료/압축은 구현하지 않았다.
`Load`, `List`는 파일을 생성하지 않는다. `GetBookmarkAsync`는 필요 시 서버에서 본문을 가져와 캐시하는 명시적인 복구 API다.
`Bookmarks`는 이제 요약이다. Checkpoint/Load가 필요하면 `GetBookmarkAsync(id)`를 호출한다.

회차 catalog는 파일 크기/수정 시각이 바뀐 항목만 다시 만든다. 파손된 JSON 캐시는 재구성하고,
조회 중에는 쓰지 않으며 유지보수에서 변경된 캐시를 보존한다. catalog는 삭제 판단의 근거가 아니다.
디렉터리/북마크 index 순회는 여전히 O(N)이고, 처음 여는 회차는 원본 파싱이 필요하다.
이번 변경이 무제한 저장량에 대한 DB 수준의 인덱싱을 제공하는 것은 아니다.

## 재시도와 충돌

- 회차 전송의 일시 실패: 5 → 10 → 20초 … 최대 300초. 횟수와 다음 시각을 envelope에 보존하므로 재시작해도 즉시 반복하지 않는다.
- HTTP 4xx(408/429 제외): BlockedReason을 기록하고 자동 전송 중단. 원인 해결 후 RetryPlaythroughAsync 사용.
- 기존 서버 회차의 CONFLICT: 기존 v3대로 최신 로컬 내용을 새 회차로 이전한다.
- 신규 baseRevision 0의 CONFLICT: 자동 fork를 중단한다. 서버 원인 해결 후 ResolveConflictAsForkAsync로 명시적 해소 가능.
- 수동 슬롯 PUT/DELETE: 슬롯 ID별 직렬 처리. 오래된 PUT ACK는 최신 슬롯을 Synced로 표시하지 않는다.
  다만 성공한 버전은 SyncedVersion에 반영하여 다음 덮어쓰기의 기준으로 사용한다.
- 북마크 전송/삭제와 부분 복구는 유지보수에서 재시도하며 실패 대기 중 간격은 최대 300초까지 늘어난다.
  연결 복구 감지는 북마크/복구 재시도 시각을 앞당긴다. 회차별 영속 backoff는 유지한다.
- 북마크 영구 4xx는 SyncError로 자동 PUT 중단. RetryBookmarkAsync는 원인 해결 후 재전송용이다.
  버전 충돌은 무조건 재전송해서 해결하지 않는다. DuplicateBookmarkAsync로 로컬 내용을 새 슬롯에 보존한 뒤 원래 슬롯 처리를 사용자가 결정한다.
- 북마크의 이전 PUT 응답을 잃은 직후 추가 덮어쓰기한 경우에도 새 요청의 기준 버전이 서버와 달라지면 409로 멈출 수 있다.
  회차 outbox와 달리 북마크에 여러 이전 PUT의 영속 재전송 로그를 추가하지 않았다. 로컬 최신 내용은 보존된다.

## 복구 비용과 재생 전환

이어하기는 `GET resume` 한 건만 startup 장벽에 포함한다. 북마크 요약 페이지는 그 뒤에 받으며,
본문/과거 회차 전체/선택 이력 전체를 목록 표시용으로 받지 않는다. 페이지 저장 후 cursor를 기록해 중단 시 이어받는다.
페이지 재전송은 이미 있는 ID를 덮지 않고 로컬 DeletedIds도 존중한다.
처음 비어 있는 저장소에서 시작한 복구를 재개하는 방식이다. 실행 중 다른 기기의 변경을 계속 병합하는 서비스는 아니다.

선택한 북마크만 단건 GET 후 로컬에 캐시한다. 다운로드 중 슬롯이 삭제/덮어써졌다면 늦은 응답으로 복구하지 않는다.
다운로드 전용 `ProgressionLauncher.TransitionAfterAsync`는 재생을 멈추기 전에 본문을 준비하고,
그동안 다른 전환이 일어났다면 늦은 로드 요청을 취소한다. 기존 디버그 로드 입력도 이 경로를 사용한다.
한 번도 내려받지 않은 원격 슬롯은 오프라인에서 불러올 수 없다. 이미 로컬에 있는 수동 저장과 이어하기는 가능하다.

## UI에서 연결할 API (UI 구현은 보류)

| 목적 | API |
|---|---|
| 수동 슬롯 요약 | `SaveCoordinator.Bookmarks` |
| 도달한 현재 지점 신규 저장 | `CreateBookmark(path, yarnChoices, target, preview, label)` |
| 기존 슬롯 덮어쓰기 | `OverwriteBookmark(id, path, yarnChoices, target, preview, label)` |
| 단건 본문 확보 | `GetBookmarkAsync(id)` |
| 로드 | `TransitionAfterAsync(본문 확보, () => ForkFromBookmark(slot))` |
| 이름 변경 | `RenameBookmarkAsync(id, label)`; 동기 RenameBookmark는 본문이 로컬에 없으면 false |
| 삭제 | `DeleteBookmark(id)` |
| 충돌 로컬 내용 별도 보존 | `DuplicateBookmarkAsync(id, label)` |
| 회차/슬롯 수동 재시도 | `RetryPlaythroughAsync(id)` / `RetryBookmarkAsync(id)` |
| 중단된 회차 충돌에서 새 회차 생성 | `ResolveConflictAsForkAsync(id)`; 진행 중 sync가 있으면 예외 |

## 서버 구현 계약 — 이번 클라이언트에 맞춰 변경 필요

서버 구현/DB migration은 이 레포에서 수행하지 않았다. 필드명은 SaveJson의 camelCase 직렬화 기준이다.

### 이어하기 포인터

`PUT /users/{uid}/resume`

```json
{
  "clientPlaythroughId": "local-guid",
  "deviceKey": "device-key",
  "selectionScopeId": "save-installation-guid",
  "selectionVersion": 12
}
```

대상 회차의 슬롯 1 snapshot이 서버에 존재해야 성공한다. worker는 첫 저장 ACK 이후에만 보낸다.
과거 회차의 일반 저장 PUT은 이 포인터를 변경하면 안 된다.
`(user, selectionScopeId)`별 version을 비교하고 낮은 세대는 거부한다. 같은 세대/같은 대상은 멱등 성공으로만 처리하며
계정 전체 포인터를 다시 앞세우지 않는다. 같은 세대의 다른 대상은 거부한다.
SelectionScopeId는 active 파일에 영속화되는 새 저장소 단위 ID로, 로컬 초기화 후 세대가 0으로 돌아오는 경우를 구분한다.
여러 scope의 실제 신규 선택은 서버가 수락한 순서로 계정 이어하기 대상을 결정한다.
이 계약은 여러 기기 사이의 실제 사용자 행동 시각을 추론하지 않는다.

`GET /users/{uid}/resume`: 없으면 204. 있으면 200:

```json
{
  "playthrough": { "id": 123, "clientPlaythroughId": "local-guid", "chapterId": "chapter" },
  "save": { "revision": 7, "snapshot": { "playthroughId": "local-guid" } },
  "nextChoiceSeq": 42
}
```

예시 snapshot은 구조를 축약했다. 실제로는 기존 LocalSaveFile 전체를 반환한다.
`nextChoiceSeq`는 해당 슬롯에서 다음으로 사용할 선택 seq이며 snapshot/revision과 일관된 읽기로 제공한다.
포인터 조회를 `lastUpdated`가 가장 큰 회차를 고르는 쿼리로 대체하면 오래된 queue 업로드가 이어하기를 바꾸므로 안 된다.

### 수동 슬롯

`GET /users/{uid}/bookmarks?limit=50&cursor=...` → `{ "items": [BookmarkDetailDto 요약], "nextCursor": "..." }`.
끝은 null. 목록은 snapshot을 제외한다. cursor는 서버가 만든 불투명 값이며 안정적인 정렬 키/동일 키의 ID로 구성한다.
삭제가 있어도 offset처럼 항목을 건너뛰지 않는 방식으로 구현한다. 사용자+정렬 키 인덱스가 필요하다.
`GET /users/{uid}/bookmarks/{id}` → snapshot 포함 단건. 목록/단건 모두 `clientVersion`과 `updatedAt`을 반환한다.

PUT은 기존 본문에 `clientVersion`과 `baseVersion`을 추가한다.

1. 신규 ID: baseVersion=0, clientVersion>=1. 사용자가 전송 전에 여러 번 덮어쓸 수 있으므로 버전 건너뛰기는 허용한다.
2. 기존 ID: baseVersion이 현재 서버 clientVersion과 같고 새 clientVersion이 더 큰 경우에만 교체한다.
3. 동일 clientVersion/동일 요청은 먼저 멱등 재전송으로 판정해 성공시킨다. 다른 내용이면 409.
4. 기준 버전 불일치/낮은 버전은 409, 삭제 tombstone이 있는 ID는 모든 버전에 대해 410.
5. DELETE는 본문을 영구 제거하고 ID tombstone을 남긴다. 없는 ID/이미 삭제한 ID도 멱등 204.
   삭제가 완료된 ID는 PUT으로 부활시키지 않는다. 다시 저장하려면 새 GUID를 사용한다.
6. 버전 확인, 교체, 삭제 tombstone은 DB 트랜잭션으로 경쟁을 제어한다. DELETE는 버전과 무관한 사용자 명시 삭제다.

클라이언트의 슬롯 ID 직렬화만으로 서버/여러 기기 경합이 해결되지는 않는다. 위 계약이 함께 구현돼야 한다.
서버의 같은 snapshot 재전송 흡수 계약은 기존 회차 PUT에도 계속 필요하다.

## 일괄 검증

로컬 harness 25개 통과. 실제 Save/Restore/Sync/Coordinator/Launcher를 컴파일하고 HTTP/Unity 실행을 대역으로 바꾼다.
GitHub Actions도 같은 harness를 실행한다. 실제 서버 DB·Unity 재생 테스트 결과로 간주하지 않는다.

1. 로컬 전용: 슬롯 2개 저장 → 진행 → 슬롯 로드 → 새 진행. 원래 슬롯의 대사/선택/완료 장면 기록은 그대로여야 한다.
2. 같은 슬롯 덮어쓰기: ID/개수 유지, 내용 변경. index 쓰기 실패 시 이전 저장이 로드돼야 한다.
3. 수동 삭제: 슬롯/본문 제거. 마지막 참조가 사라진 동기화 완료 회차만 이후 정리되고 active/다른 슬롯은 남아야 한다.
4. 서버 중단: 과거 회차 pending을 남긴 채 새 게임 가능. 서버 재가동 후 tick으로 전송되고 미참조 완료 회차 정리.
5. 지연 PUT 중 덮어쓰기/삭제: 오래된 ACK가 새 저장을 완료 표시하거나 삭제된 슬롯을 되살리지 않아야 한다.
6. 두 기기 같은 슬롯 수정: stale baseVersion은 409. 로컬 내용은 남고 새 슬롯 복사가 가능해야 한다. 삭제 후 stale PUT은 410.
7. 빈 저장소 복구: 이어하기 한 건 + 요약 페이지 호출만 발생. 본문은 슬롯 선택 시 한 건만 GET.
8. 요약 페이지 도중 끊기/재시작: cursor 재개, 기존 로컬 수정/삭제 보존. 복구 중 새 게임은 active를 빼앗기지 않아야 한다.
9. 원격 본문 요청 실패: 진행 중 게임이 멈추지 않아야 한다. 다운로드 중 새 게임 선택 시 늦은 로드 취소.
10. 오래된 회차 업로드가 새 회차 뒤에 완료돼도 서버 이어하기 포인터가 과거로 바뀌지 않아야 한다.
11. 신규 슬롯 반복 CONFLICT/413: 무한 전송·fork 없이 보존. 원인 해결 후 명시적 재시도/새 회차 생성.

저장 오류 안내 UI, 콘텐츠 버전 변환, 손상 원본 복구, 다중 프로세스 동시 파일 수정, 서버 전체 이력의 보관/삭제 정책은 별도 작업이다.
