# Framework Scope 정리

기준: `dev` @ `c9816439` (refactor: SaveCoordinator 책임 분리)
작업: `refactor/framework-scope`
기준 기록: [baseline-framework-scope.md](baseline-framework-scope.md)

## 목표 한 문장

> Yarn 대본과 진행 데이터를 받아 연출을 실행하고, 재생 제어와 상태 복원을 제공하는 Unity 프레임워크.
> 스키마 기반 리그 생성·검증 도구와 실행 예제를 포함한다.

완성형 비주얼 노벨이 아니다. 완료 기준은 "게임 부가 기능을 다 지웠다"가 아니라
**"다른 개발자가 클린 클론에서 예제를 실행하고, 대본을 수정하고, 리그나 명령을 확장할 수 있다"**이다.

## 판단 기준

기능의 가치가 낮은가가 아니라 **이 프레임워크가 책임져야 하는 기능인가**로 가른다.

| 분류 | 뜻 | 처리 |
|---|---|---|
| 본체 | 다른 게임이 가져가도 그대로 남는 것 | 유지 |
| 샘플 | 본체를 보여주기 위한 조립·콘텐츠·UI | `Samples/`로 내리고 간소화 |
| 게임 층 | 특정 작품의 부가 기능 | 제거. 사유를 SCOPE-BOUNDARY에 남긴다 |

SCOPE-BOUNDARY.md §1은 "여기 적힌 것은 전부 필요한 기능이고 주인을 옮기는 중"이라고 못 박고 있다.
게임 층 제거는 **주인을 옮기는 것이 아니라 범위 밖**이므로, 그 문서에 **별도 섹션**을 만든다.
같은 표에 섞으면 그 문서가 경고하는 오독이 그대로 생긴다.

## 확정된 결정

| # | 항목 | 결정 |
|---|---|---|
| D1 | 학습 데이터 토글 (`useLearningSaveData`·`learningChapterJson`) | **통합 샘플에 유지.** 이번 작업에서 기능을 추가하거나 확대하지 않는다 |
| D2 | 로컬 파일 저장 (`LocalFileSaveStore` 등) | **본체의 기본 제공 구현으로 유지.** 저장 경로와 사용 여부는 호스트가 결정한다 |
| D3 | Settings | 실제 연결을 확인하고 미구현이면 제거 → **확인 결과 미구현(`Debug.Log`만). 1단계에서 제거** |
| D4 | 예제 초상화 위치 | `Assets/Samples/Portraits/<cid>/<variant>/<emotion>.png` |
| D5 | 기준 기록 | 영상 1회 + 회귀 검증 결과 + 하네스 보고서. 커밋·콘텐츠·실행 조건도 함께 기록 → baseline 문서 |

## 분류표

| 영역 | 위치 | 분류 | 비고 |
|---|---|---|---|
| 순수 코어 (폴드·등가성) | `Scripts/Ked.Presentation.Core/` | 본체 | |
| 커맨드·리그·실행 수명 | `Scripts/Ked.Presentation.Runtime/` | 본체 | |
| Yarn 파싱·브리지 | `Scripts/YarnBridge/` | 본체 | |
| 라인 표시 트랜잭션 | `Scripts/DialoguePresenter/` | 본체 | 대사창·선택지 기본 구현은 샘플 UI와의 경계 확인 |
| 전진·스킵·롤백·백로그 | `Scripts/FeatureController/` | 본체 | 키 `2`~`7` 바인딩은 소비자 없음 — 3단계에서 처리 |
| 진행 판정·실행 | `Scripts/Ked.Progression/` + `ProgressionHost/` | 본체 (독립 모듈) | 복사 반입본, 건드리지 않음 |
| 저장 모델·조정자·포크 | `Scripts/Save/{Model,Coordinator,Fork}` | 본체 (독립 모듈) | append-only 타임라인·포크 |
| 로컬 파일 저장 | `Scripts/Save/{Persistence,Slots,Maintenance}` | 본체 (기본 제공 구현) | D2 |
| 등가성 하네스 | `Scripts/Game/StageEquivalenceHarness.cs` | 본체 (Diagnostics) | `Game/`에서 꺼낸다 |
| 에디터 도구 | `Assets/Editor/` | 본체 (Editor) | |
| 익스포트 산출물·하네스 | `ExportedTuning/`, `tests/` | 본체 | CI 게이트 3개 유지 |
| 부트스트랩·세이브 플로우 | `Scripts/Game/` (하네스 제외) | 샘플 | 초기화 경계 분리 대상. 학습 토글은 통합 샘플에만(D1) |
| UI 프레임·화면·바인딩 | `Scripts/UI/` | 샘플 | 타이틀은 예제 선택 화면으로 |
| 예제 대본·진행 JSON | `Assets/@Dialogue/` | 샘플 | 재캐스팅 대상 |
| 앨범·CG 해금·앨범 저장 | `Scripts/Album/`, `UI/Screens/AlbumUIRoot.cs`, `UI/Bindings/VnScreenBindings.Album.cs`, `Data/Album/`, `VNAlbumSlotButton.prefab` | 게임 층 | 제거 |
| Settings 버튼 | `TitleUIRoot`·`VNScreenBindings.Title` | 미구현 메뉴 | 제거 (D3) |
| URP 템플릿 잔해 | `Assets/TutorialInfo/`, `Assets/Readme.asset` | 잔재 | 제거 |
| 작업 기록 문서 | `docs/` 중 handoff·PLAN·learning·m8b 계열 | 기록 | `docs/archive/`로 이동 |

## 사전 조사로 확인한 사실

**범위**
- 앨범은 전체 약 5만 줄 중 약 500줄(1%)이다. 인상을 만드는 것은 **경계가 보이는가**와 **클론해서 돌아가는가**다.

**앨범·설정**
- 앨범 연결 지점: `VNAppBootstrap`(DB 참조·`BootstrapAlbum()`·Configure 인자), `VNScreenBindings`(필드·Configure 인자), `.Title`(버튼 이벤트), `.Album`, `TitleUIRoot`(`Refs.AlbumButton_Button`·`AppendMissing`).
- 앨범 → Save 방향으로만 의존한다(`AtomicFile`, `SaveJson`). 앨범을 지워도 Save와 `tests/SaveLifecycle`은 무사하다(해당 csproj는 `UI/`·`Album/`을 컴파일하지 않는다).
- `cg_test_01/02`가 가리키는 그림은 `Art/Emoji/19_*.png`, `18_*.png` — **연출용 이모지다. 그림은 남기고 SO만 지운다.**
- Settings 버튼 핸들러는 `Debug.Log("설정 화면.")`뿐이다.
- `UIBase`는 `Refs` enum **이름**으로 자식 오브젝트를 런타임에 찾는다. enum 항목을 지워도 다른 참조의 인덱스가 어긋나지 않는다.
- `UIManager`는 자식의 `UIBase`를 스캔해 등록한다. 스크립트가 빠진 `UIRoot_Album` 오브젝트는 무시되므로 **코드를 먼저 지워도 실행은 깨지지 않는다.**
- 씬에서 지울 오브젝트: `UIRoot_Album`(하위 `AlbumBG_Root`·`AlbumBG_Image`·`AlbumMenu_Root` 포함), `AlbumButton_Button`, `SettingsButton_Button`.

**클린 클론**
- 예제 대본이 캐스팅한 5명과 `PortraitGeneratedDB`의 48개 경로가 전부 git-ignore된 `Art/Portraits/`를 가리킨다. **클린 클론에서 캐릭터가 하나도 뜨지 않는다.**
- `.gitignore`는 `Art/Portraits/` **폴더 자체**를 막는다. 무시된 폴더 안을 `!`로 되살릴 수 없으므로 공개 초상화는 **별도 폴더로 .meta째 이동**한다(GUID 보존).
- `PortraitBuildSettings.scanFolders`가 `dungeon`·`fillintheblank`를 가리킨다. 그대로 재생성하면 다시 비공개 경로가 들어간다.
- `PortraitResolver`는 없는 표정을 경고 후 `01`로 대체한다.

**진입·초기화**
- 실제 진입점은 `Start()` → `OpenTitleMenu()`다. **README의 키 `2`~`7`은 바인딩만 있고 읽는 코드가 없다.** "노드 하나 재생" 경로는 현재 존재하지 않는다.
- `VNAppBootstrap.Awake()` 첫 줄이 `TryBootstrapProgressionData()`다. 챕터 JSON이나 `saveContentVersion`이 비면 전체 초기화가 중단된다. 연출만 확인하는 길이 없다.
- 씬 값은 `useLearningSaveData = true`, `saveContentVersion = "1"`(코드 기본값 "2")이다. 후자는 baseline 관찰 1.

**문서**
- README·SCOPE-BOUNDARY는 "저장은 밖이다"라고 쓰여 있으나 `Save/` 19개 파일과 `tests/SaveLifecycle`이 실존한다.
- SCOPE-BOUNDARY §7이 경고하는 `VNRuntimeStateInspectorWindow`는 코드에 없다.

## 단계

각 단계는 **하나의 커밋 묶음**으로 끝나며, 끝에 컴파일·회귀 하네스·샘플 재생을 통과해야 다음 단계로 간다.

> **순서 정정(2026-10-26).** 처음 계획은 "예제 콘텐츠 교체"를 초기화 경계보다 앞에 두었다. 그러나 샘플의 **설계**는
> 3단계가 정할 구조(어떤 진입점이 있고 누가 재생을 쥐는가)에 달려 있다 — 연출 데모 대본을 쓰려다 그 대본에 닿을
> 진입점이 없어 미룬 것이 증거다. 2단계는 "지금 유일한 실행 경로를 클린 클론에서 돌게 하기"로 좁혀 끝냈고,
> 샘플 설계는 경계 분리 뒤(4단계)로 옮겼다.

### 0. 기준 보존 ✅
- `refactor/framework-scope` 생성, 기준 기록(baseline 문서). 영상·하네스 보고서는 생략 결정(2026-10-26).

### 1. 게임 층 제거 ✅
- `01fbf779` 앨범·CG 해금·미구현 설정 · `bd75fe6b` URP 템플릿 잔해 · `9cf2dca4` 씬 오브젝트(삭제 136블록, 추가 0).

### 2. 클린 클론 복구 ✅
- `e95261cc` 공개 초상화(harin·sigure·kasmi)를 `Assets/Samples/Portraits/`로, harin·sigure `a/01~05`·`b/01~05` 더미(고유 GUID).
- `fd70efc5` `qwer_scene` 재캐스팅·`stat()` 읽기, 비공개 `qwer` 시험 챕터 제거.
- `6c17fec3` 초상화 DB·크기 덤프 재생성(비공개 경로 0).
- 정적 검사: 씬이 닿는 에셋 중 레포 밖 242건 = 패키지 203 + **끊긴 UI 이미지 17(관찰 6)** + 미확정 22(설치 폴더 내장 에셋 추정, 실제 클린 클론으로 확인).

### 3. 초기화 경계 분리 — 다음
목표: **Yarn 노드 하나만 주고도 연출이 돈다.** 지금은 `Awake`가 챕터 JSON·저장 설정을 먼저 요구하고, 롤백·백로그 점프·타이틀 복귀가 `ProgressionLauncher`를 직접 부른다.
- 재생을 쥐는 쪽을 계약 하나로 세운다(`IPlaybackHost`: 실행 중인가 / 처음부터 다시 / 끝내기). 입력·UI는 이 계약만 안다.
- 연출 조립을 순수 C# 조립기로 뽑는다. 연출 단독은 노드 호스트, 통합은 기존 `ProgressionLauncher`가 그 계약을 구현한다.
- 한 실행에 호스트는 하나 — 9/17에 걷은 디버그 러너처럼 두 주행자가 공존하지 않는다.
- 세부 설계는 착수 전 확인을 받는다.

### 4. 샘플 설계
- 연출 데모 대본(Story_blank 대체): harin·sigure·kasmi, 배치·크기·표정·모션·포커스·화면 효과·BGM·트랜지션. 실제로 있는 음원 키만(관찰 4).
- 타이틀을 **예제 선택 화면**으로: 연출 예제 / 진행+저장 예제. 저장 슬롯 화면 간소화.
- 끊긴 UI 이미지 17개(관찰 6) 처리.
- 소비자 없는 키 바인딩 `2`~`7` 정리.

### 5. 폴더 경계 정리
1. 샘플 코드를 `Assets/Samples/` 아래로: 부트스트랩·세이브 플로우·`UI/`·`@Dialogue/`·샘플 프리팹.
2. 본체 코드를 한 뿌리 아래로. asmdef는 아직 만들지 않는다.
3. 본체 → 샘플·진행·저장 방향 참조 0건을 grep으로 확인.

### 6. 문서
1. `README.md`: 목표 한 문장, 클론 후 실행법, 예제, DSL 입구, 리그 생성 입구, 확장 방법, 지원 범위와 제한. 죽은 키 안내 제거.
2. `SCOPE-BOUNDARY.md`: §0 갱신(저장이 돌아왔음), §3.2, §6 계약서 표, §7 인스펙터 경고 정리, §8.
3. `docs/`: 읽을 문서는 남기고 handoff·PLAN·learning·m8b 계열은 `docs/archive/`로. 이 문서와 baseline도 완료 후 archive로.
4. 면접 대응 문서는 이 레포 밖에 둔다.

### 7. 검증 후 완료
- ProgressionCore `dotnet test`, SaveLifecycle `dotnet run`, ProgressionHost `dotnet run` — CI 3개 녹색
- Unity 컴파일, 새 경고 0, 누락 스크립트·참조 0
- 연출 예제: 재생·표정·포커스·롤백·백로그 점프·스킵
- 통합 예제: 새 게임→장면 완료→종료→이어하기, 수동 슬롯, 백로그→롤백, 완료 장면 fork
- 등가성 하네스: 예제 장면에서 불일치 0
- **클린 클론에서 위 전부 재현**
- 이후 선택: asmdef 분리 → UPM 패키지화. 경계가 실제로 작동한 뒤에만.

## 하지 않는 것

- Progression 반입본(`Ked.Progression/`) 수정
- 커맨드 어휘·DSL 변경
- 저장 포맷 변경
- 학습 토글 기능 확대(D1)
- 처음부터 패키지화
- `Art/Portraits/` 비공개 아트 커밋
- baseline 관찰 1(`saveContentVersion` 씬 값) 수정 — 확인 후 별도 작업

## 위험

| 위험 | 대응 |
|---|---|
| 씬 직렬화 참조가 코드 삭제 뒤 깨짐 | 코드→씬 순서 고정. `UIManager`가 스캔 등록이라 누락 스크립트 오브젝트는 무시됨을 확인함 |
| 타이틀 버튼 두 개 제거 후 레이아웃 공백 | 1단계 Unity 확인 항목 |
| 초상화 이동 시 GUID 변경 | .meta 동반 이동, DB 재생성 |
| 부트스트랩 분리 중 공통 조립 중복 | 조립기 하나로 모은 뒤 두 부트스트랩이 호출 |
| 문서가 다시 구현보다 뒤처짐 | 5단계를 별도 커밋으로, 6단계에 "문서=구현" 항목 |
| Unity가 열린 채 브랜치 전환·충돌 시 설정 에셋 재생성 | 이 작업 중 브랜치 전환 금지. 필요하면 Unity를 닫고 전환 |
