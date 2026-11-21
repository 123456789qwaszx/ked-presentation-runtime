# ked-presentation-runtime

Yarn Spinner를 기반으로 제작한 **비주얼 노벨 화면 연출 런타임**입니다.
대사 진행과 캐릭터·배경·화면 효과 등의 연출을 분리하고, Yarn 스크립트의 명령어로 연출을 조합할 수 있도록 구현했습니다.

## 이 저장소의 범위

> **Yarn 대본을 줄 단위로 연출하고, 지금 재생 중인 단위 하나 안에서 그 연출을 기억하고 되감는다.**

- **여기 있는 것** — 연출 명령의 실행과 수명, 한 줄의 표시, 전진·스킵·롤백·백로그·줄 시크, 재생 단위의 수명,
  연출 기억(저장이 없어도 들고 있는 줄 위치·선택 기록·백로그), 정지 프레임 계산.
- **꽂혀 있는 것** — 다음에 무엇을 재생할지(챕터 진행)와 확정·저장 정책은 형제 저장소 `ked-progression-runtime`의
  순수 C# 코어가 맡습니다. 이 저장소에는 복사 반입돼 있고(`Assets/Ked.Progression/`), 진행·저장 예제가 그것을 꽂아 씁니다.
- **밖에 있는 것** — 저작. 외부 툴(VnTool)이 만든 `.yarn` 텍스트와 챕터 JSON(`*.progression.json`)을 이 런타임이 재생합니다.
- **떼어 둔 것(2026-10-27)** — 이 저장소가 원본입니다. 포트폴리오에서 따로 보이려고 연출 커맨드의 **실행기**(`Assets/Ked.CommandExecutor/`)를
  [ked-command-executor](https://github.com/123456789qwaszx/ked-command-executor)에, 리그·철도·커맨드·파서·정지 프레임 코어인 **무대**(`Assets/Ked.Presentation/`의 Playback을 뺀 전부와 `ExportedTuning/`)를
  [ked-stage-runtime](https://github.com/123456789qwaszx/ked-stage-runtime)에 떼어 두었습니다(분리본). 고치는 곳은 이 저장소이고 분리본이 따라갑니다.
  가른 근거는 [docs/PLAN_stage-seam.md](docs/PLAN_stage-seam.md), 분리본의 판은 각 폴더의 `Documentation~/vendoring.md`.

결정의 근거는 [docs/PLAN_playback-unit.md](docs/PLAN_playback-unit.md)(E1–E5)와 [SCOPE-BOUNDARY.md](SCOPE-BOUNDARY.md)에 있습니다.

## 사용법

`Assets/Samples/Scenes/PresentationSample.unity`를 열고 재생한다. 두 예제 모두 타이틀의 **[처음부터] / [이어하기]**로 시작한다.
재생은 언제나 **챕터 → 장면 → 에피소드**다 — 두 예제는 어느 챕터를 돌리는지만 다르고, 같은 진행 호스트·저장을 쓴다.
시작 예제는 하이어라키 `[VNAppBootstrap]`(`SampleBootstrap`)의 **예제** 칸이고, 실행 중에는 타이틀의
**[다른 예제로]**로 바꾼다 — 챕터와 저장 폴더는 조립 때 정해지므로 씬을 다시 연다.

- **진행·저장** — **예제 › Festival Eve Chapter Json**의 챕터(`Assets/Dialogue/ChapterProgression/festival_eve.progression.json`).
  여러 장면·선택·스탯이 있다.
- **연출 데모** — **예제 › Presentation Demo Chapter Json**의 챕터(`presentation_demo.progression.json`).
  챕터 하나 · 장면 하나 · 에피소드 하나가 `Demo_Presentation` 노드를 재생한다.

둘 다 장면 경계마다 확정해 저장하고, 슬롯 저장·불러오기와 백로그 줄에서 갈라지기가 있다. 저장 폴더는 예제마다 나뉜다.

| 키 | 동작 |
|---|---|
| `Space` | 다음 대사 |
| `좌/우 Ctrl` (누르는 동안) | 빨리 넘기기 |
| `R` | 한 줄 되감기 |
| `A` | 자동 진행 토글 |
| `S` | 빠르게 토글 |

## 둘러볼 곳

### 1. 실행 예제

`Assets/Samples/Scenes/PresentationSample.unity`

프로젝트의 전체 동작을 확인할 수 있는 유일한 씬입니다.

### 2. 연출 스크립트 예제

`Assets/Dialogue/ChapterProgression/presentation_demo_Demo_Presentation.yarn` — 연출 데모 챕터의 에피소드 하나가 재생하는 노드 `Demo_Presentation`

짧은 장면 하나에서 배경·BGM·등장, 배치·크기, 표정·포즈·모션, 포커스·카메라, 화면 효과·효과음,
트랜지션을 한 번씩 지나갑니다. 공개된 예제 초상화(harin·sigure·kasmi)와 저장소에 있는 배경·음원만 씁니다.

```yarn
<<bg_sprite bg_main class_day>>
<<bg_fade_in bg_main 30fr>>
<<bgm audio/bgm_idoberg-relaxing-guitar-loop-v5-245859 2s>>
<<size s1 mid 0fr>>
<<move_by s1 0u 0.639u 0fr>>
<<fade_in s1 23fr>>
하린: 배경이 들어오고, 음악이 깔리고, 내가 걸어 들어왔어.
```

명령은 쓰인 자리가 아니라 **다음 대사의 머리**에서 한꺼번에 실행됩니다. 대사 없이 연출만 보이려면 `<<step>>`을 씁니다 —
그때까지 쌓인 명령을 실행하고, 대사창을 닫고, `pause`가 끝나면 클릭 없이 다음으로 갑니다(백로그와 되감기에는 남지 않습니다).
노드 끝에 남은 명령은 `<<step>>`이 없어도 실행된 뒤 노드가 끝납니다. `pause`가 도는 동안 클릭하면 그 연출을 끝 상태로 보내고 진행합니다.
**대사 줄에는 시간이 없습니다** — 대사 줄 머리에 기다리는 명령(`pause` · `screen_cover`)을 쓰면 오류를 남기고 기다리지 않고 실행합니다.
"배경이 들어오고 조금 뒤에 인물"처럼 시간으로 짜는 일은 줄 앞의 `<<step>>`에서 합니다.

```yarn
<<fade_out s1 15fr>>
<<pause 15fr>>
<<step>>
<<fade_in s2 15fr>>
시구레: 자리를 바꿨어요.
```

대사 **글 중간**에서 명령을 실행하려면 글에는 자리 표지 `[beat=이름/]`만 두고, 명령은 줄 앞에 `<<at 이름 명령 한 줄>>`로 씁니다.
타자기가 그 글자를 드러내기 직전에 실행되고, 표지는 글에 남지 않습니다. `<<at end …>>`는 타자기가 끝날 때입니다.
번역문은 표지만 옮깁니다 — 명령은 모든 언어에 하나입니다. 자리에는 기다리는 명령(`pause` · `screen_cover`)을 쓸 수 없습니다 — 그런 일은 `<<step>>`에서 합니다
([PLAN_line-beat.md](docs/PLAN_line-beat.md)).

```yarn
<<face s1 3>>
<<at a face_swap s1 05 13fr>>
<<at end emoji s1 star>>
하린: 말하는 도중에도 바꿀 수 있지. [beat=a/]바로 여기서 바뀌어.
```

입력 없이 넘어가는 줄은 줄 태그 `#auto`입니다. 머무를 시간을 주려면 `#auto=1.8s` — 줄이 들어온 때부터 그만큼 머물고 글이 다 보이면 넘어갑니다.

화면을 덮었다 걷는 일(암전)은 `screen_cover` · `screen_cover_clear`입니다. 덮개는 무대 셋과 컷인 · 팝업을 모두 덮고 대사창 · HUD는 덮지 않습니다.
`screen_cover`는 다 덮일 때까지 기다리므로 그 뒤의 무대 정리는 늘 덮인 밑에서 일어나고, `screen_cover_clear`는 기다리지 않고 다음 줄로 넘어가도 제 길이로 걷힙니다.
무대를 어둡게 하는 일은 `stage_dim <층|all> <세기> [길이] [이징] [무대]`입니다 — 층마다 선 검은 판의 투명도라 캐릭터는 그대로이고,
`all`은 무대 전체(속말 · 회상), `far`는 배경만(인물을 띄울 때)입니다. 세기 0이 걷기, `stage_dim_reset`이 전부 걷기입니다([PLAN_screen-cover.md](docs/PLAN_screen-cover.md)).

```yarn
<<screen_cover black 0.6s>>
<<bg_sprite bg_main street_day>>
<<pause 0.4s>>
<<screen_cover_clear 0.5s>>
<<step>>
<<stage_dim all 0.45 0.27s>>
(정말 괜찮을까……)
<<stage_dim all 0 0.23s>>
하린: 응, 괜찮아.
```

대사창 안에 인물의 초상을 띄우려면 `portrait <캐릭터> [포즈] [표정]`으로 초상 인물을 세웁니다. 초상은 화면 밖에서 찍은 그림이라
무대의 마스크 · 어둡게 · 덮개를 받지 않고 대사창과 함께 보이고 숨습니다. 초상 칸이 켜지는 줄은 화자 정책 표가 고르고(예제의 화자 `나`),
표에 없는 화자의 줄은 줄 태그 `#portrait`로 켭니다. 표정 · 몸짓은 슬롯 키 `portrait`에 보통 명령으로 씁니다 — 같은 캐릭터가 무대에 서 있어도
캐릭터 키는 무대의 인물 것입니다. 속말은 줄 태그 `#thought`입니다 — 초상은 그대로 두고 창과 글자의 색만 바뀝니다.
구도와 칸의 자리는 대본이 아니라 프리셋 에셋의 값이고, 초상 칸을 켜는지 · 창 색 · 글자색은 대사창 배치 항목의 값입니다([PLAN_portrait-capture.md](docs/PLAN_portrait-capture.md)).

```yarn
<<portrait harin a 1>>
나: 내 얼굴이 대사창 안에 보여.
<<face portrait 2>>
나: 표정은 portrait에 쓰고,
나: 속말은 태그 하나야. #thought
하린: 무대의 나는 그대로야.
<<portrait_close>>
```

진행·저장 예제의 대본은 `Assets/Dialogue/ChapterProgression/festival_eve_*.yarn`(카드 하나에 파일 하나, 챕터 JSON은 곁의 `festival_eve.progression.json`)입니다.
챕터 하나(축제 전날)에 장면 둘 — 진행 선택지와 스탯, 숨는 · 잠기는 선택지, 자동 간선,
조건 분기(`<<detour>>`로 다녀가는 카드가 제 첫머리에서 스탯을 본다), 장면 확정과 지난 장면에서 갈라지기, 챕터 끝(끝 카드)을 한 번씩 지나갑니다.

두 예제는 손으로 쓴 것이 아니라 **툴이 낸 것**입니다(2026-11-04) — 원고와 진행 JSON은
[ked-story-director](https://github.com/123456789qwaszx/ked-story-director) `samples/RuntimeSamples`, 연출과 Yarn은
[ked-story-compositor](https://github.com/123456789qwaszx/ked-story-compositor) `samples/RuntimeSamples`가 정본입니다.
두 툴은 프로젝트 폴더의 `runtime.target.json`이 가리키는 **게임 폴더**(이 폴더)에 저장할 때마다 산출물을 바로 씁니다 — 파일 이름은 `{챕터}.progression.json` · `{챕터}_{노드}.yarn`입니다. 손으로 덮어쓸 일이 없습니다.

선택지는 **에피소드 끝의 진행 선택지**(챕터 JSON의 `NextOptions`) 하나뿐입니다 — 두 툴은 대사 안 선택지(`->`)를 만들지 않습니다(2026-11-04). 런타임의 `->` 연출도 2026-11-05에 걷었습니다 — Yarn 대본에 `->`가 남아 있으면 받는 프리젠터가 없으므로 고르지 않은 채 조용히 지나갑니다(`allowOptionFallthrough`). 그래서 단위 루트부터의 재생(되감기 · 이어하기 · 갈라지기)은 줄 좌표 하나로 결정됩니다.

### 3. 대사 실행 흐름

`Assets/Ked.Presentation/Playback/DialoguePresenter/Line/`

한 줄의 대사가 들어온 뒤 연출 실행, 텍스트 출력, 입력 대기, 스킵 및 상태 복원으로 이어지는 흐름을 담당합니다.
이 폴더는 이름 있는 어셈블리 `Ked.Presentation.Playback`의 일부입니다. 롤백 기록·백로그·줄 시크·선택 기록과
재생 단위(`Host/PlaybackUnit.cs`)까지 — 저장이 없어도 연출이 스스로 기억하는 것을 같은 어셈블리가 소유합니다
([ASSEMBLY-BOUNDARY.md](Assets/Ked.Presentation/Documentation~/ASSEMBLY-BOUNDARY.md)).
호스트(예제)가 만지는 것은 재생 단위와 연출 기억의 입구(`Memory/PlaybackMemory.cs`) 둘뿐입니다.

주요 파일:

* `CustomLinePresenter.cs`
* `VNLinePresentationFlow.cs`
* `VNLinePresentationState.cs`
* `VNSeekState.cs`

### 4. 연출 명령과 실행 구조

* `Assets/Ked.CommandExecutor/` — 실행기(커맨드 수명 · 스텝 스코프 · 압축 정책 포트). 분리본 ked-command-executor가 따라간다
* `Assets/Ked.Presentation/Stage/Grammar/` — 대본 커맨드 파서(`StageCommandParser`) · 낱말 해석
* `Assets/Ked.Presentation/Stage/{Character,Shot,ScreenEffect,Audio,Control}/` — 기능마다 시스템과 그 커맨드 · 팩토리
* `Assets/Ked.Presentation/Stage/` — 무대 실행 층(리그 · 샷 · 화면 효과 · 오디오, 뿌리의 `PresentationStage`가 무대 하나의 상태)

연출 명령의 생성과 실행, 실행 수명 관리(스텝/런 스코프) 구조를 확인할 수 있습니다.

### 5. 순수 코어 — 목표 계산

`Assets/Ked.Presentation/Core/`

연출 커맨드는 트윈을 시작하기 전에 "끝나면 노드가 어떤 값이어야 하는가"를 먼저 정합니다.
이 계산을 엔진 의존이 없는 순수 C# 함수로 떼어 두었습니다 — 노드는 `NodeTargets`, 카메라는 `ShotIntents`이고,
메서드 이름은 대본 커맨드 이름을 따릅니다(`move_by` → `NodeTargets.MoveBy`).

떼어 낸 이유는 나온 값 하나가 세 곳에 쓰이기 때문입니다.

```
                    NodeTarget  (NodeTargets.MoveBy …)
       ┌────────────────────┼────────────────────┐
  ① 장부 예약           ② 트윈 종점            ③ 상태 폴드
  같은 줄의 다음 커맨드가  실제 재생              정지 프레임
  중간값 대신 끝값을 잰다                       (저작 툴 미리보기 · 등가성 판정)
```

계산이 한 자리에 있으니 재생 결과와 정지 프레임이 갈라질 자리가 없습니다.
커맨드 쪽은 세 줄입니다.

```csharp
NodeTarget target = RigTargets.MoveBy(_rect, _spec, _startPos);  // 계산 (Unity → Core 경계)
_rig.PlacementTargets.Reserve(_rect, target, Releases);          // 장부 예약, 확정 때 저절로 풀린다
_destPos = target.Value.XY.ToUnity();                            // 트윈 종점
```

같은 Core가 저작 툴(ked-story-compositor)에도 복사 반입돼 미리보기를 접습니다.
런타임에서는 접은 상태가 실제 재생한 무대와 같은지를
`Assets/Ked.Presentation/Playback/Host/StageEquivalenceHarness.cs`가 라인마다 판정합니다.

규약과 그 이유는 [target-boundary.md](Assets/Ked.Presentation/Core/Documentation~/target-boundary.md),
좌표 계산과 허용 오차는 [transform-math-and-epsilon.md](Assets/Ked.Presentation/Core/Documentation~/transform-math-and-epsilon.md)에 있습니다.

### 6. 진행 층

* `Assets/Ked.Progression/` — 판정 코어와 챕터 실행 경계(엔진 의존 0, 복사 반입)
* `Assets/Samples/Scripts/ProgressionHost/` — 진행 코어를 연출에 꽂는 얇은 어댑터. Scene 실행 순서, 로딩, 저장 브리지

챕터 JSON을 실어 불변식을 검사하고, 대사 재생 뒤 한 번 판정해 선택지를 띄우고
스탯 반영과 이동을 장면 경계에서 한 번에 커밋하는 루프는
[ProgressionDriver.cs](Assets/Ked.Progression/Core/ProgressionDriver.cs)와
[SceneRunner.cs](Assets/Samples/Scripts/ProgressionHost/SceneRunner.cs)에 있습니다.

`ProgressionDriver`는 `ISceneRunner`를 통해 게임의 `SceneRunner`를 실행합니다. SceneRunner는
재생·선택·저장처럼 비동기 또는 실패를 재현해야 하는 경계만 계약으로 두고, 고정된 게임 상태와
서비스는 구체 타입으로 사용합니다. 코어는 유니티 없이 돕니다 —
`dotnet test tests/ProgressionCore/ProgressionCore.csproj`.
SceneRunner의 실행 순서와 리플레이·롤백·로드·정지 규칙도 유니티 없이 검증합니다 —
`dotnet run --project tests/ProgressionHost/ProgressionHost.csproj`.

저장도 같은 경계로 나뉩니다. 챕터·장면·경로·스탯과 슬롯·갈라지기는 반입본의 `Ked.Progression.Save`가,
줄 위치·Yarn 선택·백로그는 저장이 없어도 연출이 늘 들고 있는 기억이고, 진행 저장에는 문자열 한 칸(`HostState`)으로 실립니다 —
[PlaybackMemory.cs](Assets/Ked.Presentation/Playback/Memory/PlaybackMemory.cs)가 꺼내고 되돌립니다.
`dotnet run --project tests/SaveLifecycle/SaveLifecycle.csproj`.

## 개발 환경

* Unity `6000.3.16f1`
* Yarn Spinner Unity `3.2.1`
