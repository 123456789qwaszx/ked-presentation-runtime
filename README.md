# ked-presentation-runtime

Yarn Spinner를 기반으로 제작한 **비주얼 노벨 화면 연출 런타임**입니다.
대사 진행과 캐릭터·배경·화면 효과 등의 연출을 분리하고, Yarn 스크립트의 명령어를 통해 여러 연출 트랙을 조합할 수 있도록 구현했습니다.

사용법: @Dialogue폴더의 'Story_**'.yarnscript의 Node명을 복사하여, 하이어아키의 'VnAppBootstrap'의 'Entry Keys'에 복사.
단축키: 2: Node실행 / 좌ctrl: fastforward / r: Rollback / a: Auto모드 토글 / s: SpeedUp모드 토글 / Space: RequestNextLine


### 1. 실행 예제

`Assets/Scenes/SampleScene.unity`

프로젝트의 전체 동작을 확인할 수 있는 메인 샘플 씬입니다.

### 2. 연출 스크립트 예제

`Assets/@Dialogue/Story_StellaEvent01_01.yarn`

대사와 함께 캐릭터 배치, 크기 변경, 페이드, 화면 효과와 같은 연출 명령을 작성한 예제입니다.

```yarn
<<cast c4 bandi a 3>>
<<place_center @4 bust 24fr>>
<<size @4 14 bust -1>>
<<fade_in @4>>
```

### 3. 대사 실행 흐름

`Assets/@Scripts/VNLinePresentationFlow/`

한 줄의 대사가 들어온 뒤 연출 실행, 텍스트 출력, 입력 대기, 스킵 및 상태 복원으로 이어지는 흐름을 담당합니다.

주요 파일:

* `CustomLinePresenter.cs`
* `VNLinePresentationFlow.cs`
* `VNLinePresentationState.cs`
* `VNSeekState.cs`

### 4. 연출 명령과 실행 구조

* `Assets/@Scripts/PresentationCore/`
* `Assets/@Scripts/Commands/`

연출 명령의 생성과 실행, 실행 수명 관리, 여러 연출 트랙의 동기화 구조를 확인할 수 있습니다.

## 개발 환경

* Unity `6000.3.16f1`
* Yarn Spinner Unity `3.2.1`
