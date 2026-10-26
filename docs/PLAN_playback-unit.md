# PLAN — 재생 단위와 연출 기억의 경계

2026-10-26. `PLAN_framework-scope.md` 3단계(재생 계약)와 저장 분할(`d3d7ecfb`) 뒤에 이어지는 계획.
4단계(연출 데모 대본 등)보다 먼저 한다 — 이 레포가 무엇을 소유하는지가 여기서 정해진다.

## 정한 것

| | 결정 |
|---|---|
| E1 | 롤백·백로그·줄 시크·선택 기록은 **이 레포의 일부**이고 끝까지 유지한다 |
| E2 | 되감기 = **루트 세우기 + 루트부터 시크**. 시크하는 반쪽은 연출 것이다 |
| E3 | 이 레포는 **재생 단위의 루트를 하나만 둔다.** 이전 단위의 루트 보관(과거 장면에서 갈라지기)은 진행(ked-progression-runtime)의 책임이다 |
| E4 | **재생 단위의 수명(열기·닫기·루트부터 다시 달리기)은 연출 쪽 타입 하나가 소유한다.** 호스트는 "이 단위에서 무엇을 재생할지"와 "되감을 때 자기 기록을 어디까지 자를지"만 말한다 |
| E5 | 줄 진행·선택 기록·시크·롤백 기록·백로그·재생 호스트 계약·연출 기억 스냅샷은 **이름 있는 어셈블리**(`Ked.Presentation.Playback`)가 소유한다. 지금의 "글루" 분류를 벗는다 |

## 이 레포의 한 문장 (시안)

> Yarn 대본을 줄 단위로 연출하고, 지금 재생 중인 단위 하나 안에서 그 연출을 기억하고 되감는 런타임.

책임 다섯: ① 연출 언어 실행 ② 줄의 수명 ③ 재생 단위 하나의 기억과 되감기 ④ 정지 프레임 계산 ⑤ 호스트 계약.
하지 않는 것: 다음에 무엇을 재생할지(진행), 확정·저장 정책·과거 루트 보관(진행·저장), 저작(VnTool).

---

## 단계

### P0. 원본 레포 — E3을 지키는 장치 (ked-progression-runtime)

"과거 장면 루트 보관"은 원본 레포 안에 소비자가 없으면 무의미한 일반화로 읽혀 지워질 수 있다. 세 겹으로 지킨다.

| | 작업 | 완료 기준 |
|---|---|---|
| P0-1 | `tests/ProgressionSave`에 결정 시나리오 테스트 하나: **호스트가 자기 순번으로 과거 장면을 찾아 갈라지고, 재생 계획에 호스트 상태가 실린다.** 이름에 결정(E3)을 적는다 | 하네스 통과. 기능을 지우면 이 이름으로 깨진다 |
| P0-2 | README §11 저장 절에 결정 기록 한 단락: 무엇이 걸려 있었나, 연출 레포가 루트를 하나만 두는 이유 | 문서 |
| P0-3 | 원본 샘플에 소비자: 저장·이어하기, **대사 기록 줄을 누르면 그 장면에서 갈라짐**. 대사 기록의 줄 번호가 호스트 순번이다. `SampleSceneRunner`가 복원 시작(경로 소비 + 표적 줄까지 빨리 감기)을 지원한다 | Unity 없이 도는 부분(runner·호스트 상태)은 dotnet으로 검증. 화면은 사용자 Unity 확인 |

### P1. 재생 단위 타입 — E4 (이 레포)

| | 작업 | 완료 기준 |
|---|---|---|
| P1-1 | 재생 단위 타입(가칭 `PlaybackUnit`): 열기(정지 → `BeginScene` → 백로그 장면 경계), 리플레이(정지 완료 대기 → `PrepareReplay` → 롤백 표적 소비 → 호스트에 표적 전달), 정지(한 구간에 한 번) | 타입 하나가 순서를 소유 |
| P1-2 | `SceneRunner`·`NodePlaybackHost`가 그 타입을 쓴다. 두 호스트에서 같은 순서를 반복하던 줄이 사라진다 | `tests/ProgressionHost` 20건 통과(필요하면 대역 조정). 연출 단독 동작 동일 |

### P2. `Ked.Presentation.Playback` 어셈블리 — E5 (이 레포)

| | 작업 | 완료 기준 |
|---|---|---|
| P2-1 | 의존 조사 확정. 지금 조사로는 바깥(UI) 의존이 셋뿐이다: 대사창·선택지창 뷰 베이스가 `UIBase`를 상속, `PresentationAssembler`·`PresentationSceneRefs`가 UI를 조립 | 옮길 목록과 남길 목록 |
| P2-2 | 폴더 `Assets/Scripts/Ked.Presentation.Playback/` + asmdef. 옮길 것: DialoguePresenter(뷰 베이스 제외 또는 UI 계약으로 분리), FeatureController, YarnBridge, Playback 계약·재생 단위·`NodePlaybackHost`, `ScenePresentation`, `IEpisodeNodeRunner`, `SaveLineTarget` | Unity 컴파일(사용자 확인) |
| P2-3 | **연출 기억 스냅샷**을 이 어셈블리로: 지금의 `PresentationSaveState`(ProgressionHost)를 옮기고 진행 저장(`SaveJson`) 의존을 끊는다 | 스냅샷이 진행을 모른다 |
| P2-4 | `ASSEMBLY-BOUNDARY.md` 갱신 — "글루"에서 빠진 것과 이유 | 문서 |

**P2 결과.** 대사창·선택지창 뷰 베이스 셋(`PresentationDialogueBoxViewBase` · `PresentationOptionsBoxViewBase` · `VNDefaultOptionsPanel`)은
`UI/DialogueBox/`(예제)에 남고 Playback은 그 계약만 안다. `PresentationAssembler` · `PresentationSceneRefs`는 `Game/`(예제 조립)으로 갔다.
이름도 저장에서 떼었다 — `PresentationSaveState` → `Memory/PresentationMemory`(자기 JSON 설정, 형식은 그대로),
`SaveLineTarget` → `LineTarget`, `PresentationLoadPlan` → `PresentationResumePoint`. 옮긴 파일의 `.meta` GUID는 그대로다.
근거는 [ASSEMBLY-BOUNDARY.md](../Assets/Scripts/Ked.Presentation.Runtime/ASSEMBLY-BOUNDARY.md) §1·§3.

### P3. 얇은 어댑터와 연출 단독 저장 (이 레포)

| | 작업 | 완료 기준 |
|---|---|---|
| P3-1 | ProgressionHost를 "진행 런타임을 꽂는 어댑터"로: SceneRunner·Launcher·SaveBridge가 스냅샷 API만 쓴다 | `tests/SaveLifecycle`·`tests/ProgressionHost` 통과 |
| P3-2 | 연출 단독 예제에 저장·불러오기 하나: 같은 스냅샷 + 노드 이름 | 사용자 Unity 확인 |

**P3 결과.** 연출 기억의 입구 `Memory/PlaybackMemory`(꺼내기 · 되돌리기 · 과거 단위 읽기)를 두고, 문자열 형식·재개 지점·줄 좌표·
과거 단위 셈법·무대 계약은 `internal`로 닫았다. `ProgressionReplayState`는 없어졌다 — 재개 예약은 `PlaybackUnit`이 갖고,
단위를 열 때 `ScenePresentation`이 선택 복원과 줄 시크를 건다. 그래서 `SceneRunner`는 "시크 중인가"만 묻고,
런처·브리지·저장 흐름은 문자열만 나른다. 단위를 열 때 앞 단위가 남긴 시크는 버린다(시크 도중 타이틀로 나간 경우).

연출 단독 예제는 안 A(타이틀 재사용)로 했다. 타이틀의 처음부터·이어하기는 `IPlaythroughEntry`로 풀어
`ProgressionLauncher`와 `NodePlaythrough`가 각각 구현한다. `NodePlaythrough`는 줄마다 `{노드, 단위 앞 백로그, 재개 지점}`을
파일 하나에 쓰고, 노드 끝까지 보면 지운다. 저장 슬롯이 없는 예제에서는 불러오기·저장 메뉴만 숨기고 타이틀로 가기는 둔다.
검증: `tests/ProgressionHost` 25건, `tests/SaveLifecycle` 15건(연출 기억 3건, 연출 단독 회차 2건 추가).

### P4. 문서와 검증

README, SCOPE-BOUNDARY, vendoring, PLAN_framework-scope 갱신. 하네스 전부 통과. Unity 확인 목록 정리.

---

## 순서와 커밋

P0 → P1 → P2 → P3 → P4. 원본 레포는 P0 끝에 한 번(P0-3은 따로), 이 레포는 단계마다 커밋한다.
Unity 확인이 꼭 필요한 곳은 P0-3(원본 샘플 화면), P2-2(어셈블리 분리), P3-2(연출 단독 저장)이다.
