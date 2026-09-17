# Progression 이관 계획 — ked-progression-runtime → ked-presentation-runtime

`ked-progression-runtime`에서 굳힌 Progression Core/Runtime 경계를 이 저장소로 되가져오기 위한 작업 계획이다.
분석 대상과 기준 커밋은 다음으로 고정한다.

```text
Target (이 저장소 / 바꿀 쪽)
ked-presentation-runtime/dev @ df8ec2cf

Source (가져올 쪽)
ked-progression-runtime/dev @ 581233e

현재 사본의 출처 (폐기)
ked-progression/feat/host-integration @ 82b3cf0
  → Assets/Scripts/Ked.Progression/ 으로 복사 반입돼 있음
  → 더 이상 쓰지 않는다
```

> 저장소는 셋이었으나 `ked-progression`은 폐기됐다. 진행 코어의 원본은 **Source 하나**다(§4.1 결정됨).
> 이관 뒤 `diff`의 상대도 Source 하나다.

---

# 0. 진행 현황 (2026-09-17)

```text
M0  게이트          ✔ 실질 통과    Source EditMode 20 PASS (dotnet으로 실행)
M1  코어 정렬        ✔ 완료        커밋 c1abc130
M2  Runtime 교체     ✔ 완료
M3  DIRECT 연결      ✔ 완료
M4  THIN ADAPTER     ✔ 완료
M5  Save 분리        ✔ 완료
M6  Launcher/조립    ✔ 완료
M7  실기 검증·문서    ◐ 문서만 완료 — G3/G4가 남았다
M8  SceneRunner Host 정리  ✔ 완료    고정 구현은 구체 타입, 비동기·실패 경계만 계약 유지
```

검증 상태:

```text
✔ ProgressionCore   28 PASS   현재 활성화된 순수 코어 테스트
✔ SaveLifecycle     14 PASS
✗ Unity 컴파일               (미실행)
✗ G3 통로 10개               (미실행)
✗ G4 저장 파일 회귀           (미실행)
```

M0은 원래 "Unity 컴파일 + EditMode PASS + PlayMode smoke"였다. 이 중 EditMode는
**Source의 Runtime이 `noEngineReferences: true`인 순수 C#이고 테스트도 순수 NUnit이라**
유니티 없이 그대로 돌려 통과를 확인했다. Unity 컴파일과 PlayMode Debug Host smoke는 남았지만,
Debug Host는 이관 대상이 아니므로(§4.5) M2를 막는 근거가 되지 않는다고 보고 진행했다.

이관 중에 계획에 없던 것 둘이 나왔다.

```text
SaveLifecycle 하네스가 6커밋째 빨간불이었다
  fede0693에서 ForkFromSaveSlot 인자가 늘었는데 하네스를 안 고쳤다.
  dev가 워크플로 push 목록에 없어 아무도 못 봤다 → dev·main을 목록에 넣었다.

진행 코어에 CI 게이트가 없었다
  tests/ProgressionCore + .github/workflows/progression-core.yml 로 세웠다.
  ⚠ .gitignore의 *.csproj에 걸리므로 예외(!tests/ProgressionCore/ProgressionCore.csproj)가 함께 있어야 한다.
```

---

# 1. Source 분석 — ked-progression-runtime에 실제로 서 있는 것

## 1.1 구조

Unity 프로젝트지만 진행 코드는 **엔진을 전혀 참조하지 않는다**. `Assets/Progression/Runtime` 아래 45개 `.cs`에
`UnityEngine` 참조가 0건이고, 어셈블리도 `noEngineReferences: true`로 그것을 강제한다. 로그조차 `IProgressionLog` 포트로 뽑아 두었다.

```text
Assets/Progression/
├─ Runtime/            ← 이관 대상 (어셈블리 Ked.Progression.Runtime, references: [])
│  ├─ Spec/ State/ Transition/ Vocabulary/ Loading/   ← 코어 (이 저장소의 사본과 거의 동일)
│  ├─ Scene/                                          ← 새로 선 Scene 상태·실행 층
│  ├─ Contracts/                                      ← Host 경계 9개
│  ├─ Response/CommittedChoice.cs
│  └─ ProgressionDriver.cs
├─ Debug/              ← 이관 대상 아님 (실험실로 남긴다)
└─ Tests/              ← 이관 대상 (EditMode 3파일 · PlayMode smoke 1파일)
```

`AssemblyInfo.cs`가 테스트 어셈블리 둘에 `InternalsVisibleTo`를 연다 — `ScenePendingHistory`가 `internal`이기 때문이다.

## 1.2 세 층의 분리

```text
Core   : 무엇이 유효한 진행/상태인가   ChapterDefinition · ProgressionState · SceneProgress · ScenePendingHistory · ChapterTransition
Runtime: 언제 무엇을 실행하는가       ProgressionDriver · SceneRunner · SceneRunContext
Host   : 실제로 무엇으로 실행하는가    Unity / Yarn / Stage / UI / Save
```

Runtime 실행기는 `ProgressionDriver → SceneRunner` **하나뿐**이다. 두 번째 실행기를 만들지 않는다.

## 1.3 Scene 상태 모델 — 이 이관의 알맹이

```text
SceneProgress                     ← Scene 진행 데이터의 단일 source of truth
├─ Definition / EntryState        ← Scene 수명 동안 고정
├─ RootEpisodeId / CurrentEpisodeId
├─ WorkingState = EntryState + 소비된 pending choices  (계산 프로퍼티)
├─ TryRestorePath / TakeRecordedChoice / DiscardUnconsumedChoices
├─ RewindAfter / RestartReplay
└─ CreateCommitResult → SceneCommitResult(State, Choices, WatchedEpisodeIds)

SceneRunContext                   ← 실행 중에만 필요한 것만
├─ SceneProgress Progress
├─ IReadOnlyList<ScenePathStep> RestorePath   (null=일반 진입 / empty=유효한 restore)
└─ bool ReplayPending
```

핵심은 **Runtime이 `ScenePendingHistory`를 직접 만지지 않는다**는 것이다. 모든 상태 변경이 `SceneProgress` API를 지난다.

## 1.4 Host 경계 9개

| 계약 | 요구하는 것 |
| --- | --- |
| `IScenePlayback` | `BeginSceneAsync` / `PlayNodeAsync(node)` / `PrepareReplayAsync` / `StopAsync` |
| `IChapterOptionsView` | `ShowAsync(options, hiddenCount)` / `Cancel()` |
| `ISceneReplayState` | `IsSeekingActive` / `BeginLoadReplay()` / `ClearSeek()` |
| `IRollbackHistory` | `LastHistoryIndex` / `TryTakeRollbackTarget(out int)` |
| `ISceneBacklog` | `MarkSceneStart()` |
| `IChapterLifecycle` | `BeginChapter(ChapterDefinition)` |
| `IScenePersistence` | `EnterScene(chapterId, sceneId, entryState)` / `CommitScene(chapterId, sceneId, SceneCommitResult, SceneRunOutcome)` — **저장을 실행한다** |
| `IProgressionReporter` | Chapter/Scene/Episode Enter·Commit·Exit — **관찰만 한다** |
| `IProgressionLog` | Info / Warning / Error |

`SavedLoadPlan`은 `ScenePathStep[]`로 잘려 들어온다. `YarnChoices`와 `Target(NodeName/LineId/Occurrence)`은 Runtime 밖이다.

⚠ **저장과 관찰은 다른 계약이다**(`06e6435`에서 갈라졌다). `IScenePersistence`는 실패하면 현재 Scene 실행을 실패시키고 다음 Scene으로
넘어가지 않는다. `IProgressionReporter`는 무엇도 결정하지 않는다. `SceneRunner`는 Enter/Commit 양쪽에서
**persistence를 먼저, reporter를 나중에** 부른다. 현재 계약은 동기(`void`)라 commit 경로가 async로 번지지 않는다.

## 1.5 테스트와 하네스

```text
EditMode  SceneProgressTests(256줄) · SceneRunnerTests(654줄) · ScenePendingHistoryTests(43줄)
PlayMode  ProgressionPlayModeSmokeTests(19줄)
Debug     ProgressionDebugHost + uGUI 패널 — 10개 통로를 버튼으로 재현
```

`SceneRunnerTests`의 `FailingScenePersistence`가 저장 실패 경로를 잰다 — 실패하면 Commit/Exit 보고를 만들지 않고
(`RunAsync_PersistenceFailure_DoesNotReportCommitOrExit`) run이 fault된다(`Driver_PersistenceFailure_FaultsCompletion`).

⚠ PLAN.md §8이 직접 적어 둔 대로, **테스트 소스가 활성 = Test Runner PASS 확인이 아니다.** 저장소에 Unity CI가 없다.
이관 착수 전 첫 게이트가 여기다(§6 G0).

## 1.6 문서가 코드보다 늦다

`README.md`는 아직 `ChapterProgression` · `SceneProgression` · `SceneTransaction` · `Phase`로 쓰여 있다.
실제 코드는 `ChapterDefinition` · `SceneProgress` · `SceneRunContext`이고 Phase는 없다.
`PLAN.md`와 `ADAPTER_MAP.md`는 최신이다. **README를 근거로 이관 판단을 하지 않는다.**

---

# 2. 두 저장소의 차이

## 2.1 코어 — 사실상 rename 하나

`diff -r`로 붙여 본 결과, 차이가 있는 파일은 6개이고 그 내용은 **전부 `ChapterProgression` → `ChapterDefinition`
개명**과 주석/줄바꿈이다. `EpisodeNode` · `EpisodeOption` · `ChapterInvariants` · `Vocabulary/*` · `Transition/*` ·
`Loading/Dto/*`는 바이트 단위로 같다.

```text
Loading/LoadResults.cs          rename만
Loading/ProgressionLoader.cs    rename만
Spec/ScenarioInvariants.cs      rename만
Spec/ScenarioProgression.cs     rename만
State/ProgressionState.cs       rename만
Transition/ChapterTransition.cs rename + 주석 2줄 + 서명 줄바꿈
Spec/ChapterProgression.cs ↔ Spec/ChapterDefinition.cs   rename + 필드 위치/메서드명 정리
```

한쪽에만 있는 것:

```text
Source에만: Contracts/ · Scene/ · Response/ · ProgressionDriver.cs · AssemblyInfo.cs
Target에만: Reachability/ (5파일) · Tests/EditMode 3파일
```

**Reachability는 이 저장소에서 참조 0건이다** — 툴(VnTool)의 도달성 증명용이고 런타임은 쓰지 않는다.

## 2.2 Scene 층 — 같은 알고리즘, 다른 소유권

| 축 | Target (현재) | Source (새것) |
| --- | --- | --- |
| Scene 상태 | `SceneTransaction`(커서·Phase) + `ScenePendingHistory`(pending)로 **분산** | `SceneProgress` **단일** source of truth |
| Runner의 접근 | `history.RecordChoice(...)` 등 History 직접 조작 | `progression.RecordChoice(...)` — 전부 SceneProgress 경유 |
| WorkingState | Runner가 그때그때 `EntryState.FoldChoices(...)` 호출 | `SceneProgress.WorkingState` 프로퍼티 |
| Phase | `SceneRunPhase` 16단계 기록 | **없음** (관측자가 없어 폐기) |
| restore 입력 | `SavedLoadPlan`(Path+YarnChoices+Target) 통째 | `ScenePathStep[]`만 |
| 로그 | `UnityEngine.Debug` 직접 | `IProgressionLog` 포트 |
| 취소 재확인 | playback await **뒤 없음** | await 뒤 `ThrowIfCancellationRequested()` 1회 추가 |

`ScenePendingHistory`는 두 저장소가 **의미상 동일**하다(EventKey watched 필터 포함). 포맷과 네임스페이스만 다르다.

## 2.3 실행 순서 — 같다

`RunEpisodeStepAsync`의 순서(대사 재생 → watched 기록 → recorded/seek 분기 → Via 전 pending 기록 → Via 재생 →
커서 이동 → Scene 경계 판정)와 `RestartReplayAsync`, `CommitScene`의 순서가 일치한다. §2.2의 취소 재확인 1건이
유일한 실행 의미 차이이고, 이것은 PLAN.md §7.2가 "bug-for-bug 복제 대신 no-commit invariant를 더 확실히 지키는 방어"로
의도적으로 남긴 것이다.

## 2.4 경계 — 여기가 실제 작업량

| Target 현재 | Source 계약 | 분류 |
| --- | --- | --- |
| `ScenePlaybackSession` (구체 타입 직접 주입) | `IScenePlayback` | DIRECT — 네 메서드가 이름까지 같다 |
| `ChapterOptionsView` | `IChapterOptionsView` | DIRECT — 서명 동일 |
| `BacklogRecorder` | `ISceneBacklog` | DIRECT — `MarkSceneStart()`만 쓴다 |
| `VNLinePresentationState` + `ChoiceHistory` + staged target | `ISceneReplayState` | THIN ADAPTER |
| `RollbackHistory` (`TakeRollbackTarget(out RollbackPoint)`) | `IRollbackHistory` (`out int`) | THIN ADAPTER — historyIndex만 |
| `ProgressionYarnBridge` + `YarnProject` staging | `IChapterLifecycle` | THIN ADAPTER |
| `SaveCoordinator.ReportSceneEntered/Committed` | `IScenePersistence` | THIN HOST ADAPTER — 진행 결과에 Yarn/Backlog snapshot을 얹어 확정 |
| `SaveCoordinator : IProgressionReporter` (**Save sink**) | `IProgressionReporter` (**관찰자**) | 재조립 필요 — §2.5 |
| `UnityEngine.Debug` | `IProgressionLog` | 1파일 신규 |

## 2.5 Driver가 쥐고 있던 남의 일

현재 `ProgressionDriver.Start(...)`는 진행과 무관한 것을 여섯 개나 받는다.

```text
현재  Start(YarnProject, ChapterProgression, ProgressionState,
            YarnVariableSnapshot, IReadOnlyList<DialogueLogEntry>, SavedLoadPlan)

새것  Start(ChapterDefinition, ProgressionState, IReadOnlyList<ScenePathStep>)
```

빠지는 것의 새 주인:

```text
YarnProject / YarnVariableSnapshot  → IChapterLifecycle 어댑터가 staging
DialogueLogEntry 백로그 복원        → Launcher가 Start 전에 BacklogRecorder.Restore
SavedLoadPlan.YarnChoices / Target  → ISceneReplayState 어댑터가 staging
SavedLoadPlan.Path                  → ScenePathStep[]로 잘라 Start에 전달
```

순서 보존 확인: 현재 `SyncChapterVariables()`는 Scene 루프 첫 회에만 실제로 돌고(같은 챕터면 early-return),
새 코드의 `BeginChapter()`는 루프 **전에** 한 번 돈다. 둘 다 첫 `BeginSceneAsync()`(= Yarn 변수 체크포인트 캡처)보다
앞선다. **리플레이가 복원된 변수에서 시작한다는 성질이 유지된다.**

그리고 `SceneRunner`가 받던 `Func<YarnVariableSnapshot> captureVariables`와 `ChoiceHistory`가 사라진다.
`SceneEntryReport` / `SceneCommitReport` 조립은 Host의 `ProgressionSaveBridge`(M5)가 맡는다 — Runtime은
`SceneCommitResult`와 `SceneRunOutcome`까지만 건네고, Yarn 변수·Yarn 선택·Backlog는 Bridge가 그 자리에서 캡처한다.

## 2.6 어셈블리·네임스페이스

```text
Target 현재
  Assets/Scripts/Ked.Progression/   → asmdef "Ked.Progression" (noEngineReferences: true) · namespace Ked.Progression
  Assets/Scripts/Progression/       → asmdef 없음 = Assembly-CSharp · **전역 네임스페이스**

Source
  Assets/Progression/Runtime/       → asmdef "Ked.Progression.Runtime" · namespace Ked.Progression 하나로 통합
```

즉 이관은 **전역 네임스페이스에 있던 실행 층을 `Ked.Progression` 어셈블리 안으로 넣는 일**이다.
`Ked.Progression.asmdef`는 `autoReferenced: true`이므로 Assembly-CSharp에서 별도 참조 추가 없이 보인다.
Runtime 코드에 `UnityEngine` 참조가 0건이므로 `noEngineReferences: true`를 깨지 않는다.

---

# 3. 이관이 실제로 깨뜨리는 것

## 3.1 이름 충돌 — 원자적 교체가 강제된다

새 타입을 전역 네임스페이스의 기존 타입과 **공존시킬 수 없다.** 기존 `Assets/Scripts/Progression/ProgressionDriver.cs`와
`SceneRunner.cs`는 `using Ked.Progression;`을 이미 달고 있어서, 같은 이름이 네임스페이스 안에 생기는 순간 CS0104가 난다.

충돌 목록:

```text
SceneRunner · ProgressionDriver · SceneChoice · SceneChoiceResolution · SceneRunResult
ScenePendingHistory · CommittedChoice · IChapterOptionsView · IProgressionReporter
```

→ §5 M2는 **한 커밋에서 지우고 넣는다.** 쪼개면 중간 상태가 컴파일되지 않는다.

## 3.2 `IProgressionReporter` 이름이 둘 다 필요하다

Save 경계용(현재, 2메서드)과 관찰자용(새것, 7메서드)이 의미가 다르다. 같은 이름으로 둘 다 살릴 수 없다.
→ Save 쪽을 `ISceneRecordReporter`로 개명하고 `Assets/Scripts/Save/`로 옮긴다(원래 거기가 집이다).

## 3.3 Save 회귀 하네스가 진행 파일을 직접 컴파일한다

`tests/SaveLifecycle/SaveLifecycle.csproj`가 다음을 그대로 긁어 간다.

```text
Assets/Scripts/Progression/Response/*.cs
Assets/Scripts/Progression/IProgressionReporter.cs
Assets/Scripts/Progression/ProgressionLauncher.cs
Assets/Scripts/Ked.Progression/{Spec,State,Transition,Vocabulary}/*.cs
```

그리고 `Stubs.cs`가 `ProgressionDriver.Start(object yarn, ChapterProgression, ...)`를 **손으로 흉내 낸다.**
→ 파일 위치가 바뀌면 csproj include가, 개명이 되면 Stubs/Program이 함께 깨진다. 이관의 매 단계에서
`dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj`가 초록인지 본다. 이것이 이 저장소에 있는 **유일한 CI**다.

## 3.4 `Documentation~/vendoring.md`가 거짓이 된다

지금 이 사본의 원본은 `ked-progression`이라고 적혀 있다. 이관 후 Scene/Runtime 층은 `ked-progression-runtime`에서 온다.
표를 고치지 않으면 다음 사람이 어느 쪽으로 diff를 돌려야 하는지 모른다.

---

# 4. 먼저 정해야 할 것

## 4.1 ✔ 진행 코어의 원본 — `ked-progression-runtime` 하나 (결정됨)

`ked-progression`은 쓰지 않는다. 진행 코어와 Runtime의 원본은 Source 하나이고, 이 저장소는 그 사본과 Host 구현을 갖는다.

```text
원본     ked-progression-runtime        Core + Runtime + 계약
사본     ked-presentation-runtime       Assets/Scripts/Ked.Progression/
Host     ked-presentation-runtime       Yarn / Stage / UI / Save
```

남은 숙제 하나: `ked-progression`의 dotnet 테스트 자산(`Tests/` 15파일)을 Source로 옮길지 버릴지.
Source의 EditMode 테스트와 겹치는 범위가 있으므로 M0에서 실제 커버리지를 본 뒤 정한다.

## 4.2 ✔ Reachability — 저작 도구에 위임, 반입본에서 걷었다 (2026-09-17 결정·완료)

도달성 증명의 주인은 저작 도구의 `ChapterReachabilityProver` 하나다. 반입본의 `Reachability/`(5파일)를
삭제했다 — 이 저장소 참조 0건이었고, 원본(`ked-progression-runtime`)에는 애초에 들어오지도 않았다.

이 결정이 작업 지시서(`one-stat-layer-orders.md`) §D를 **답이 아니라 소멸로 닫는다.** 상태 벡터 축소를
합의할 상대가 없어졌고, `reachability-oracle.json` 코퍼스 재생성 시점도 물을 필요가 없다.
저작 쪽은 프루버를 자유롭게 고칠 수 있다.

⚠ 잃은 것은 **두 번째 증인**이다. 이유와 함께 `SCOPE-BOUNDARY.md` §3.3에 적었다.

<details><summary>결정 전에 적어 둔 검토 (보존)</summary>

## 4.2 Reachability를 어디 둘 것인가

런타임 사용처 0건. 원본이 Source로 정해졌으므로 그쪽으로 옮기되 **Unity 반입본에는 넣지 않는** 선택이 가능하다.
지금 사본에는 들어와 있으므로, 이관 시 유지/제거를 명시적으로 정한다. 권장: **반입본에서 뺀다**(툴 전용).

</details>

## 4.3 `ChapterCompleted`를 어떻게 얻을 것인가 — 계약이 직접 준다 (결정됨)

`LocalSaveFile.ChapterCompleted`는 `ProgressionLauncher`가 "완료된 챕터의 세이브면 새로 시작"을 판정하는 데 쓴다.
`IScenePersistence.CommitScene`이 `SceneRunOutcome`을 함께 넘기므로 Host는 그대로 읽는다.

```text
ChapterCompleted = outcome == SceneRunOutcome.ChapterEnded
```

> 이 계획서의 이전 판은 Host가 `ChapterTransition.Resolve(chapter, committedState).Kind == ChapterEnded`로
> **되짚는** 우회를 적었다. 계약이 outcome을 싣게 되면서 폐기한다. 되짚기는 "종단 노드에는 선택 가능한 간선이 없다"는
> 성질에 기대므로, 조건이 전부 잠겨 `ChapterEnded`가 된 노드와 진짜 종단 노드를 구분하지 못한다.
> **Runtime이 이미 아는 사실은 Runtime이 말하게 한다.**

이것이 Runtime API가 늘어난 유일한 자리이고(`06e6435`), §1의 규칙을 지킨 것이다 — 저장 성공을 진행의 전제로 삼는 의미는
관찰자 계약으로 표현할 수 없다.

## 4.4 `SceneRunPhase`를 버릴 것인가

현재 16개 Phase를 기록하지만 **읽는 곳이 `SceneTransaction.Phase` 프로퍼티뿐이고 그 프로퍼티의 소비자가 없다.**
권장: 함께 폐기한다. 진단이 필요하면 `IProgressionLog`로 표현한다.

## 4.5 Debug Host는 옮기지 않는다

`Assets/Progression/Debug/UI/Framework/`의 `UIManager` · `UIBase` · `UIRoot`가 이 저장소의 동명 타입과 정면 충돌한다.
Debug Host는 `ked-progression-runtime`에 실험실로 남기고, 이 저장소에서는 실제 Presentation으로 같은 통로를 검증한다(§6).

---

# 5. 단계별 계획

각 단계는 **하나의 tree = 하나의 커밋**이다. `.cs` 하나 단위로 쪼개지 않는다.

## M0 — 착수 전 게이트 (Source 쪽 작업)

```text
목표     "테스트 소스가 있다"를 "테스트가 통과한다"로 바꾼다
작업     ked-progression-runtime을 Unity Editor로 열어 컴파일 확인
         EditMode Test Runner 실행 (SceneProgressionTests / SceneRunnerTests / ScenePendingHistoryTests)
         PlayMode Debug Host로 10개 통로 smoke
         작업 트리에 남아 있는 SceneRunnerTests.cs 수정(ThrowsAsync→CatchAsync) 커밋
검증     EditMode 전건 PASS
중단조건 여기서 실패가 나오면 이관을 시작하지 않는다 — 깨진 것을 옮기면 원인이 두 배가 된다
```

## M1 — 코어 정렬 (`ChapterProgression` → `ChapterDefinition`)

```text
목표   기계적 개명만으로 두 코어를 문자 단위로 같게 만든다
작업   Assets/Scripts/Ked.Progression/Spec/ChapterProgression.cs → ChapterDefinition.cs (+ .meta)
       코어 내부 6파일 참조 교체
       호출부 교체: ProgressionLauncher · SceneRunner · SceneTransaction · ScenePendingHistory ·
                    ProgressionContentLoader/Preflight · Ked.Progression.Tests 3파일
       tests/SaveLifecycle/{Stubs.cs,Program.cs} 교체
검증   Unity 컴파일 · Ked.Progression.Tests(EditMode) PASS
       dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj  초록
       diff -r 로 Spec/State/Transition/Vocabulary/Loading 이 Source와 0건
롤백   단일 커밋 revert
```

## M2 — Runtime 층 원자적 교체

```text
목표   전역 네임스페이스의 Scene/실행 층을 Ked.Progression 어셈블리 안의 새 층으로 바꾼다

넣는다 Assets/Scripts/Ked.Progression/ 아래로
         Contracts/(9) · Scene/(8) · Response/CommittedChoice.cs · ProgressionDriver.cs · AssemblyInfo.cs
       asmdef는 덮지 않아도 된다 — Source가 이미 이름·설정까지 같은 Ked.Progression이다

뺀다   Assets/Scripts/Progression/ 에서
         ProgressionDriver.cs · SceneRunner.cs · SceneTransaction.cs · ScenePendingHistory.cs
         SceneChoice.cs · SceneChoiceResolution.cs · SceneRunResult.cs · Response/CommittedChoice.cs
         IChapterOptionsView.cs · IProgressionReporter.cs

옮긴다 Response/{SceneEntryReport,SceneCommitReport,ProgressionResumePoint}.cs → Assets/Scripts/Save/Model/
       IProgressionReporter → Assets/Scripts/Save/Coordinator/ISceneRecordReporter.cs (개명)
       DialoguePresenter/.../SceneRunPhase.cs 삭제 (§4.4)

남긴다 Assets/Scripts/Progression/ → Assets/Scripts/ProgressionHost/ 로 개명
         ProgressionLauncher · ChapterOptionsView · ProgressionYarnBridge
         ProgressionContentLoader · ProgressionContentPreflight

주의   이 단계 끝에서는 아직 트리가 컴파일되지 않는다 — M3가 붙어야 닫힌다.
       M2·M3를 한 커밋으로 묶는 것을 권장한다.
```

## M3 — DIRECT 계약 연결

```text
작업   ScenePlaybackSession : IScenePlayback         (메서드 변경 없음, 선언만)
       ChapterOptionsView   : IChapterOptionsView    (Ked.Progression 쪽으로)
       BacklogRecorder      : ISceneBacklog          (MarkSceneStart 이미 있음)
       UnityProgressionLog  : IProgressionLog        (신규 1파일, Debug.Log 위임)
검증   Unity 컴파일 통과 — 여기서 처음 트리가 닫힌다
```

## M4 — THIN ADAPTER 3종

```text
ProgressionChapterLifecycle : IChapterLifecycle
    Stage(YarnProject, YarnVariableSnapshot)
    BeginChapter(ChapterDefinition) → yarnBridge.BeginChapter(project) [+ Restore(snapshot)], staging 소비

ProgressionReplayState : ISceneReplayState
    Stage(IReadOnlyList<VNChoiceRecord>, SaveLineTarget)
    IsSeekingActive   → VNLinePresentationState.IsSeekingActive
    BeginLoadReplay() → ChoiceHistory.RestoreChoices(staged) + VNLinePresentationState.BeginLoadSeek(target), staging 소비
    ClearSeek()       → VNLinePresentationState.ClearSeek()

ProgressionRollbackHistory : IRollbackHistory
    LastHistoryIndex               → RollbackHistory.LastHistoryIndex
    TryTakeRollbackTarget(out int) → TakeRollbackTarget(out RollbackPoint) 의 historyIndex만

규칙   어댑터에 진행 판정을 넣지 않는다. 변환과 staging만 한다.
주의   RollbackHistory(Scene-local 좌표)와 BacklogRecorder(회차 연속 좌표)를 한 어댑터로 합치지 않는다.
```

## M5 — Save orchestration 분리

```text
목표   SaveCoordinator가 Progression 계약을 직접 구현하던 것을 끊고,
       저장(실행)과 lifecycle(관찰)을 두 구현으로 가른다

작업   [1] ProgressionSaveBridge : Ked.Progression.IScenePersistence   ← 저장 실행
         EnterScene(chapterId, sceneId, entryState)
           → new SceneEntryReport(chapterId, entryState, yarnBridge.Capture(), backlog.NextSerial)
           → saveCoordinator.ReportSceneEntered(...)
         CommitScene(chapterId, sceneId, result, outcome)
           → new SceneCommitReport(chapterId, result.Choices, choiceHistory.CreateChoiceSnapshot(),
                                   result.WatchedEpisodeIds, result.State, yarnBridge.Capture(),
                                   backlog.Entries 복사, backlog.NextSerial,
                                   outcome == SceneRunOutcome.ChapterEnded)
           → saveCoordinator.ReportSceneCommitted(...)

       [2] ProgressionLifecycleLog : Ked.Progression.IProgressionReporter   ← 관찰만
         Chapter/Scene/Episode Enter·Commit·Exit → 로그. 저장하지 않는다

       [3] SaveCoordinator는 : ISceneRecordReporter 로 바꾸고 메서드 본문은 그대로 둔다

순서 근거
       EnterScene은 BeginSceneAsync + MarkSceneStart **직후**에 불린다 → 변수 캡처/serial 시점이 현재와 같다
       CommitScene은 현재 CommitScene과 같은 지점이다 → 스냅샷 시점이 같다
       SceneRunner가 persistence → reporter 순으로 부르므로, 저장이 실패하면 Commit/Exit 보고 자체가 없다

⚠ 실패 의미가 바뀐다
       저장 실패 = 현재 Scene 실행 실패 = 다음 Scene으로 넘어가지 않음. 디스크에는 이전 snapshot이 남는다.
       Reference에서도 SaveCoordinator의 예외가 run을 fault시켰으므로 실질 동작은 같지만,
       이제는 그것이 사고가 아니라 계약이다. ProgressionSaveBridge에서 예외를 삼키지 않는다.

검증   dotnet SaveLifecycle 하네스 초록 (Stubs/Program을 새 조립에 맞춰 갱신)
```

## M6 — Launcher / Bootstrap 교체

```text
ProgressionLauncher.LaunchCoreAsync
    기존: driver.Start(project, chapter, state, variables, backlog, loadPlan)
    새것: backlogRecorder.Restore(resume.Backlog)
          chapterLifecycle.Stage(dialogueRunner.YarnProject, resume.Variables)
          replayState.Stage(plan?.YarnChoices, plan?.Target)
          restorePath = plan?.Target != null ? plan.Path → ScenePathStep[] : null
          driver.Start(chapter, state, restorePath)

⚠ restorePath 규칙 (Reference 동작 보존)
    Target이 없으면 restorePath = null   → 현재 ApplyLoadPlan의 "표적 없으면 루트에서 시작"과 같다
    Target이 있고 Path가 비었으면 빈 배열 → Scene root 자체가 저장 위치. null과 구분한다
    null을 넘기면 BeginLoadReplay가 불리지 않는다 = Yarn 선택/시크 복원이 시작되지 않는다

VNAppBootstrap.CreateScenePlayback
    구현 9개를 모아 조립한다 (DIRECT 3 + 신규 어댑터 6)
    → SceneRunner(playback, options, replayState, rollback, persistence, reporter, backlog, log)
    → ProgressionDriver(sceneRunner, chapterLifecycle, reporter, log)
    → ProgressionLauncher(...)

    playback        = ScenePlaybackSession          (DIRECT, M3)
    options         = ChapterOptionsView            (DIRECT, M3)
    backlog         = BacklogRecorder               (DIRECT, M3)
    log             = UnityProgressionLog           (M3)
    chapterLifecycle= ProgressionChapterLifecycle   (M4)
    replayState     = ProgressionReplayState        (M4)
    rollback        = ProgressionRollbackHistory    (M4)
    persistence     = ProgressionSaveBridge         (M5)
    reporter        = ProgressionLifecycleLog       (M5)

검증   Unity 컴파일 · 전체 EditMode PASS · SaveLifecycle 초록
```

> 위 조립표는 M6 완료 당시의 기록이다. `ProgressionRollbackHistory`를 포함한 최신 Host 조립은 M8을 따른다.

## M7 — 실기 검증과 문서 정리

```text
통로 10개 수동 검증 (§6 G3)
vendoring.md 표 갱신 — 원본 저장소/브랜치/커밋, 마지막 동기화 날짜, 방향
SCOPE-BOUNDARY.md §3.3 "✅ 돌아왔다" 문단 갱신 — 새 원본과 어휘(ChapterDefinition, SceneProgress)
ked-progression-runtime/PLAN.md §12, ADAPTER_MAP.md §12 결과 반영
```

## M8 — SceneRunner Host 의존성 정리

`SceneRunner`가 `Ked.Progression` 밖의 `ProgressionHost`로 이동한 뒤에도 남아 있던
Host 계약을 실제 책임 기준으로 다시 줄인다. 구현을 바꿔 끼우기 위한 추상화는 만들지 않고,
결정적인 단위 테스트가 필요한 비동기·실패 경계만 유지한다.

```text
유지  ISceneRunner          ProgressionDriver → 게임 SceneRunner의 어셈블리 경계
유지  IScenePlayback        재생 대기·Stop·Replay 경쟁 조건을 가짜 playback으로 재현
유지  IChapterOptionsView   선택 대기·취소·응답을 UI 없이 재현
유지  IScenePersistence     저장 실패 시 Commit/Exit 금지를 재현

직접  ProgressionReplayState   게임의 Load/Seek 상태
직접  RollbackHistory          장면 안 롤백 기록의 실제 소유자
직접  ProgressionLifecycleLog  게임 런타임의 Scene 관찰자
직접  BacklogRecorder          회차 백로그와 장면 시작 순번의 실제 소유자
직접  UnityProgressionLog      Unity 콘솔 로그 구현
```

`ProgressionRollbackHistory`는 `RollbackPoint.historyIndex` 하나만 꺼내던 전달 객체라 삭제한다.
`ISceneReplayState`·`IRollbackHistory`·`ISceneBacklog`도 활성 사용처가 없어 함께 삭제한다.

`IProgressionReporter`와 `IProgressionLog`는 `ProgressionDriver`가 여전히 사용하는 순수 진행 계층의
선택적 관찰 경계이므로 파일은 유지한다. 다만 게임의 `SceneRunner`에서는 각각
`ProgressionLifecycleLog`, `UnityProgressionLog`를 직접 받는다.

검증 과정에서 기존 SaveLifecycle 하네스의 `ProgressionReplayState` 대역이 실제
`PrepareLoad(SavedLoadPlan)` 대신 폐기된 `Stage(...)`를 흉내 내고 있던 것도 고쳤다.
같은 테스트가 드러낸 `resume == null` 경로의 `NullReferenceException`도 Launcher에서 새 게임으로
떨어지도록 복구했다. 둘 다 SceneRunner 의존성 변경과 별개의 기존 회귀지만, 검증 게이트를 다시
초록으로 만들기 위해 같은 단계에서 닫았다.

검증:

```text
ProgressionCore · SaveLifecycle 회귀 테스트
활성 Runtime 코드에서 삭제한 세 계약과 ProgressionRollbackHistory 참조 0건
Unity 컴파일과 G3/G4 실기 검증은 M7의 미완료 항목으로 유지
```

---

# 6. 검증 게이트

```text
G0 (M0)  Source EditMode 전건 PASS + PlayMode smoke
G1 (M1)  Unity 컴파일 · Ked.Progression.Tests PASS · SaveLifecycle 초록 · 코어 diff 0건
G2 (M3)  Unity 컴파일 (트리가 닫힌다)
G3 (M7)  실기 10통로
         New Game / Continue(root) / Continue(mid-Scene) / Manual Load(mid-Scene) /
         정상 Scene commit / Rollback / Backlog jump(현재 Scene) / Backlog fork(이전 Scene) /
         Episode Skip / Stop·Title Exit
G4 (M7)  저장 파일 회귀 — 같은 조작으로 만든 LocalSaveFile이 이관 전후로 같은 모양인가
         (특히 Scenes[].Path · YarnChoices · BacklogSerial · ChapterCompleted)
```

G3의 각 통로에서 **확인하는 것은 로그가 아니라 invariant**다.

```text
Rollback / 현재 Scene Backlog → Scene Commit·Exit·Enter 가 없어야 한다
Stop / New Game / Manual Load → 기존 Scene Commit 이 없어야 한다
이전 Scene Backlog            → 버린 Scene 의 Commit 이 없어야 한다 (Stop + 새 run)
Episode Skip                  → 커서/Commit 을 직접 바꾸지 않아야 한다
```

---

# 7. 위험

| 위험 | 실제 내용 | 완화 |
| --- | --- | --- |
| 원자적 교체 실패 | M2/M3가 한 커밋이라 되돌릴 단위가 크다 | M1을 먼저 독립 커밋으로 끝내 되돌릴 표면을 줄인다 |
| Unity 검증 부재 | Source가 Test Runner PASS를 확인한 적이 없다 | G0를 착수 조건으로 건다 |
| Save 포맷 회귀 | 어댑터가 스냅샷을 다른 시점에 찍으면 저장이 조용히 갈린다 | M5의 "순서 근거"를 코드 주석으로 고정 + G4 |
| 저장 실패가 진행을 멈춘다 | `IScenePersistence` 실패 = Scene 실행 실패다. 지금까지 조용히 넘어가던 저장 예외가 이제 플레이를 끊는다 | `ProgressionSaveBridge`에서 예외를 삼키지 않되, 어떤 실패가 실제로 던져지는지 `SaveCoordinator` 경로를 먼저 읽는다 |
| 저장이 비동기가 되는 날 | 계약이 동기(`void`)라 지금은 commit 경로가 async로 번지지 않는다. 서버 저장이 붙으면 `Task`로 올라가고 `SceneRunner.CommitScene`도 async가 된다 | 그때 "저장 대기 중 Stop"이 새 통로로 생긴다. 지금 미리 만들지 않고, 통로 표(§6 G3)에 항목이 하나 늘어난다는 것만 기억한다 |
| 사본 표류 | 원본은 하나로 정해졌지만 사본을 사람이 지킨다 | `vendoring.md`의 대조 절차를 M7에서 Source 기준으로 다시 쓴다 |
| restorePath 규칙 오해 | null/empty를 섞으면 로드가 조용히 루트에서 시작한다 | M6의 ⚠ 규칙을 어댑터 주석으로 고정, mid-Scene Continue를 G3에서 반드시 본다 |
| `SceneProgress` 생성자 | 진입 Episode가 챕터에 없으면 NRE (현재 구현은 null을 무시) | Launcher가 이미 `TryGetNode` + `IsSceneRoot`로 거른다 — 이 방어를 지운다면 함께 고친다 |

---

# 8. 하지 않을 것

```text
Progression Runtime API를 더 키우지 않는다 — 기존 계약으로 표현 못 하는 의미가 실제로 확인될 때만 늘린다
  (지금까지 그 근거가 선 것은 IScenePersistence 하나다: 저장 성공을 진행의 전제로 삼는 의미)
Debug Host / uGUI Framework를 옮기지 않는다 (§4.5)
SceneExitReason 같은 통합 enum을 만들지 않는다 — Load/Rollback/Stop/Skip은 다른 사건이다
IScenePersistence와 IProgressionReporter를 다시 하나로 합치지 않는다 — 실행과 관찰은 실패 의미가 다르다
SaveCoordinator를 다시 Progression 계약의 직접 구현체로 만들지 않는다 (§2.5)
Reference를 bug-for-bug로 복제하지 않는다 — 취소 재확인은 남긴다 (§2.3)
Unity 실기 검증 전에 구조를 더 넓게 바꾸지 않는다
```
