# M8 검증 절차 — B. Unity 클라 (F6 · 복구 · 409)

> spring-prepare `docs/M8-check.md` §A(서버, 통과 09-02)의 짝. 코드: `save-plan.md` F6 B-1~B-6 (2026-09-02).
> 서버는 `bootRun`(`game` DB, V6). DB 질의는 **이번 계정의 userId**로 거른다(seed·지난 검증 행이 있다).
> 계정: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\account.json`. 저장 폴더: 같은 자리의 `saves\`.
> 결과는 §7 표에.

**함정**
- 회차 POST(생성)는 큐 파일에 서버 id가 없을 때만 나간다. "같은 guid로 다시 POST → 200"을 보려면 `{id}.queue.json`을 지운다.
- 옛 회차 큐 순회는 **앱 시작 시**만 돈다. 갈라지기 뒤 옛 큐의 확인은 재시작 후.
- 복구는 **로컬에 회차 파일이 하나도 없을 때**만. 하나라도 있으면 서버는 읽지 않는다(로컬이 진실).
- 즐겨찾기 PUT은 찍는 즉시. 서버가 꺼져 있으면 `SyncedAtUtc`가 비어 다음 시작에 다시 간다.

---

## 1. F6-1 스모크 — 멱등 생성

| # | 조작 | 기대 |
|---|---|---|
| 1.1 | 4번(재개) 또는 5번(새 게임) → 첫 장면 끝 | 콘솔 `[동기화] 회차 생성(201) — playthroughId N, client <guid>`. guid = `saves\playthroughs\<guid>.json` |
| 1.2 | 5번 두 번, 각각 첫 장면 끝 | 둘째·셋째 회차도 201, N이 는다. `SELECT id, client_id FROM game.playthroughs WHERE user_id = ?` → 회차 수만큼 |
| 1.3 | 앱 종료 → 활성 회차의 `<guid>.queue.json` 삭제 → 시작 → 4번 → 장면 끝 | `회차 확인(200) — playthroughId N` — **같은 N**. DB 행 수 그대로 |
| 1.4 | `GET /users/{uid}/playthroughs` | 각 줄의 `clientPlaythroughId`가 로컬 guid. 슬롯 1의 `inheritedPlaySeconds`·`ownPlaySeconds`·`chapterCompleted`가 로컬 파일과 같다 |

## 2. 갈라지기 왕복 (B-1 + B-4)

| # | 조작 | 기대 |
|---|---|---|
| 2.1 | 장면 두세 개 진행(동기화 완료 로그 확인) | |
| 2.2 | 백로그에서 **이전 장면** 항목 클릭 | `[저장] 갈라지기 — <A> 장면 k … → 새 회차 <B>`. 그 앞에 `[동기화] 완료`(flush) 또는 `갈라지기 전 동기화 못 함` 경고 |
| 2.3 | 새 회차 첫 장면 끝 | `회차 생성(201) — … client <B>, 갈래 ← <A> 장면 k`. 목록 GET에서 B의 `forkedFrom = { playthroughId: A의 서버 id, clientPlaythroughId: A, sceneIndex: k }` |
| 2.4 | B의 `choice_history` | seq 1부터. A의 것을 복사하지 않았다 |
| 2.5 | **오프라인 갈라지기**: 서버 끄고 A에서 장면 하나 더 → 이전 장면 클릭(갈라지기) → 서버 켜고 앱 재시작 | 시작 로그 `[동기화] 옛 회차 <A> — 미전송 n건을 보낸다` → `완료`. A의 마지막 장면 선택이 서버에 있다. 그 뒤 활성(B) 동기화 |

## 3. 즐겨찾기 왕복 (B-3)

| # | 조작 | 기대 |
|---|---|---|
| 3.1 | 6번(즐겨찾기) | `[저장] 즐겨찾기 …` 다음에 `[즐겨찾기] 서버 등록(201)`. `bookmarks.json`의 그 항목에 `syncedAtUtc` |
| 3.2 | `GET /users/{uid}/bookmarks` | 1건, `playthroughClientId` = 활성 guid, `sceneIndex` = 로컬과 같음, `createdAt`이 UTC |
| 3.3 | 서버 끄고 6번 → `syncedAtUtc` null → 서버 켜고 재시작 | 시작 시 `서버 등록(201)`. 목록 2건 |
| 3.4 | (F5 UI 전이라 코드로) `DeleteBookmark(id)` 또는 `bookmarks.json`에서 항목을 빼고 `pendingDeletes`에 id 추가 후 재시작 | `[즐겨찾기] 서버 삭제(204)`. 목록에서 사라짐. `pendingDeletes` 빔 |
| 3.5 | 7번(마지막 즐겨찾기로 갈라지기) → 첫 장면 끝 | 새 회차 201 + `갈래 ← <출처 guid> 장면 k` |

## 4. 새 기기 복구 (B-5)

| # | 조작 | 기대 |
|---|---|---|
| 4.1 | 앱 종료. `saves\` 폴더를 **통째로 다른 곳에 복사**해 두고(§5용) 삭제. `account.json`은 그대로 | |
| 4.2 | 시작 | `[복구] 회차 <guid> ← 서버 N (revision r, 선택 n건, qwer/EPxx, 기록 k개)` 회차마다, `[복구] 서버에서 회차 x/x개, 즐겨찾기 y개 재구성 — 활성 <guid>`. 활성 = 서버 `lastSavedAt` 최신 |
| 4.3 | `saves\` | `playthroughs\<guid>.json`·`<guid>.queue.json`(playthroughId·baseRevision·nextSeq·syncedSceneCount, 미전송 0) 회차마다, `active.json`, `bookmarks.json`(`syncedAtUtc` 채워짐) |
| 4.4 | 4번 | 활성 회차가 장면 루트에서 이어진다. 장면 끝 → `[동기화] 완료 — revision r+1` (409 아님, 회차 생성 로그 없음) |
| 4.5 | 백로그 | 이전 장면 항목이 있다(복구된 `Backlog`) |

## 5. 두 기기 409 (B-6, D-023)

| # | 조작 | 기대 |
|---|---|---|
| 5.1 | §4.1에서 복사해 둔 `saves\`를 **기기 B**로 본다. 지금 폴더가 **기기 A** | 둘 다 같은 회차 guid·서버 id·revision |
| 5.2 | A: 장면 하나 진행 → `완료 — revision r+1` | |
| 5.3 | 앱 종료 → `saves\`를 B 사본으로 바꿔치기 → 시작 → 4번 → 다른 선택으로 장면 하나 진행 | `[저장] 충돌(409) — 다른 기기가 회차 <A> 를 먼저 저장했다. 이 기기의 진행은 새 회차 <B'> 로 갈라 이어 간다 (출처 장면 k, 미전송 선택 n건 → seq 1부터 …)` 이어서 `회차 생성(201) — … 갈래 ← <A> 장면 k` → `완료 — revision 1` |
| 5.4 | 로컬 | `active.json` = B'. `playthroughs\<A>.json`은 그대로, `<A>.queue.json`은 미전송 0(서버 id·revision 유지). `<B'>.queue.json`은 미전송 0, `syncedSceneCount` = 이력 수 |
| 5.5 | 서버 | 회차 하나 늘었다. B'의 `forkedFrom.sceneIndex` = k, `choice_history`는 seq 1..n = 진 기기의 그 장면 선택. A의 revision은 r+1 그대로(덮이지 않았다) |
| 5.6 | 계속 진행 | 재생이 끊기지 않았다(멈춤·재시작 없음). 다음 fold도 B'에 붙는다 |
| 5.7 | 재시작 | `옛 회차 <A>` 순회 로그 없음(미전송 0). 409 다시 안 남 |
| 5.8 | 서버 목록 | B'의 `inheritedPlaySeconds` = A의 장면 k 진입 시각까지, `ownPlaySeconds` = 나머지. 합 = `playSeconds` |
| 5.9 | (선택) 옛 회차 큐 409: A 사본 폴더에서 활성을 다른 회차로 바꾸고 A 큐에 미전송을 남긴 채 재시작 | `옛 회차 <A> 충돌(409) — … 다시 보내지 않는다`. `<A>.queue.json`에 `conflictedAtUtc`. 다음 재시작엔 로그 없음 |

## 5-1. 시작 경쟁 (서버 검토 §2)

| # | 조작 | 기대 |
|---|---|---|
| 5-1.1 | §4.1 상태(로컬 비움)에서 시작 직후 **곧바로** 4번 | 복구 로그가 먼저 끝난 뒤 재개된다(활성이 복구된 회차). 새 게임으로 가지 않는다 |
| 5-1.2 | `account.json`만 지우고 즐겨찾기 미전송·회차 미전송을 함께 남긴 채 시작 | `[계정] 게스트 계정 생성` **한 번**. 회차와 즐겨찾기가 같은 userId 밑에 |

## 6. 바뀌지 않은 것

- 서버 테스트 116건 그대로 — 클라 작업이 서버 코드를 건드리지 않았다.
- `serverBaseUrl`이 비면 위 전부 무관(로컬만). 새 게임·갈라지기·즐겨찾기·롤백은 서버 없이 그대로.

## 7. 결과

| 절 | 결과 | 비고 |
|---|---|---|
| 1 | | |
| 2 | | |
| 3 | | |
| 4 | | |
| 5 | | |
| 6 | | |
