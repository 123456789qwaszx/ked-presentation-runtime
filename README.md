# ked-presentation-runtime

Yarn Spinner를 기반으로 제작한 **비주얼 노벨 화면 연출 런타임**입니다.
대사 진행과 캐릭터·배경·화면 효과 등의 연출을 분리하고, Yarn 스크립트의 명령어로 연출을 조합할 수 있도록 구현했습니다.

## 이 저장소의 범위

> **yarn 노드 하나를 재생한다. 저작·진행·저장은 밖이다.**

저작은 외부 툴(VNTool)이 하고, 그 결과인 `.yarn` 텍스트를 이 런타임이 재생합니다.
에피소드·챕터 진행과 세이브는 이 저장소의 책임이 아니며, **필요 없어서가 아니라 주인을
옮기는 중이라서** 여기서 빠져 있습니다.

무엇이 왜 밖으로 나갔고 어떤 모양으로 돌아오는지는 **[SCOPE-BOUNDARY.md](SCOPE-BOUNDARY.md)**
에 있습니다. 어셈블리 경계는 [ASSEMBLY-BOUNDARY.md](Assets/Scripts/Ked.Presentation.Runtime/ASSEMBLY-BOUNDARY.md).

## 사용법

1. `Assets/Scenes/PresentationSample.unity`를 연다.
2. `Assets/@Dialogue/`의 `.yarn` 파일에서 재생할 노드 이름(`title:`)을 복사한다.
3. 하이어라키의 `[VNAppBootstrap]`을 선택하고 **Entry Keys** 항목에 붙여넣는다.
4. 재생 후 `2`를 눌러 해당 노드를 실행한다.

| 키 | 동작 |
|---|---|
| `2` | Node 실행 |
| `Space` | RequestNextLine |
| `좌 Ctrl` | FastForward |
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

`Assets/Ked.Presentation.Core/`

커맨드 열을 "트윈이 끝났다면 어디에 있을 것인가"로 접는 순수 C# 층입니다(엔진 의존 0).
그 계산이 실제 재생과 같은지는 `Game/StageEquivalenceHarness.cs`가 라인마다 판정합니다.

## 개발 환경

* Unity `6000.3.16f1`
* Yarn Spinner Unity `3.2.1`
