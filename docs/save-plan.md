# 세이브 확장 — 작업 계획 (정본, v2 갈래 모델)

2026-09-02. 장면 경계 재정렬(`scene-boundary-plan.md`) 위에 **이력 · 즐겨찾기 · 갈래 로드**를 올린다.
v1(되감기 모델)은 소유자 발상으로 폐기하고 다시 썼다 — §1.

---

## 0. 이 문서를 읽는 법

- 정본이다. 관문(F0~F6) 순서대로, 관문마다 소유자 Unity 확인 → 커밋.
- §2가 어휘다. 코드·UI·서버가 같은 이름을 쓴다.
- §5는 서버 저장소에 넘길 계약. §7은 소유자가 정한 것(2026-09-02).
- 서버(F6)는 로컬 관문을 끝낸 뒤 spring-prepare 로컬을 보며 별도 논의한다.

## 1. 왜 v2인가 — "확정된 것은 되돌리지 않는다. 대신 갈라진다."

v1은 로드를 "회차의 타임라인을 과거로 되돌리기"로 봤다. 그래서 이미 서버에 올라간 선택 seq를
`superseded`로 표시하는 되감기 표식이 필요했고, 장면 경계 설계의 한 문장("장면이 끝나면 확정된다")과
정면으로 부딪혔다.

소유자 발상: **세이브는 저장이 아니라 즐겨찾기다.** 이미 진행된 장면·에피소드의 이력 위에 원하는
지점을 표시해 두는 것. **로드는 그 지점의 데이터를 물려받아 새 회차를 시작하는 것.** 회차의 타임라인은
append-only로 남고, 되돌리는 대신 갈라진다 — git의 rewind가 아니라 branch.

이 한 줄이 세 요구를 한 기전으로 접는다:

| 요구 | v1 | v2 |
|---|---|---|
| 이전 장면 루트로 | 체크포인트 k로 되감기 + 되감기 표식 | 이력의 장면 k에서 **갈라져 새 회차** |
| 수동 세이브 | 별도 슬롯 파일 | 이력 위의 **즐겨찾기** |
| 로드 | 타임라인 되감기 | 즐겨찾기(또는 이력 장면)를 물려받아 새 회차 |
| 서버 | 되감기 표식 R3 | `forkedFrom` 하나. 회차마다 선택 로그는 처음부터 선형 |

그리고 현재 장면 안의 백점프는 그대로 롤백이다 — 미확정이니까. **현재 장면 = 되감기, 이전 장면 =
갈라지기.** 플레이어에겐 둘 다 "그 대사로 간다"로 보이지만 데이터는 한 번도 거짓말을 하지 않는다.

## 2. 어휘

| 이름 | 뜻 | 코드 |
|---|---|---|
| **회차** (playthrough) | 한 줄의 타임라인. 새 게임으로 시작하거나, 갈라져서 시작한다 | 로컬 파일 하나 + 자기 큐. 서버 playthrough |
| **갈래** (branch) | `forkedFrom`이 있는 회차. 물려받은 이력 + 자기 이력 | 회차의 부분집합 |
| **이력** (history) | 회차가 지나온 장면 기록의 목록 | `Scenes[]` |
| **장면 기록** (scene record) | 장면 하나의 진입 스냅샷 + 그 장면 안에서 지나온 경로 | `SceneRecord` |
| **즐겨찾기** (bookmark) | 이력 위의 한 점 — 장면 + 장면 안 경로 + 표적 라인. 스스로 완결된 사본 | `bookmarks.json` |
| **활성 회차** | 지금 플레이 중인 회차 하나 | |

## 3. 데이터 모델

### 3.1 회차 파일 — `playthroughs/{id}.json` (지금의 `slot1.json`이 이것이 된다)

```
PlaythroughId        로컬 id(guid). 서버 id는 큐 파일이 든다(지금과 같다)
ForkedFrom           { PlaythroughId, SceneIndex, Target } 또는 null(새 게임)
ChapterId · CurrentEpisodeId · Stats · Variables · ChapterCompleted   ← 지금 최상위 필드 그대로 = 마지막 장면 기록의 사본
Scenes[]             장면 기록 이력(현재 챕터 안). 갈라진 회차는 물려받은 것으로 시작한다
Backlog[]            현재 장면 이전의 백로그 항목(상한 300). 현재 장면의 것은 싣지 않는다
InheritedPlaySeconds 갈라진 지점까지 물려받은 이야기상의 시간
OwnPlaySeconds       이 회차에서 실제로 새로 플레이한 시간
SavedAtUtc
```

**장면 기록 `SceneRecord`** — 장면이 끝날 때(fold) 하나씩 붙는다:

```
Checkpoint     { ChapterId, EpisodeId(루트), Stats, Variables, BacklogSerialStart, LastChoiceSeq, PlaySecondsAtEntry }
Path[]         장면 안에서 확정된 진행 선택 { FromEpisodeId, OptionIndex }
YarnChoices[]  장면 안 Yarn 인라인 선택 기록(VNChoiceRecord: 순번·lineId·index) — 처음부터 싣는다(§7)
BacklogSerialEnd  이 장면의 마지막 라인 순번 + 1
```

이 넷이 있으면 **과거 어느 장면의 어느 라인이든 좌표가 선다** — 백로그 항목(노드·lineId·순번)에서
장면을 찾고, 그 장면의 경로와 Yarn 선택으로 루트부터 달려 그 라인까지 갈 수 있다. 회고적 즐겨찾기는
UI를 나중에 열더라도 데이터는 지금부터 남긴다.

### 3.2 즐겨찾기 — `bookmarks.json`

```
[{ Id, Label, Preview(라인 텍스트), CreatedAtUtc,
   From: { PlaythroughId, SceneIndex }                     ← 출처(이력 화면 표시용)
   Scene: SceneRecord 사본(진행 중 장면이면 그 시점까지의 초안)  ← 스스로 완결
   Target: { NodeName, LineId, Occurrence }
   Backlog[]: 그 장면 이전 항목들(사본)
   InheritedPlaySeconds, ChapterId, ChapterVersion }]
```

즐겨찾기는 **사본**이다. 출처 회차 파일이 없어도 로드된다. 현재 장면(아직 fold 전)에서 찍을 때는
지금까지의 경로·Yarn 선택·표적으로 초안을 만든다 — 그 뒤 롤백으로 장면의 최종 기록이 달라져도
즐겨찾기는 찍은 순간을 가리킨다. 수는 제한하지 않는다(파일 하나에 색인처럼 목록만, 100개 이상 전제).

### 3.3 보관 정책

- 회차 파일은 **전부 보관**한다. UI는 활성 회차와 즐겨찾기가 걸린 회차만 펼치고 나머지는 접는다(§7-1).
- 큐 파일은 회차마다 하나(`playthroughs/{id}.queue.json`). 갈라진 회차의 큐는 seq 1부터.
- 옛 회차의 나머지 이력(갈라진 지점 뒤)은 새 회차에서 보이지 않는다. 이력 화면이 갈래를 보여주는 것은 후속.

### 3.4 흐름

- **플레이 중 저장** = 즐겨찾기 생성(현재 라인). 라인 표시 중이든 옵션 박스가 떠 있든 허용(§7).
  표적은 `RollbackHistory.Points[^1]`.
- **로드(즐겨찾기)** = 갈라지기: 새 회차 파일 생성(`ForkedFrom`, 물려받은 `Scenes[0..k)`·백로그·
  `InheritedPlaySeconds`), 즐겨찾기의 장면 진입 스냅샷으로 장면 진입, `LoadPlan`(경로·Yarn 선택·표적)으로
  Load 시크. 첫 동기화에서 서버 회차를 `forkedFrom`과 함께 만든다.
- **이력 장면 루트로** = 경로 없는 갈라지기(즐겨찾기 없이 이력 항목에서 바로).
- **현재 장면 안 백점프** = 롤백(변화 없음).
- 활성 회차의 자동 저장(장면 끝 fold)은 지금과 같다 — 장면 기록 하나가 이력에 붙는 것.

## 4. 재검토 — 복원 방식은 B(루트부터 달리기)

v1 §2의 결론 그대로다. 즐겨찾기가 "장면 진입 스냅샷 + 경로 + 표적"이라는 데이터 정의 자체가
재실행을 전제로 한다. `StageState` 복원(A)은 즉시 복원·썸네일이 필요해지는 날 되살리기 반쪽을 만들 때
다시 본다 — 데이터가 겹쳐 길이 막히지 않는다. 표적을 못 찾으면 그 장면 루트에서 시작하고 알린다.

## 5. 서버 측 요구 (spring-prepare에 넘김 — F6, 별도 논의)

| # | 요구 | 근거 |
|---|---|---|
| R1 | `POST /users/{uid}/playthroughs`에 선택 필드 `forkedFrom { playthroughId, sceneIndex, seq }` | 갈래의 출처. 서버는 기록만 |
| R2 | 갈라진 회차의 `choice_history`는 seq 1부터, 부모 것을 복사하지 않는다 | 회차마다 선형. 집계는 `forkedFrom` 유무로 나눠 센다 |
| R3 | 즐겨찾기 동기화(선택): `PUT /playthroughs/{pid}/bookmarks/{id}` 스냅샷(사본) 통째. 서버는 열지 않는다 | 다른 기기 복구 |
| R4 | 회차 목록 GET에 `forkedFrom`·활성 여부·`inherited/own play seconds` | 이력 화면·통계 |
| R5 | 슬롯 개념 폐지 → 회차 + 즐겨찾기. `save_slots`는 회차 스냅샷 1개로 축소 | v1 R1/R2 대체 |
| — | 되감기 표식(v1 R3) **없음** | 되돌리지 않으니까 |

## 6. 관문

| # | 관문 | 한 줄 | 의존 |
|---|---|---|---|
| F0 | 문서 확정 | 이 문서. 어휘·모델·결정 | — |
| F1 | 이력 모델 | 회차 파일에 `Scenes[]`(경로·Yarn 선택 포함)·백로그·두 종류 시간. 재개가 백로그를 되살림. **동작 불변** | — |
| F2 | 갈래 로드 런타임 | Load 시크(`BeginLoadSeek`·`LoadPlan`·`RestoreChoices`·퇴행·실측) + 회차 생성·물려받기. 먼저 "이력 장면 루트로"(경로 없음)로 검증 | F1 |
| F3 | 즐겨찾기 | 현재 라인에서 캡처(옵션 박스 중 허용) → `bookmarks.json`. 즐겨찾기 로드 = 경로 있는 갈라지기 | F2 |
| F4 | 이력·백로그 통합 | 백로그의 이전 장면 항목 → "여기서 갈라져 다시 보기"(장면 루트). 회고적 라인 갈라지기는 데이터만 준비, UI 후속 | F2 |
| F5 | UI | 즐겨찾기 목록(라벨·미리보기·시각·출처), 이력 화면(장면 목록, 접힌 갈래), 로딩 화면(F2 실측 뒤) | F3·F4 |
| F6 | 서버 | R1~R5. 보류 — 별도 논의 | F2·F3, 서버 |

### F1. 이력 모델 (동작 불변)

- 진행 중이던 S1 코드가 그대로 첫 조각이다(`SaveCheckpoint`·`Checkpoints[]`·`Backlog[]`·`BacklogRecorder.Restore`·
  `SyncQueue.NextSeq`). 여기에 더한다:
  - `SaveCheckpoint` → `SceneRecord { Checkpoint, Path[], YarnChoices[], BacklogSerialEnd }`. fold에서 `SceneRunner`가
    경로(pending)와 `ChoiceHistory.CreateChoiceSnapshot()`을 보고에 싣는다.
  - `Checkpoint.PlaySecondsAtEntry`, 파일의 `InheritedPlaySeconds`/`OwnPlaySeconds`(기존 `PlaySeconds`는 둘의 합으로 유지 — 서버 DTO 호환).
  - 파일 이름은 아직 `slot1.json` 유지. `PlaythroughId`·`ForkedFrom(null)` 필드만 추가. 회차 폴더 구조는 F2.
- 완료 기준: 기존 흐름 동일. 파일에 장면 기록이 경로·Yarn 선택과 함께 쌓이고, 재개 뒤 백로그에 이전 장면 대사가 보인다.
  구세이브(이력 없음) 로드 정상. 두 시간 필드가 맞게 흐른다.

### F2. 갈래 로드 런타임

- 회차 파일·큐를 id로 관리(`playthroughs/{id}.json`, `{id}.queue.json`), 활성 회차 포인터(`active.json`).
  기존 `slot1.json`은 첫 실행에 회차 하나로 옮긴다(마이그레이션 1회, 로그).
- `SaveCoordinator.ForkAsync(from: SceneRecord 원천, target?)`: 새 회차 파일(물려받기) + 새 큐 → 활성 전환.
  `OwnPlaySeconds`는 0에서, `InheritedPlaySeconds`는 원천의 `PlaySecondsAtEntry` + 장면 안 경과(즐겨찾기가 든 값).
- `VNLinePresentationState.BeginLoadSeek`. `SceneRunner.RunAsync`에 `LoadPlan`(경로·Yarn 선택·표적):
  진입 뒤 `ChoiceHistory.RestoreChoices`, 경로를 `_picks`로 미리 실음(앵커는 자동 응답 시점 `LastHistoryIndex`),
  `BeginLoadSeek`. 자동 응답 규칙(시크 활성 시)이 경로를 따라간다. Load 시크는 백로그를 적는다(기존 규칙).
- 퇴행: 경로가 끝났는데 시크가 살아 있으면(표적 못 찾음) 시크를 끄고 그 자리에서 일반 재생 + 경고 + UI 알림 훅.
  `ChapterVersion`이 다르면 시크를 시도하지 않고 루트에서 시작.
- 재실행 시간 로그(장면 길이 × 커맨드 비용 실측 → 로딩 화면 판단).
- 먼저 **경로 없는 갈라지기**(이력 장면 루트)로 회차 생성·물려받기를 검증하고, 그 다음 경로 있는 것.
- 완료 기준: 복도에서 사무실 장면 기록으로 갈라지기 → 새 회차, 사무실 루트부터, 스탯·[3]·백로그가 사무실 진입값,
  옛 회차 파일 그대로. 테스트 하네스로 만든 `LoadPlan`(`[사무실 8]`)으로 로드 → 무대·[3]·백로그 일치, 자동 응답 로그,
  표적 뒤 사람이 조작. 표적을 없는 라인으로 바꾸면 퇴행 경로.

### F3. 즐겨찾기

- 캡처: `SceneRunner`가 진입 스냅샷(상태 + `YarnVariableCheckpoint`의 진입 변수 — 노출)과 pending 경로를,
  `VNRuntimeStateProvider`가 현재 라인·Yarn 선택 스냅샷을 안다 → `BookmarkCapture`(호스트 층) → `bookmarks.json`에
  추가. 라벨 기본값 = 라인 미리보기. 조건: 시크 아님·장면 안. 옵션 박스 중 허용.
- 로드: 즐겨찾기 → `ForkAsync` + `LoadPlan`. 새 게임/재개와 같은 대조(D-017).
- 완료 기준: `[사무실 8]`에서 즐겨찾기 → 다른 곳까지 진행 → 로드 → 새 회차, `[사무실 8]`에서 같은 무대·[3]·백로그.
  Via 안·Yarn 인라인 선택 뒤·옵션 박스 중 즐겨찾기도. 옛 회차와 즐겨찾기 파일 그대로.

### F4. 이력·백로그 통합

- 백로그의 이전 장면 항목: 흐림 대신 "이 장면 처음부터 다시 보기" 액션 → 경로 없는 갈라지기.
- 회고적 라인 갈라지기(이전 장면의 특정 라인): 데이터(`SceneRecord`)로 가능하다. UI는 후속 — 열 때 백로그
  항목에서 장면 기록을 찾아 `LoadPlan`을 만드는 함수만 F4에서 둔다.

### F5. UI

- 즐겨찾기 목록: 라벨·미리보기·시각·출처(회차/장면), 로드 확인, 삭제. 100개 이상 스크롤.
- 이력 화면: 활성 회차의 장면 목록. 접힌 갈래(§7-1). 로딩 화면(F2 실측 뒤).
- 프리팹·레이아웃은 소유자 작업. 바인딩(`VNScreenBindings.*`)은 여기서.

## 7. 결정 (소유자, 2026-09-02)

| # | 결정 | 결론 |
|---|---|---|
| 7-1 | 옛 갈래 보관 | **데이터는 전부 보관. UI에서만 접거나 숨김**(기본 접힘) |
| 7-2 | Yarn 인라인 선택을 이력에 | **처음부터 싣는다.** 회고적 즐겨찾기 UI는 후속이어도 데이터는 지금부터 |
| 7-3 | 이름 | 회차 · 갈래 · 이력 · 장면 기록 · 즐겨찾기 · 활성 회차 (§2) |
| 7-4 | 플레이 시간 | 갈라진 지점까지 물려받는다. **두 종류로 나눔** — `InheritedPlaySeconds`(이야기상 계승) / `OwnPlaySeconds`(이 회차에서 새로) |
| 7-5 | 옛 회차의 나머지 이력 | 새 회차에서 보이지 않는다 |
| 7-6 | 옵션 박스 중 즐겨찾기 | 허용 — 표적은 노드 마지막 라인, 로드 뒤 판정이 박스를 다시 띄운다 |
| 7-7 | 즐겨찾기 수 | 제한 없음(100개 이상 전제, 목록 파일) |
| 7-8 | 복원 방식 | B(루트부터 달리기). 로딩 화면은 F2 실측 뒤 |
| 7-9 | 서버 | 로컬 관문 뒤 별도 논의 |

## 8. 위험

- **결정론.** Yarn에 난수·시간·외부 입력 분기가 있으면 재실행이 갈라진다. 툴 린트 전까지는 저작 규칙.
- **재실행 비용.** 장면이 길수록 로드가 길다. F2에서 실측, 길면 로딩 화면 또는 장면 길이 가이드.
- **콘텐츠 버전.** 저장 뒤 챕터가 바뀌면 경로·표적이 어긋난다. `ChapterVersion` 대조 → 다르면 루트에서 시작하고 알린다.
- **회차 증식.** 로드마다 회차 파일이 는다. 텍스트라 작지만 목록 화면은 접힘이 기본이어야 한다(7-1).
- **백로그와 순번.** 현재 장면 항목을 싣지 않는 이유(재기록으로 롤백 포인트와 정렬)를 지키지 않으면 백점프가 어긋난다.
- **즐겨찾기 초안.** 현재 장면에서 찍은 즐겨찾기는 그 순간의 경로다. 이후 롤백으로 장면 기록이 달라져도 즐겨찾기는 사본이라 독립 — 의도된 것.

## 9. 진행 상태

- 2026-09-02: **F1 작성됨** (Unity 확인 남음). 회차 파일에 `PlaythroughId`·`ForkedFrom(null)`·`Scenes[]`(진입 스냅샷 + 경로 + Yarn 선택 + 순번 경계)·`Backlog[]`·`InheritedPlaySeconds`/`OwnPlaySeconds`(합은 `PlaySeconds`). `IProgressionReporter.ReportSceneEntered` 신설 — SceneRunner가 장면 진입 직후 진입 스냅샷을 보고하고, fold에서 경로·Yarn 선택을 붙여 장면 기록으로 접는다. 재개 시 백로그 복원. 파일 이름은 아직 `slot1.json`.
