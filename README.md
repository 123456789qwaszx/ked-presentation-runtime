# ked-presentation-runtime

Yarn Spinner를 기반으로 제작한 **비주얼 노벨 화면 연출 런타임**입니다.
대사 진행과 캐릭터·배경·화면 효과 등의 연출을 분리하고, Yarn 스크립트의 명령어로 연출을 조합할 수 있도록 구현했습니다.

## 이 저장소의 범위

> **yarn 노드를 재생하고, 챕터 진행을 돌린다. 저작·저장은 밖이다.**

저작은 외부 툴(VnTool)이 하고, 그 결과인 `.yarn` 텍스트와 챕터 JSON(`*.progression.json`)을
이 런타임이 재생합니다.

## 사용법

`Assets/Scenes/PresentationSample.unity`를 열고 하이어라키의 `[VNAppBootstrap]`을 선택한다.

- **노드 하나 재생**: `Assets/@Dialogue/`의 `.yarn`에서 노드 이름(`title:`)을 복사해
  **Entry Keys › Yarn Entry Key**에 넣고, 재생 후 `2`.
- **챕터 진행 재생**: **진행 층 › Progression Chapter Json**에
  `Assets/@Dialogue/ChapterProgression/`의 `.json`을 물리고, 재생 후 `4`.

| 키 | 동작 |
|---|---|
| `2` | 노드 하나 실행 (Yarn Entry Key) |
| `3` | 디버그 에피소드 사슬 실행 |
| `4` | 챕터 진행 시작 (Progression Chapter Json) |
| `Space` | RequestNextLine |
| `좌/우 Ctrl` | FastForward |
| `R` | Rollback |
| `A` | Auto 모드 토글 |
| `S` | SpeedUp 모드 토글 |

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

`Assets/Scripts/DialoguePresenter/VNLinePresentationFlow/`

한 줄의 대사가 들어온 뒤 연출 실행, 텍스트 출력, 입력 대기, 스킵 및 상태 복원으로 이어지는 흐름을 담당합니다.

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

* `Assets/Scripts/Ked.Progression/` — 챕터·에피소드 판정 코어(엔진 의존 0, 복사 반입)
* `Assets/Scripts/Progression/` — 로딩·프리플라이트·드라이버(호스트 접착)

챕터 JSON을 실어 불변식을 검사하고, 대사 재생 뒤 한 번 판정해 선택지를 띄우고
스탯 반영과 이동을 한 연산으로 커밋하는 루프는
[ProgressionDriver.cs](Assets/Scripts/Progression/ProgressionDriver.cs)에 있습니다.

## 개발 환경

* Unity `6000.3.16f1`
* Yarn Spinner Unity `3.2.1`
