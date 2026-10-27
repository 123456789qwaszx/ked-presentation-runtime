# 무대 이음매 — 실행기 · 무대 · 재생을 가르는 선

기준: `dev` @ `f79f13d` (fix(tests): 하네스 둘이 끝 화면을 받는 생성자로 다시 빌드된다)
작업: `refactor/stage-seam`

**현재 위치(2026-10-27):** ①②③ 끝(Unity 확인 남음). 다음은 리포지토리 분리(§4), 그 뒤 모션(§5).

## 0. 왜

이 저장소에는 관심사가 셋 섞여 있다.

| 조각 | 무엇 | 지금 자리 |
|---|---|---|
| **실행기** | 커맨드 하나의 수명 — `CommandBase` · `ClaimTweenCommandBase` · `CommandRunScope` · `LifetimeScope` · 포트(입력·시간·신호·정책) | `Runtime/PresentationCore/{Base, Execution, CommandRunScope, LifetimeScope, Signal*, Unity*}` |
| **무대** | 리그 계층 · 철도(shot·depth 층) · 커맨드(스펙·실행 어댑터) · 대본 문법의 뜻(파서) · 정지 프레임(리듀서·장부·클레임) · 이징·커브 | `Core/` 전부 + `Runtime/{Commands, CharacterRig, ShotResponse, BackgroundRig, ScreenEffect, StageMask, Audio, CoreBridge, Equivalence}` + `PresentationCore/PresentationStage*` |
| **재생** | 대사 재생 루프 · 롤백·백로그·시크 · 연출 기억 · Yarn 등록·전달 · 대사창·선택지·UI | `Playback/` · `Samples/` |

캐릭터 리그 쪽을 **별도 저장소에서 키우려면** 실행기와 재생이 리그를 몰라야 하고, 리그가 실행기와 재생을 몰라야 한다.
그 선을 코드에 먼저 긋고(§1–3), 그 다음 가른다(§4). 모션(§5)은 가른 뒤 무대 저장소에서 만든다 — 파서가 거기 있으니까.

## 1. ① 리졸버는 무대만 받는다 — `c358dae2`

`CharacterRigTargetResolver` · `CharacterDepthResolver` · `PortraitResolver` · `CharacterFocusPlacementSolver` ·
`CharacterFocusPointResolver`가 `CommandRunScope`에서 쓰던 것은 레지스트리 셋뿐이었다. `PresentationStage`를 받게 바꿨다.
`CharacterRig/`·`ShotResponse/`에서 `CommandRunScope` 참조 **0건**. 커맨드는 `scope.Stage`를 넘긴다.

## 2. ② 스코프에서 VN 깃발을 걷는다 — `80288682` · `f2c1700a`

커맨드가 스코프에 실제로 묻던 것은 둘이었다 — 실행을 **압축**할지(`ShouldCompressCommandExecution`), **시간 배율**(`ScalePresentationDuration`).
그 둘을 `ICommandPacePolicy` 포트로 두고 스코프는 `(정책, 무대)`만 받는다. `IsSpeedUpMode`·`IsRapidSkipMode`·`IsAutoMode`·`IsSeekPassThrough`는 사라졌다.

VN 쪽은 `PresentationSessionContext`(Playback/Host)가 재생 상태를 그 둘로 접는다. 동작 불변의 근거:
속도 모드가 아닐 때 `TimeScale`은 이미 1이었고(`VNFeatureController.ApplySpeedUpModeState`), `StageMaskMotionCommand`의
시크 가드는 기본 `SkipPolicy`라 전후 모두 닿지 않는다. 재생 성격 파일 다섯(`VNPlaybackRuntimeState` · `VNPlaybackSettings` ·
`VNAdvanceGate` · `ISeekStateQuery` · `PresentationSessionContext`)은 Playback으로 옮겼다(이동만, GUID 그대로).

## 3. ③ 대본 커맨드의 뜻은 무대가 갖는다 — `d36c5438`

`Playback/YarnBridge/CommandBridge.*.cs`에 있던 "인자 → CommandSpec" 번역(인자 자리·생략값·단위·커브·숫자)을
`Runtime/Commands/StageCommandParser/`로 옮겼다. 입력은 코어의 `StageCommand`다 — 하네스의 폴드와 같은 낱말.

- **표 한 곳**(`StageCommandParser.Table.cs`)이 이름·인자 자리·생략값을 갖는다. 핸들러 시그니처의 기본값은 걷었고 본문은 브리지의 것 그대로다.
- 브리지는 Yarn 등록(이름·인자 수)과 `Forward`뿐이다. 전달 메서드의 인자는 전부 문자열, 생략은 `null` — 생략값이 두 곳에 있지 않게.
  숫자 변환도 파서가 한다(못 읽으면 경고 + 생략값). ysls는 파라미터 이름을 유지하고 기본값·숫자 타입 표기만 잃는다.
- 대사창(`box_*`·`surface_*`)·`debug_log`는 재생 쪽이라 브리지에 남는다.
- 남은 중복은 **이름·인자 수**가 브리지 등록과 파서 표 두 곳에 있다는 것이다. 파서가 모르는 이름이 오면 `Forward`가 오류를 찍는다.
  파서 표에서 Yarn 등록을 **생성**하면 한 곳이 된다(`AddCommandHandler(string, Delegate)` + 인자 수별 전달자) — 대신 ysls 항목이 사라진다.
  VS Code Yarn 자동완성을 버려도 되면 그렇게 간다.

### 상태 소유권 — 무대 안의 선

무대 안은 어셈블리가 아니라 **누가 어느 상태를 쓰는가**로 긋는다. depth가 경첩이라(리그 값이면서 철도 층을 고른다) 억지로 어셈블리를 가르면 depth가 두 곳에 생긴다.

| 계통 | 쓰는 상태 | 쓰는 방식 | 커맨드 |
|---|---|---|---|
| **철도**(shot) | `StageState.Shot` · 무대 루트(`StagePan_Root`·`StageZoom_Root`) · 층 프레이밍 | 의도 한 장 → 응답 시스템이 매 프레임 전부 다시 씀 | `shot_*` |
| **열차 배치**(슬롯 축) | `Track_Focus`(해) · `DepthY`·`DepthScale` · `Track*`·`Rotation`·`SwayPivot`·`Scale` | 클레임 → 장부 → 트윈 → 폴드 | `place`·`size`·`move_by`·`left/right/up/down`·`rotate_by`·`scale_by` |
| **열차 연기**(초상 축) | `CharacterPortrait_*` | 클레임(포즈) 또는 순변위 0(몸짓) | `gesture`·`char_rotate_to`·`char_scale_to`·`mirror`·`face`·`pose` · **모션(§5)** |

세 계통은 서로의 상태를 **쓰지** 않는다. `shot_focus_to`가 열차의 정착 체인을 **읽는** 것은 허용이다.

## 4. 리포지토리 분리 (다음)

패키지(UPM)는 만들지 않는다. `Ked.Progression`과 같은 **복사 반입**이다([vendoring.md](../Assets/Ked.Progression/Documentation~/vendoring.md)).

| 저장소 | 내용 | 원본/사본 |
|---|---|---|
| `ked-command-executor` | 실행기 — `CommandBase` · `ClaimTweenCommandBase` · `CommandRunScope` · `LifetimeScope` · `CommandExecutor` · `SequencePlayer` · 포트 | 원본. 무대·재생 둘 다 반입 |
| `ked-stage-runtime` | 무대 — `Core/` 전부 · 리그·철도·커맨드·파서 · 무대 계약(`IShotResponseStageProvider` 등) · 등가성 캡처 | 원본. 재생이 반입 |
| `ked-presentation-runtime` | 재생 — 지금 저장소. Playback · 예제 · 철도 구현(`PresentationUIRoot`) | 실행기·무대·진행을 반입 |

가르기 전에 이 저장소에서 하나 더 긋는다: **스코프에서 `PresentationStage`를 뺀다.** 커맨드 팩토리(`CharacterRigCommandFactory` 등)가 무대를 들고
커맨드 생성자에 넣으면, 스코프는 실행 수명만 남고 실행기 → 무대 참조가 0이 된다. 그러면 `PresentationCore/`가 실행기와 무대로 깨끗이 나뉜다.

`partial class`는 어셈블리를 못 넘는다(§5, ASSEMBLY-BOUNDARY). 철도 계약은 무대에, `PresentationUIRoot`의 partial 구현은 재생에 남는다.

## 5. 모션 (가른 뒤, 무대 저장소에서)

정한 것(2026-10-27):

- 모션 = 채널들. 채널 = (노드 또는 shot 필드, 종류 `position|scale|euler`, `mode: offset|absolute`, `CurveKey[]` 초 단위).
- **채널마다 마지막 키 = 그 노드의 클레임.** 시작에 장부 게시, 재생은 샘플러 하나가 매 프레임 `CurveFunctions.Evaluate`, 폴드는 마지막 키만.
  "이징은 종점에 관여하지 않는다"가 그대로 선다 — 종점은 평가 결과가 아니라 키 값이다. `gesture`의 순변위 0은 "마지막 키 = 시작값"인 특수한 경우다.
- `Track_Focus`는 키잉 금지(로더가 거부) — 저작값이 아니라 해(解)다. focus를 움직이려면 의도(ScreenPoint·프리셋)를 키잉한다(2단계).
- `mode`는 **채널 단위**, 기본값 없음(빠지면 로더가 거부). offset의 시작값은 정착값이다 — 키잉하는 노드를 먼저 `DOKill(true)`.
- **1단계는 노드 채널만.** 카메라 채널(zoom·pan)과 의도 채널은 둘 다 `ShotIntentState`를 건드려 같은 소유권 규칙이 필요하므로 2단계에서 함께.
  같은 줄의 `<<shot_*>>` + `<<motion>>`은 1단계에서도 이미 겹쳐 돈다 — 쓰는 곳이 다르다.
- 값은 리그 로컬 px·도·배율. 원근(`DepthScale`)·시차(층 프레이밍)·카메라는 위에서 곱해지므로 모션은 셋을 모른다.
- `curves.json`은 이동 명령의 이징(모양)으로 그대로 남는다. 모션은 내용이다.

그 전에 남은 것: `curves.json`을 `Application.dataPath`로 읽어 빌드에서 안 읽힌다(TextAsset으로, [PLAN_framework-scope.md](PLAN_framework-scope.md) §5).

## 6. 검증

- 등가성 하네스: 예제 장면에서 불일치 0 — ①②③ 전부 "값·순서·부작용 불변"을 주장하므로 이것이 오라클이다.
- EditMode: `StageCommandParserTests` 7개 + 기존 전부.
- Unity 컴파일, 새 경고 0. 생성된 ysls 갱신(파라미터 이름 유지 확인).
