# ked-presentation-runtime

Yarn Spinner를 기반으로 제작한 **비주얼 노벨 화면 연출 런타임**입니다.
대사 진행과 캐릭터·배경·화면 효과 등의 연출을 분리하고, Yarn 스크립트의 명령어로 연출을 조합할 수 있도록 구현했습니다.

## 이 저장소의 범위

> **Yarn 대본을 줄 단위로 연출하고, 지금 재생 중인 단위 하나 안에서 그 연출을 기억하고 되감는다.**

- **여기 있는 것** — 연출 명령의 실행과 수명, 한 줄의 표시, 전진·스킵·롤백·백로그·줄 시크, 재생 단위의 수명,
  연출 기억(저장이 없어도 들고 있는 줄 위치·선택 기록·백로그), 정지 프레임 계산.
- **꽂혀 있는 것** — 다음에 무엇을 재생할지(챕터 진행)와 확정·저장 정책은 형제 저장소 `ked-progression-runtime`의
  순수 C# 코어가 맡습니다. 이 저장소에는 복사 반입돼 있고(`Assets/Scripts/Ked.Progression/`), 진행·저장 예제가 그것을 꽂아 씁니다.
- **밖에 있는 것** — 저작. 외부 툴(VnTool)이 만든 `.yarn` 텍스트와 챕터 JSON(`*.progression.json`)을 이 런타임이 재생합니다.

결정의 근거는 [docs/PLAN_playback-unit.md](docs/PLAN_playback-unit.md)(E1–E5)와 [SCOPE-BOUNDARY.md](SCOPE-BOUNDARY.md)에 있습니다.

## 사용법

`Assets/Scenes/PresentationSample.unity`를 열고 하이어라키의 `[VNAppBootstrap]`(`SampleBootstrap`)을 선택한다.
**예제** 칸에서 하나를 고르고 재생한다. 두 예제 모두 타이틀의 **[처음부터] / [이어하기]**로 시작한다.

- **연출 단독** — 진행 없이 Yarn 노드 하나(**Presentation Entry Node**, `.yarn`의 `title:`)를 재생한다.
  줄이 다 보일 때마다 이어하기 지점을 파일 하나(`persistentDataPath/presentation-only/resume.json`)에 쓰고,
  [이어하기]는 노드 처음부터 다시 달려 그 줄까지 보이지 않게 빨리 감는다. 노드 끝까지 보면 지운다.
- **진행·저장** — **진행 층 › Progression Chapter Json**의 챕터(`Assets/@Dialogue/ChapterProgression/`)를 진행한다.
  장면 경계마다 확정해 저장하고, 슬롯 저장·불러오기와 백로그 줄에서 갈라지기가 있다.

| 키 | 동작 |
|---|---|
| `Space` | 다음 대사 |
| `좌/우 Ctrl` (누르는 동안) | 빨리 넘기기 |
| `R` | 한 줄 되감기 |
| `A` | 자동 진행 토글 |
| `S` | 빠르게 토글 |

## 둘러볼 곳

### 1. 실행 예제

`Assets/Scenes/PresentationSample.unity`

프로젝트의 전체 동작을 확인할 수 있는 유일한 씬입니다.

### 2. 연출 스크립트 예제

`Assets/@Dialogue/Story_blank_ch01_ep00.yarn`

대사와 함께 배경·캐릭터 배치, 페이드, 화면 효과와 같은 연출 명령을 작성한 예제입니다.

```yarn
<<bg_sprite bg_main class_day>>
<<bg_fade_in bg_main 24fr>>
<<bgm bgm_school_morning 2s>>
<<12fr>>
```

### 3. 대사 실행 흐름

`Assets/Scripts/Ked.Presentation.Playback/DialoguePresenter/VNLinePresentationFlow/`

한 줄의 대사가 들어온 뒤 연출 실행, 텍스트 출력, 입력 대기, 스킵 및 상태 복원으로 이어지는 흐름을 담당합니다.
이 폴더는 이름 있는 어셈블리 `Ked.Presentation.Playback`의 일부입니다. 롤백 기록·백로그·줄 시크·선택 기록과
재생 단위(`Host/PlaybackUnit.cs`)까지 — 저장이 없어도 연출이 스스로 기억하는 것을 같은 어셈블리가 소유합니다
([ASSEMBLY-BOUNDARY.md](Assets/Scripts/Ked.Presentation.Runtime/ASSEMBLY-BOUNDARY.md)).
호스트(예제)가 만지는 것은 재생 단위와 연출 기억의 입구(`Memory/PlaybackMemory.cs`) 둘뿐입니다.

주요 파일:

* `CustomLinePresenter.cs`
* `VNLinePresentationFlow.cs`
* `VNLinePresentationState.cs`
* `VNSeekState.cs`

### 4. 연출 명령과 실행 구조

* `Assets/Scripts/Ked.Presentation.Runtime/PresentationCore/`
* `Assets/Scripts/Ked.Presentation.Runtime/Commands/`

연출 명령의 생성과 실행, 실행 수명 관리(스텝/런 스코프) 구조를 확인할 수 있습니다.

### 5. 순수 코어

`Assets/Scripts/Ked.Presentation.Core/`

커맨드 열을 "트윈이 끝났다면 어디에 있을 것인가"로 접는 순수 C# 층입니다(엔진 의존 0).
그 계산이 실제 재생과 같은지는 `Game/StageEquivalenceHarness.cs`가 라인마다 판정합니다.

### 6. 진행 층

* `Assets/Scripts/Ked.Progression/` — 판정 코어와 챕터 실행 경계(엔진 의존 0, 복사 반입)
* `Assets/Scripts/ProgressionHost/` — 진행 코어를 연출에 꽂는 얇은 어댑터. Scene 실행 순서, 로딩, 저장 브리지

챕터 JSON을 실어 불변식을 검사하고, 대사 재생 뒤 한 번 판정해 선택지를 띄우고
스탯 반영과 이동을 장면 경계에서 한 번에 커밋하는 루프는
[ProgressionDriver.cs](Assets/Scripts/Ked.Progression/ProgressionDriver.cs)와
[SceneRunner.cs](Assets/Scripts/ProgressionHost/SceneRunner.cs)에 있습니다.

`ProgressionDriver`는 `ISceneRunner`를 통해 게임의 `SceneRunner`를 실행합니다. SceneRunner는
재생·선택·저장처럼 비동기 또는 실패를 재현해야 하는 경계만 계약으로 두고, 고정된 게임 상태와
서비스는 구체 타입으로 사용합니다. 코어는 유니티 없이 돕니다 —
`dotnet test tests/ProgressionCore/ProgressionCore.csproj`.
SceneRunner의 실행 순서와 리플레이·롤백·로드·정지 규칙도 유니티 없이 검증합니다 —
`dotnet run --project tests/ProgressionHost/ProgressionHost.csproj`.

저장도 같은 경계로 나뉩니다. 챕터·장면·경로·스탯과 슬롯·갈라지기는 반입본의 `Ked.Progression.Save`가,
줄 위치·Yarn 선택·백로그는 저장이 없어도 연출이 늘 들고 있는 기억이고, 진행 저장에는 문자열 한 칸(`HostState`)으로 실립니다 —
[PlaybackMemory.cs](Assets/Scripts/Ked.Presentation.Playback/Memory/PlaybackMemory.cs)가 꺼내고 되돌립니다.
연출 단독 예제는 같은 문자열을 노드 이름과 함께 파일 하나에 둡니다 — [NodePlaythrough.cs](Assets/Scripts/Game/NodePlaythrough.cs).
`dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj`.

## 개발 환경

* Unity `6000.3.16f1`
* Yarn Spinner Unity `3.2.1`
