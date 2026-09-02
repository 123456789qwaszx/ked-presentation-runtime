# 장면(Scene) 런타임 — 설명서

2026-09-02. `docs/scene-boundary-plan.md`(G0~G5)의 결과를 "지금 시스템이 어떻게 도는가"로
다시 쓴 것. 계획서는 과정이고, 이 문서는 현재 상태다. 아직 하지 않은 일은 `docs/scene-future-plan.md`.

---

## 1. 한 문장

**장면 안에서는 모든 게 물릴 수 있고, 장면이 끝나면 확정된다.**

연출 연속·롤백·커밋·저장의 네 경계가 전부 장면 하나에 있다. 에피소드는 저작·실행 블록일 뿐
연출적으로 투명하다 — 에피소드 경계에서는 아무것도 초기화되지 않는다.

## 2. 수명 계층

| 수명 | 소유 | 경계에서 일어나는 일 |
|---|---|---|
| 회차 | 백로그, 플레이 시간 | 새 게임에서만 백로그 클리어 |
| Chapter | [2] 확정 상태, [3] 변수, 그래프·스탯 정의 | `BeginChapter` — [3] declare 초기값, [2] 진입 상태 |
| **Scene** | 무대, PresentationScope, 롤백 포인트, 변수 체크포인트, 선택 기록, **미확정 선택(pending)** | 진입: 무대 기준선·체크포인트 / 종료: fold(확정) + 저장 1회 |
| Episode | 진행 커서, 시청 기록 | 없음 |

세 상태 계층은 그대로다: **[1] 영구**(아직 없음), **[2] 진행 스탯**(`ProgressionState`, 간선
커밋만이 바꾼다, 진행 코어만 안다), **[3] 연출 변수**(Yarn 저장소가 유일한 집, 챕터 로컬).
G0에서 [2]→Yarn 투영을 철거했으므로 **대사는 스탯을 읽지 못한다** — `<<if>>`는 [3] 전용,
스탯 분기는 그래프 간선 조건으로 올린다.

## 3. 데이터 — 장면은 어디서 오나

런타임은 장면을 정의하지 않고 받는다. 챕터 JSON의 `Nodes[].SceneId`(문자열, 생략 가능)가
소속이고, 비면 에피소드마다 고유 장면이 발급된다(퇴화 상태 = 장면 개념 전과 동일 동작).

불변식은 하나 — **장면에 밖에서 들어오는 자리는 하나다.** 그 자리가 장면 루트이고, 롤백이
되돌아갈 곳과 이어하기가 재개할 곳이다. 이 규칙이 연결성까지 보장한다. 장면을 나갔다
되돌아오는 것(허브 구조)은 허용된다 — 재진입은 루트에서 다시 여는 새 장면 방문일 뿐이다.
위반은 로드에서 죽고 진단이 간선 자리를 짚는다(`ChapterInvariants.VerifySceneEntries`).

코어 질의: `ChapterProgression.IsSameScene(from, to)`(경계 판단은 이 답 하나만 본다),
`SceneIdOf`, `IsSceneRoot`.

## 4. 실행 — 누가 무엇을 쥐나

```
ProgressionLauncher   세이브 대조 → 챕터·진입 상태·[3] 덤프를 드라이버에
ProgressionDriver     챕터 루프: 장면 다음에 장면. 챕터 변수(BeginChapter + 덤프 덮기). [2] 확정 상태 소유
SceneRunner           장면 루프: 노드 재생 → 시청 기록 → 판정 → 선택(자동 간선이면 생략) → Via → 다음 에피소드 … → fold
EpisodePlayer         장면의 연출 수명 + 노드 실행
                      EnterSceneAsync   (Stop·체크포인트·기록 리셋·무대 기준선)
                      PlayNodeAsync     (노드 하나. Completed | ReplayRequested)
                      PrepareReplayAsync(Stop 대기·변수 되감기·무대 기준선)
```

장면 진입에서만 `PresentationStage.Clear`·`PresentationScopeSession.Start`·체크포인트 Capture·
`ChoiceHistory` 클리어가 일어난다. 같은 장면의 다음 에피소드는 `PlayNodeAsync`만 — 노드 사이에서
`StopDialogueAsync`를 부르면 롤백 포인트가 지워져 시크 좌표계가 중간에 리셋되므로 금지.
Via 연출 노드도 장면 안 노드로 이어서 튼다. 드라이버의 세 규칙(판정은 대사 뒤 한 번 / 연출은
커밋 앞 / 반영과 이동이 한 연산)은 장면 단위로 지켜진다.

디버그 경로(`RunYarn`, `RunEpisodeChain`)는 `StartGameAsync`/`ContinueEpisodeAsync` 래퍼로
진행 층 없이 노드 하나를 장면으로 튼다.

## 5. 롤백 — 장면 루트부터 재실행

이 프로젝트의 롤백은 복원이 아니라 **다시 재생**이다.

1. `VNFeatureController.RequestRollbackOneStep` — 표적 = 뒤에서 두 번째 롤백 포인트. Yarn 선택
   기록(`ChoiceHistory`)과 백로그 꼬리를 표적 기준으로 정리, 롤백 포인트 클리어, 시크 시작,
   표적을 `RollbackHistory.MarkRollbackTarget`에 남긴다.
2. `EpisodePlayer.RequestReplayAsync` — 노드 안이면 Stop, 노드 밖(진행 선택지 대기)이면
   `ReplayRequestedWhileIdle`로 박스를 접는다. 둘 다 같은 정리(라인 중단·샷 응답·스코프 종료).
3. `SceneRunner.BeginReplayAsync` — `PrepareReplayAsync`(체크포인트 Restore·무대 Clear) 뒤 표적
   뒤의 진행 선택·시청 기록을 물리고(앵커 > 표적), 루트 에피소드부터 다시 돈다.
4. 재실행 중 라인은 시크 패스스루(화면 생략), **커맨드는 그대로 실행**(`CommandRunScope.
   IsSeekPassThrough`) — 무대가 원래 순서대로 재구성된다. 진행 선택지는 **시크가 살아 있는 동안만**
   기록대로 자동 응답하고, 표적에 닿아 시크가 꺼지면 사람이 다시 고른다(Yarn 옵션 흐름과 같은
   규칙). 그래서 "선택 직전 라인으로 돌아갔는데 선택이 자동으로 지나가는" 일이 없다.

시크 좌표는 (노드명, 라인ID, 장면 시작 이후 등장 순번)이라 노드를 넘어 이어진다.
`RollbackHistory`와 `VNSeekState`의 카운터가 같은 순간(장면 진입·롤백 요청)에 리셋되므로 정렬된다.

**백점프(백로그 → 라인).** 백로그 항목을 누르면 그 라인으로 되돌아간다 — 기전은 한 걸음 롤백과 같고
표적만 다르다(`VNFeatureController.RequestBacklogJump` → `RequestRollbackTo`). 항목의 장면 소속은
"장면 진입 시점의 백로그 순번"(`BacklogRecorder.MarkSceneStart`) 하나로 판정하고, `순번 - 장면 시작 =
롤백 포인트 historyIndex`로 표적을 찾는다. 두 좌표계는 같은 순간(장면 진입·롤백 요청)에 0에서 출발하며,
롤백 truncate가 순번도 되감아 정렬을 지킨다. **현재 장면 항목만** 되돌아갈 수 있다 — 이전 장면·지금
라인은 흐리게 보이고 눌리지 않는다. 이전 장면은 확정·저장된 것이라 되감기가 아니라 "다시 여는" 일이다
(후속 — `scene-future-plan.md` §1).

## 6. 커밋 유예 — 장면 끝이 커밋이다

장면 안의 진행 선택은 전부 미확정(pending)이다. 판정은 `진입 상태.Fold(pending)`으로 만든 작업
상태로 하고(`ProgressionState.Fold` = `Commit`의 합성), 장면을 나가는 순간 한 번에 접어 확정한다.
롤백은 pending을 자르는 것뿐 — 상태를 되돌리는 코드가 아니라 입력을 줄이는 코드다. 리플레이
자동 응답은 커서만 옮기므로 이중 커밋이 없다. 장면 중간 멈춤은 pending을 버린다.

퇴화 상태(장면 = 에피소드 1개)에서는 선택 직후 장면이 끝나 fold가 즉시 일어난다 — 장면 개념
전과 커밋 타이밍이 같다.

## 7. 저장과 이어하기

세이브의 뜻은 둘 중 하나다: **장면 진입 스냅샷**(`CurrentEpisodeId` = 장면 루트, `Stats`·
`Variables` = 그 시점) 또는 **챕터 완료**(`ChapterCompleted`). 장면 중간을 가리키는 세이브는
만들지 않는다.

- 장면 끝 fold → `IProgressionReporter.ReportSceneCommitted` 한 번: 선택 목록·시청 목록·확정
  상태·[3] 통덤프·완료 여부. `SaveCoordinator`가 로컬 저장 1회 → 큐 적재(Seq 순서) → 동기화.
- [3] 덤프(`YarnVariableSnapshot`, floats·strings·bools)는 fold 안에서 굽는다(다음 장면 진입 전).
  이어하기는 `BeginChapter`(declare 초기값) **위에 덮는다** — 덤프에 없는 신규 declare는 초기값
  으로 남는다([2] `Restore`가 저장 후 추가된 스탯을 다루는 것과 같은 문장).
- 런처의 세 갈래: 챕터 완료 → 새 게임 / 위치가 루트가 아님(구형식) → 경고 후 새 게임 /
  아니면 복원. "이어 가는 척하지 않는다"(D-017).
- 서버 스냅샷은 `LocalSaveFile` 통째라 서버 변경 없음.

무대 복원이 상황별로 어떻게 닫혔는지는 `scene-boundary-plan.md` §7 — 롤백은 재실행, 이어하기는
루트 재개, 장면 경계 무대 승계만 후속.

## 8. 저작 규칙 (툴 반입용)

- 챕터 JSON `Nodes[].SceneId`로 장면을 묶는다. 비우면 에피소드 하나가 장면 하나.
- 장면에 들어오는 자리는 하나 — 작가가 장면 중간으로 들어오는 간선을 그리면 그 자리에서
  "여기서 장면을 나눌까요?"가 자연스럽다.
- `<<if>>`는 연출([3])용. 스탯 분기는 간선 조건. 대사에서 `$스탯`을 읽는 것은 금지.
- 장면 안에서 에피소드만 나누고 물을 것이 없으면 간선에 `Auto: true`. 묻지 않고 지나간다. 규칙 넷:
  그 에피소드의 유일한 간선 · 조건 없음 · 스탯 변화 없음 · 같은 장면. 스탯을 바꾸려면 보이는 선택지로.
  문구만 비우는 것으로는 자동이 되지 않는다(옛 자동 진행이 툴의 실수를 조용히 지나가게 했다).
- 장면 첫 노드(루트)가 캐스팅·배경을 세운다. 뒤 노드는 세우지 않는다 — 장면 중간에서 시작하는
  경로가 없으므로 안전하다.
- 장면 중간에 종료하면 이어하기는 장면 처음부터. 롤백이 "장면은 통째로 다시"인 것과 같은 결.

## 9. 검증 도구

- `dotnet build Assembly-CSharp.csproj` / `Ked.Progression.csproj` — 3초 안에 타입 오류.
- `Ked.Progression`은 엔진 참조가 없어 Unity 밖 콘솔 프로젝트로 컴파일·실행된다(장면 불변식·
  Fold 검증에 사용). EditMode 테스트: `SceneBoundaryTests`, `ProgressionStateFoldTests`.
- 테스트 콘텐츠: `qwer.progression.json`(퇴화 기준선) / `qwer_scene.progression.json` +
  `test/qwer_scene.yarn`(장면 2개, Via 2개, 대사에 `[사무실 7]` 식 번호).

