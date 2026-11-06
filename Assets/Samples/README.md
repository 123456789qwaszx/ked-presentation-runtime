# Samples — 예제 게임

연출 런타임(`Ked.Presentation`)과 진행(`Ked.Progression`)을 꽂아 실제로 돌리는 예제다.
라이브러리가 아니라 게임 하나라서, 폴더를 **게임에서 맡은 역할**로 나눈다.

## 돌려 보기

`Scenes/PresentationSample.unity`를 열고 재생한다. 예제는 둘이고, 하이어라키의 `SampleBootstrap` › **예제** 칸이나
타이틀의 **[다른 예제로]**로 고른다.

| 예제 | 챕터 JSON | 무엇을 보여 주나 |
|---|---|---|
| 축제 전야 | `Assets/Dialogue/ChapterProgression/festival_eve.progression.json` | 여러 장면 · 선택 · 스탯 · 저장과 갈라지기 |
| 연출 데모 | `Assets/Dialogue/ChapterProgression/presentation_demo.progression.json` | 연출 커맨드를 한 번씩 지나가는 장면 하나 |

대본과 챕터 JSON은 `Samples/` 밖의 `Assets/Dialogue/`에 있다 — 저작 툴(ked-story-director · ked-story-compositor)이 내는 자리다.

## 폴더 지도

| 폴더 | 무엇 |
|---|---|
| `Scenes/` | 실행 입구 — 씬 하나 |
| `Scripts/` | 예제 코드(기본 어셈블리). `Bootstrap/` 조립과 예제 고르기 · `ProgressionHost/` 진행 코어를 연출에 꽂는 어댑터(장면 실행 · 로딩 · 저장 브리지) · `Stage/` 무대가 서는 자리(`PresentationUIRoot`)와 디버그 오버레이 |
| `UI/Kit/` | 화면 묶음 — 화면마다 코드 · 프리팹 · 그림 · Presentation, 공통 위젯 · 글꼴, 화면 짓기 도구(어셈블리 `Ked.Samples.UI` · `.Editor`, UIPresentationFlow 위) |
| `UI/App/` | 화면 흐름과 바인딩, 캔버스 — 호스트를 직접 받아야 해서 기본 어셈블리에 있다 |
| `Content/` | 작가가 만드는 것 — `Portraits/` 초상화 원화 · `Motions/` 모션 저작 원본(→ `Assets/Dialogue/motions.json`) · `Resources/` 키로 읽는 배경 · 음원 · `Art/` 이모지 |
| `Settings/` | 씬이 꽂는 설정 에셋 — 대사창 배치 · 화자 정책 · 초상 DB와 빌드 설정 · 오디오 믹서 |

연출 커맨드의 프리셋과 튜닝 기본값은 예제가 아니라 프레임워크 쪽(`Ked.Presentation/Stage/Presets/`)에 있다.
