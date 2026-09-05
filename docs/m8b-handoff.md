# 클라이언트 → 서버 답신 — M8-B 반영 결과 (초안, 검증 뒤 `spring-prepare/docs/handoff/unity-2026-09-XX.md`로)

> 원문: 서버 답신 `server-2026-09-02.md`, 작업 지시 `m8b-work-order-2026-09-02.md`. 코드 반영 2026-09-02.
> 검증: `docs/m8b-check.md` §1~§6 — **(결과 채울 것)**.

## 0. 한 장 요약

F6-1~F6-4·B-5·B-6 전부 붙였다. 서버 계약은 답신 §1 그대로 탔고, 서버에 새로 바라는 것은 **없다**.
답신과 달랐던 점 하나(§2), 클라가 스스로 정한 것 둘(§3), D-023의 실제 구현(§5).

## 1. 바뀐 파일

| 파일 | 무엇 |
|---|---|
| `Save/ServerDtos.cs` | `PlaythroughCreateRequestDto`·`ForkOriginDto`, `PlaythroughCreatedDto.ClientPlaythroughId`, PUT 세 필드, 목록·슬롯·선택 이력·즐겨찾기 DTO |
| `Save/ServerApi.cs` | 회차 POST에 본문, 목록·슬롯·선택 이력 GET, 즐겨찾기 PUT/GET/GET 단건/DELETE |
| `Save/ServerSyncSaveStore.cs` | `SyncOnceAsync(save, queue, slotNo)`로 분리, `SyncStaleQueuesAsync`, 409 → `ConflictDetected`, 413 로그 |
| `Save/ServerBookmarkSync.cs` (신설) | 즐겨찾기 PUT(upsert)/DELETE 직접, 시작 시 재시도 |
| `Save/ServerRestore.cs` (신설) | 새 기기 복구 — 회차·큐·즐겨찾기 재구성, 활성 = `lastSavedAt` 최신 |
| `Save/GuestSession.cs` | `CallAsync` — 토큰 + 401 한 번 재시도를 한 곳에 |
| `Save/SyncQueue.cs` · `SaveData.cs` | `SyncedSceneCount`, `Reset(choices, events)`(seq 재번호), `Restore`, `Discard`; `Bookmark.SyncedAtUtc`·`SyncError`, `BookmarkFile.PendingDeletes` |
| `Save/SaveCoordinator.cs` | 시작 순서(복구 → 옛 큐 → 즐겨찾기 → 활성), 갈라지기 전 flush, 즐겨찾기 트리거, `HandleConflict` |
| `Save/SaveJson.cs` | `Serializer`(응답 안 스냅샷 되읽기) |
| `Game/VNAppBootstrap.cs`, `UI/Bindings/VNScreenBindings.Backlog.cs`, `FeatureController/VNAdvanceInputPoller.cs` | 배선, `await` |

## 2. 답신과 달랐던 점

- **§1.1 `forkedFrom.playthroughId`** — 답신 권고대로 **보내지 않는다**. `clientPlaythroughId`·`sceneIndex`만.

## 3. 클라가 정한 것 (서버 계약 밖)

- **옛 회차 큐의 409는 갈라지지 않는다.** 활성이 아니라 이어 갈 진행이 없다. 큐 파일에 `conflictedAtUtc`를 적고 다음 시작부터 건너뛴다(같은 baseRevision은 다시 보내도 409). 드문 경우(옛 기기에 남은 옛 회차 큐)라 첫 버전은 이대로. 그 회차도 갈라 두는 것(활성만 안 바꾸는 `HandleConflict`)은 M9 후보.

## 3-1. 서버 검토(09-02, 검증 전) 반영

- 시작 동기화를 `SaveCoordinator.StartupSync`로 붙들고, 재개(4번)·새 게임(5번)이 먼저 기다린다 — 복구·409 갈라지기가 활성 파일을 쓰는 창과 경쟁하지 않는다.
- `GuestSession.EnsureTokenAsync` 단일 비행 — 동시에 몇이 부르든 가입·로그인은 한 번. `SyncPendingAsync`도 즐겨찾기를 기다린 뒤 활성으로(문서의 순서 그대로).
- 409 갈래도 시간을 나눈다 — `Inherited = Scenes[k].Checkpoint.PlaySecondsAtEntry`, `Own = PlaySeconds − Inherited`.
- **즐겨찾기 label·preview는 클라에서 자른다**(100·200). 원문은 스냅샷 안에 그대로.

## 4. 서버에 새로 바라는 것

없음.

## 5. D-023 — 실제 구현

409는 `ServerSyncSaveStore`가 큐를 손대지 않은 채 `ConflictDetected`로 넘기고, `SaveCoordinator.HandleConflict`가 받는다.
활성 회차 파일에 새 로컬 guid를 주고 `forkedFrom = { 옛 guid, sceneIndex = 큐의 SyncedSceneCount }`로 저장한다 —
`SyncedSceneCount`는 마지막 200 시점의 장면 기록 수라 "서버가 마지막으로 받아 준 장면"과 같다. 옛 큐의 미전송
선택·이벤트는 `Discard`로 빼고 새 큐에 seq 1..n으로 다시 매겨 넣는다(`baseRevision` 없음 → 0). 재생·이력·진입
스냅샷은 손대지 않는다 — 플레이어는 멈춤 없이 이어 간다. 그 자리에서 동기화를 다시 걸면 새 회차 POST(201, 갈래)
→ PUT(revision 1)이다. `force`는 서버에 남아 있지만 클라 UI에 없다. 사용자에게는 `ConflictForked` 이벤트와 경고
로그로 알리고, 목록은 `forkedFrom`으로 갈래를 표시한다(UI는 F5).

## 6. M8-check §B 결과표

(검증 뒤 `docs/m8b-check.md` §7을 옮겨 적는다.)
