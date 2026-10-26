# 기준 기록 — framework-scope

`refactor/framework-scope` 작업 전의 동작을 고정한다. 각 단계가 끝날 때 이 기록과 비교한다.

## 기준

| | |
|---|---|
| 커밋 | `c981643952fd072a40d59bc5704f6fc5209c65bc` — refactor: SaveCoordinator 책임 분리 (`dev` = `origin/dev`) |
| 작업 브랜치 | `refactor/framework-scope` — 이 문서를 담은 커밋이 첫 커밋이며 코드 변경은 없다 |
| 기록일 | 2026-10-26 |

## 환경

| | |
|---|---|
| Unity | `6000.3.16f1` (a56f230f6470) |
| 패키지 | URP 17.3.0 · Input System 1.19.0 · Test Framework 1.6.0 · Yarn Spinner Unity 3.2.1 (git 태그) |
| 씬 | `Assets/Scenes/PresentationSample.unity` (유일한 씬) |
| URP 글로벌 설정 | `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` (GraphicsSettings 매핑 guid `18dc0cd2…`) |

## 실행 조건 — `[VNAppBootstrap]` 직렬화 값

| 필드 | 값 | 비고 |
|---|---|---|
| `progressionChapterJson` | `@Dialogue/ChapterProgression/qwer.progression.json` | 아래 토글 때문에 **쓰이지 않는다** |
| `useLearningSaveData` | **true** | |
| `learningChapterJson` | `@Dialogue/ChapterProgression/qwer_scene.progression.json` | **실제로 재생되는 챕터** |
| 저장 경로 | `persistentDataPath/saves-learning` | |
| `saveContentVersion` | **"1"** | 코드 기본값은 "2". 씬 값이 우선한다 — 아래 관찰 1 |
| `enableEquivalenceHarness` | false | 하네스 보고서를 뽑을 때만 켠다 |

## 진입 흐름 (실제)

- Play → `Awake`가 진행 데이터를 확인한 뒤 전체를 조립 → `Start`에서 **타이틀 화면**.
- 타이틀 버튼: Continue · Start · Load · Album · Settings · Quit.
  - Settings는 `Debug.Log("설정 화면.")`만 한다(미구현).
- 키보드: `Space` 전진 · 좌/우 `Ctrl` 고속 스킵 · `S` 속도 토글 · `A` 오토 · `R` 롤백.
- **README의 키 `2`~`7`은 바인딩(`VNAdvanceInputBindings`)만 있고 읽는 코드가 없다.** 눌러도 반응이 없다.

## 콘텐츠 (git blob, 앞 10자리)

| 해시 | 경로 |
|---|---|
| `efb1e3c09f` | `Assets/@Dialogue/Story_blank_ch01_ep00.yarn` |
| `7e7f58e099` | `Assets/@Dialogue/test/` (tree) |
| `937da55606` | `Assets/@Dialogue/YarnProject.yarnproject` |
| `d99142961b` | `Assets/@Dialogue/ChapterProgression/qwer.progression.json` |
| `cd08a3b97f` | `Assets/@Dialogue/ChapterProgression/qwer_scene.progression.json` |
| `1b20dbc8b7` | `Assets/Data/Generated/PortraitGeneratedDB.asset` |
| `d15c13fb13` | `Assets/Scenes/PresentationSample.unity` |

⚠ 이 기록의 화면은 **git에 없는 로컬 초상화**(`Assets/Art/Portraits/`, 123개 파일, git-ignore)에 기대고 있다.
클린 클론에서는 같은 화면이 나오지 않는다. 2단계에서 해소한다.

## 자동 회귀

### SaveLifecycle — 25/25 PASS

- 실행: `dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj -c Release`와 동치
- 환경: .NET SDK 8.0.132 (Ubuntu 24.04). NuGet 접근이 막힌 환경이라 SDK에 동봉된 Newtonsoft.Json **13.0.3**(패키지와 같은 버전)을 직접 참조했다. 소스·컴파일 대상은 그대로다.

```
PASS Reads do not create files and one session owns each playthrough
PASS Commit failure changes neither disk nor memory
PASS Malformed playthrough data is rejected at the read boundary
PASS A different content version is not resumed
PASS Save slot index is light and overwrite is atomic
PASS Legacy local bookmark files remain readable
PASS A different content version blocks a manual slot before transition
PASS Manual slot loads as a new playthrough and keeps its source
PASS New game keeps the old active save until its first file succeeds
PASS Active pointer failure preserves the old resume and permits retry
PASS ProgressionSaveBridge captures Host state at the Scene boundary
PASS Completed scene fork preserves replay path and elapsed time
PASS Retention keeps only active autosave because slots are self-contained
PASS Old server metadata is ignored and not written again
PASS Active resume keeps the scene entry snapshot and replay data
PASS Failed active resume update keeps the previous disk and memory snapshot
PASS Scene commit clears the active line resume plan
PASS A new playthrough drops the old history and waits for its first scene
PASS Scene commit failure keeps the entry checkpoint and permits retry
PASS Resuming an active playthrough restores its history and elapsed time
PASS A backlog line resolves to a fork target only after its scene is committed
PASS Manual slot capture and reload round-trip the current scene
PASS Resume without a load plan starts from the scene root
PASS Resume with an empty path still opens line seek
PASS Duplicate transitions are ignored and resume starts locally
25 tests passed.
```

### ProgressionCore (NUnit) — 미기입

NUnit 패키지를 받을 수 없는 환경이라 실행하지 못했다. GitHub Actions `Progression core`의 `c9816439` 실행 결과나
로컬 `dotnet test tests/ProgressionCore/ProgressionCore.csproj -c Release` 결과를 적는다.

- [ ] 결과:

### Unity EditMode — 미기입

`Ked.Presentation.Core.Tests`, `Ked.Progression.Tests.EditMode` — Test Runner 결과를 적는다.

- [ ] 결과:

## 수동 기록

### 영상 1회 — 생략

**생략했다(2026-10-26, 결정).** 아래 체크리스트는 이후 확인 순서로 남겨 둔다.

타이틀에서 시작해 한 번에 찍는다.

1. 타이틀 표시 — Album·Settings 버튼이 있는 마지막 모습
2. Album 열기 → 닫기
3. Start(새 게임) → 첫 장면: 캐릭터 배치·표정·포커스·화면 효과·BGM
4. `Space` 전진, `Ctrl` 고속 스킵, `S`, `A`
5. `R` 롤백 — 연출이 진행되는 도중에 한 번 포함
6. 백로그 열기 → 과거 라인으로 돌아가기
7. 선택지가 나오는 장면에서 선택 → 결과 진행
8. 시스템 메뉴 → 수동 저장 → 더 진행 → 그 슬롯 불러오기
9. Play 종료 → 다시 Play → Continue로 이어하기


### 등가성 하네스 보고서 — 생략

**생략했다(2026-10-26, 결정).** 하네스는 기준 보고서와 비교하는 도구가 아니라 라인마다 폴드와 실제 무대를
직접 판정하는 도구다. 기준 없이도 각 단계 뒤에 돌려 "불일치 0"을 확인할 수 있다. 아래 절차는 그때 쓴다.

1. 인스펙터에서 `[VNAppBootstrap] › enableEquivalenceHarness`를 켠다. **씬은 저장하지 않는다.**
2. Play → Start → 첫 장면을 끝까지 진행 → Play 종료.
3. 프로젝트 루트의 `EquivalenceReports/*.json`(git-ignore)이 생긴다. 파일명과 요약을 아래에 적는다.
4. 토글을 되돌린다(씬 변경 취소).


## 관찰 (이번 작업 범위 밖, 기록만)

1. **`saveContentVersion` 씬 값이 "1"이다.** 코드 툴팁은 "2 — Yarn 변수 층 제거(2026-09-17). 1로 저장된 회차는 재개할 수 없다"고 적는다.
   씬 값이 이를 덮고 있어, 지금 만들어지는 저장은 "1"로 찍힌다. 의도인지 확인이 필요하다.
2. `SCOPE-BOUNDARY.md` §7이 경고하는 `VNRuntimeStateInspectorWindow`는 **코드에 존재하지 않는다.** 문서만 남았다(5단계에서 정리).
3. README "사용법"의 키 `2`/`3`/`4`·"Yarn Entry Key" 필드는 현재 구현과 맞지 않는다(5단계에서 정리).
