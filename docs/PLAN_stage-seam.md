# 무대 이음매 — 실행기 · 무대 · 재생을 가르는 선

기준: `dev` @ `f79f13d` (fix(tests): 하네스 둘이 끝 화면을 받는 생성자로 다시 빌드된다)
작업: `refactor/stage-seam`

**현재 위치(2026-10-27):** ①②③④ 끝, 세 저장소로 갈랐다(§4 — Unity 확인은 셋 다 남음). 다음은 모션(§5)을 무대 저장소에서.

## 0. 왜

이 저장소에는 관심사가 셋 섞여 있다.

| 조각 | 무엇 | 지금 자리 |
|---|---|---|
| **실행기** | 커맨드 하나의 수명 — `CommandBase` · `ClaimTweenCommandBase` · `CommandRunScope` · `LifetimeScope` · 포트(입력·시간·신호·정책) | `Runtime/PresentationCore/` 전부 (④ 뒤로 실행기만 남았다) |
| **무대** | 리그 계층 · 철도(shot·depth 층) · 커맨드(스펙·실행 어댑터) · 대본 문법의 뜻(파서) · 정지 프레임(리듀서·장부·클레임) · 이징·커브 | `Core/` 전부 + `Runtime/{Commands, CharacterRig, ShotResponse, BackgroundRig, ScreenEffect, StageMask, Audio, CoreBridge, Equivalence}` + `Runtime/Stage/` |
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

## 4. 리포지토리 분리 — 끝 (2026-10-27)

패키지(UPM)는 만들지 않는다. `Ked.Progression`과 같은 **복사 반입**이다([vendoring.md](../Assets/Ked.Progression/Documentation~/vendoring.md)).

| 저장소 | 내용 | 원본/사본 |
|---|---|---|
| `ked-command-executor` | 실행기 — `CommandBase` · `ClaimTweenCommandBase` · `CommandRunScope` · `LifetimeScope` · `CommandExecutor` · `SequencePlayer` · 포트 | 원본. 무대·재생 둘 다 반입 |
| `ked-stage-runtime` | 무대 — `Core/` 전부 · 리그·철도·커맨드·파서 · 무대 계약(`IShotResponseStageProvider` 등) · 등가성 캡처 | 원본. 재생이 반입 |
| `ked-presentation-runtime` | 재생 — 지금 저장소. Playback · 예제 · 철도 구현(`PresentationUIRoot`) | 실행기·무대·진행을 반입 |

### ④ 스코프에서 무대를 뺀다 — 끝

무대가 필요한 커맨드 35개가 `PresentationStage`를 **생성자 첫 인자**로 받고, 팩토리 다섯(`CharacterRigCommandFactory` ·
`BackgroundRigCommandFactory` · `ShotResponseCommandFactory` · `PresentationControlCommandFactory` · `ScreenEffectCommandFactory`)이
무대를 들고 넘긴다. `CommandRunScope`는 `ICommandPacePolicy` 하나만 받는다. 등가성 하네스는 무대를 `Initialize`로 받는다.

같이 옮긴 것: `PresentationStage` · `PresentationStageKey` · `IStageDepthContentSlotProvider` → `Runtime/Stage/`,
토큰 파서(`Parser/`) → `Runtime/Commands/Parser/`. 이제 `Runtime/PresentationCore/`는 실행기뿐이고 밖(무대·재생)으로 나가는 참조가 0이다 —
가를 때 폴더째 든다.

`partial class`는 어셈블리를 못 넘는다(§5, ASSEMBLY-BOUNDARY). 철도 계약은 무대에, `PresentationUIRoot`의 partial 구현은 재생에 남는다.

### 한 것

- [ked-command-executor](https://github.com/123456789qwaszx/ked-command-executor) `main` @ `d12e098` — `Runtime/PresentationCore/` → `Assets/Ked.CommandExecutor/`(어셈블리 `Ked.CommandExecutor`, 참조 DOTween.Modules). 뼈대는 이 저장소의 ProjectSettings·Packages(Yarn 제외)·DOTween·URP 설정.
- [ked-stage-runtime](https://github.com/123456789qwaszx/ked-stage-runtime) `main` @ `1f40501` — `Assets/Ked.Presentation/`(Playback 제외) + `ExportedTuning/` + 실행기 반입본. asmdef: Runtime·Editor·Core.Tests가 `Ked.CommandExecutor`를 참조, 쓰지 않던 `Unity.TextMeshPro` 참조는 걷음. 무대 에셋이 재생 저장소 파일을 가리키는 GUID 0건.
- 이 저장소: `PresentationCore/`를 `Assets/Ked.CommandExecutor/`로 옮기고(GUID 그대로), Playback asmdef가 실행기를 참조. 공유 파일(asmdef 셋·ASSEMBLY-BOUNDARY.md)은 무대 원본의 것. `diff -r` 0건 — 각 `Documentation~/vendoring.md`의 대조 명령.
- 세 저장소 모두 `main`·`dev`. 어셈블리·네임스페이스 이름(`Ked.Presentation.*`)은 그대로다 — VnTool 반입과 스크립트 GUID가 매달려 있어서, 바꿀 날이 오면 셋을 한 번에.

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

### 한 것 (2026-10-27, ked-stage-runtime `dev`)

모션 툴은 **Unity Animation 창**이다(A안). 리그 프리팹의 노드는 트랜스폼 계층이라 그대로 키잉되고, 키는 Hermite라 `CurveKey`와 1:1이다.
그래서 툴은 내보내기 하나다 — 런타임은 `AnimationClip`을 모른다. VnTool은 모션 **이름**만 쓴다(`<<motion c1 nod>>`).

| 단계 | 무엇 | 어디 |
|---|---|---|
| 1 Core | `MotionClip`(채널 = 노드·종류·mode·X/Y/Z 키, 초 단위) · `MotionMath`(Sample · Compose · EndValue · ClaimFor) · `MotionRules`(이름·길이·키·Track_Focus 금지·offset 첫값 0·스키마 노드) | `Core/Motion/` |
| 2 Runtime | `MotionLibrary`(motions/1 로더, 호스트가 본문을 준다) · `MotionCommandCharR`(노드마다 트윈 하나, 마지막 키 = 클레임, 장부 게시) · 리듀서 `motion`(샘플링 없이 마지막 키) · 파서 표 `motion` | `Runtime/Commands/` |
| 3 Editor | `MotionExportSettings`(클립 → 채널별 mode, 기본값 없음) · `MotionClipExporter`(Animation 창의 anchoredPosition·localScale·localEulerAnglesRaw만, 거부 사유 전부 모음, 되읽기 대조, 메뉴 Ked/Motion) | `Editor/` |
| 호스트 | `SampleBootstrap`의 '작가 자산' TextAsset(curves.json · motions.json) — 빌드에서도 읽힌다. 안 꽂으면 에디터에서만 옛 경로를 읽고 소리 낸다. 하네스 튜닝에 `Motions` | 이 저장소 |

테스트: 코어 17 · 로더 5 · 리듀서 3 · 파서 1 · Unity 대조 4(`AnimationClip.SampleAnimation` ↔ `MotionMath.Compose`).

리뷰(2026-10-27) 뒤 다듬은 것: `offset` → `relative`(배율은 곱셈) · `end: restore|commit`(모션 단위, 필수) · 제작 원본은 `MotionDefinition` ·
내보내기는 `rig-schemas.json`으로 노드와 **계층 전체**를 대조(Core + 데이터 의존) · 제작 리그는 `Ked/Motion/Create Authoring Rig`가 스키마로 세운다 ·
relative의 전 구간 중립값 성분은 싣지 않는다. 규약은 무대 저장소 `docs/motion-contract.md`.

**확인(2026-10-28, Unity).** 견본 둘 — `nod`(restore, Track_Move_Y 0→-18→0, 0.6s) · `bow`(commit·absolute, SwayPivot z 0→8, 0.4s).
연출 데모 S3에서 `<<motion s1 nod>>` · `<<motion s1 bow>>` → `place_center`(숙인 채로 이동, 장부) → `char_rotate_to 0`(같은 노드를 다른 명령이 이어받음) → `place_right`.
재생·스킵·롤백 모두 기대대로. 씬의 `SampleBootstrap › 작가 자산`에 `curves.json`·`motions.json`이 꽂혀 있다.

**motions/2 (2026-10-28, 소유자 결정 — 무대 반입본 @ `40d19d7`, 브랜치 `motion/motions-2`).** 모션은 값만 갖고 relative/absolute는
`<<motion slot 이름 [relative|absolute]>>`의 세 번째 인자(생략 relative)가 정한다. 채널 `mode`와 중립값 규칙(relative 첫 키 0 ·
restore 마지막 키 0 · restore는 relative만)이 규약에서 빠졌다. restore는 재생이 끝·스킵·스텝 경계에서 **시작값을 되써서** 지킨다.
이 저장소: Yarn 등록 `motion`이 인자 셋, 견본 `motions.json`은 motions/2, 데모 S3는 `<<motion s1 bow absolute>>`. 규약은 무대 저장소 `docs/motion-contract.md`.
⚠ 유니티에서 아직 돌려 보지 않았다 — 반입본 테스트(코어·로더·리듀서·파서·Unity 대조)를 한 번 돌릴 것.

남은 것: VnTool이 `motions.json`의 이름·길이·`end`를 읽어 `<<motion>>` 자동완성 · 2단계(카메라·의도 채널) · 전용 모션 툴 여부(`PLAN_motion-tool.md`, 무대 저장소).

## 6. 검증

- 등가성 하네스: 예제 장면에서 불일치 0 — ①②③ 전부 "값·순서·부작용 불변"을 주장하므로 이것이 오라클이다.
- EditMode: `StageCommandParserTests` 7개 + 기존 전부.
- Unity 컴파일, 새 경고 0. 생성된 ysls 갱신(파라미터 이름 유지 확인).
