# 이 폴더는 복사 반입본이다

`Ked.Progression`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소의
`Assets/Ked.Progression/`과 같은 배치, 같은 바이트의 한 벌이다(`.meta` 포함).

```
원본   ked-progression-runtime   (형제 폴더 · Unity 프로젝트 · 진행 코어의 집 + 최소 샘플)
반입   Assets/Ked.Progression/   ← 이 폴더 (원본과 같은 자리)
         Core/            Ked.Progression       (장면 실행은 Core/SceneRun/)
         Save/            Ked.Progression.Save
         Tests/EditMode/  Ked.Progression.Tests.EditMode
원본에만  Samples/ · Tests/PlayMode/
저장소마다 자기 것  README.md · Documentation~/
```

`Assets/Ked.Presentation/Core`가 저작 도구(VnTool)에 `src/Ked.Presentation.Core`로
복사 반입돼 있는 것과 같은 방식이다. 실행기(`Assets/Ked.CommandExecutor/`)와 무대(`Assets/Ked.Presentation/`의
Playback을 뺀 전부)는 반대다 — 이 저장소가 원본이고 ked-command-executor · ked-stage-runtime이 포트폴리오용 분리본이다
(각 폴더의 `Documentation~/vendoring.md`).

사본에서 먼저 고치는 일은 생긴다. 그때는 **원본에도 같은 손을 대서** 두 벌을 다시 맞춘다.
2026-10-26의 동기화가 그 경우다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-progression-runtime` · `dev` @ `7cc2703` 위의 2026-11-07 배치 맞춤 (아래 줄). 코어 코드는 `99677cb`, 저장은 `fb5c4cb`에서 맞춤 |
| 마지막 동기화 | 2026-11-07 · **사본 → 원본** (역반입) |
| 맞춘 것 | `Runtime/` 전체(Spec · State · Transition · Vocabulary · Loading · Scene · Contracts · Response · `ProgressionDriver` · `ISceneRunner`) + `Save/`(`Ked.Progression.Save`) + EditMode 테스트 |
| 일치 | `.meta` 포함 `diff -r` 0건 (아래 "대조" 명령) |
| 2026-11-07 | **사본 → 원본, 배치 맞춤.** 원본도 `Assets/Progression/` → `Assets/Ked.Progression/`, `Runtime/` → `Core/`(`.meta` 그대로), `Runtime/Save/` → `Save/`, `Runtime/Scene/` → `Core/SceneRun/`. 주석 세 곳을 사본대로 고치고, 두 저장소가 각자 GUID를 쓰던 `.meta` 다섯(`Ked.Progression.asmdef` · `Spec` · `State` · `Vocabulary` · 테스트 asmdef)과 `Tests` · `Tests/EditMode` · 뿌리 폴더 `.meta`도 사본 것으로 맞췄다 — 원본 asmdef 참조는 모두 이름이라 GUID가 바뀌어도 끊기지 않는다. 원본 `tests/ProgressionSave`의 컴파일 경로와 README 구조도도 함께 |
| 2026-11-06 | **사본 배치를 어셈블리 단위로.** 뿌리에 펼쳐 두던 코드를 `Core/`(코어 어셈블리)와 `Save/`(저장 어셈블리, 원본에선 `Runtime/Save/`)로 나눴다(.meta 그대로, `Core.meta`는 원본 `Runtime.meta`). 모든 Ked 모듈이 같은 규칙(폴더 = 어셈블리)을 따르게 한 것이다. 같은 날 `Core/Scene/`은 `Core/SceneRun/`으로 이름을 바꿨다(Unity의 `Scenes`와 헷갈려서). 원본은 11-07에 같은 배치로 따라왔다 |
| 2026-11-04 | **사본 → 원본, `Save/` 배치.** 사본에서 먼저 `Coordinator/` · `Fork/` · `Maintenance/` · `Slots/`를 걷고 다섯 파일(`SaveCoordinator` · `PlaythroughSaveSnapshot` · `SaveSlotService` · `PlaythroughForkService` · `LocalSaveMaintenance`)을 `Save/` 바로 아래로 옮겼고, 원본도 같게 옮겼다(.meta 그대로). 원본 `tests/ProgressionSave` 25/25 |
| 원본 쪽 정리 | 옛 `SceneRunner`·`SceneRunContext`·`ProgressionState`·디버그 호스트 삭제, `Assets/Progression/Samples/` 최소 샘플 추가, README 갱신 |
| 그 뒤 원본에만 | 결정 E3 테스트(`tests/ProgressionSave`), 샘플의 저장·이어하기·대사 기록 갈라지기, README — **코드(`Runtime/`)는 그대로**라 사본에 가져올 것이 없다 |
| 같은 날 두 번째 | 이 저장소의 `Assets/Scripts/Save/`에서 **진행 겹을 떼어** `Save/`로 반입본에 넣고 원본에 보냈다. 연출 겹(줄 좌표·Yarn 선택·백로그)은 연출 어셈블리 `Ked.Presentation.Playback`의 `PlaybackMemory`가 `HostState` 문자열로 꺼내고 되돌린다(진행을 모른다) |

원본 `sync/from-presentation`(`8857ead`)은 `dev`에 합쳐졌다.

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

## 원본이 지켜 주는 것 — 결정 E3

이 저장소는 재생 단위의 루트를 **하나만** 둔다. 이전 장면의 루트를 보관해 그 장면에서 다시 여는 일
(백로그 줄에서 갈라지기)은 진행 저장의 몫이다 — `PlaythroughSaveSnapshot.FindSceneIndexBySerial`,
`PlaythroughForkService.ForkFromScene`, 장면 기록의 `HostSerialStart/End`·`HostState`.

원본만 보면 이것들은 "백로그도 없는데 왜 있지" 싶은 일반화로 읽힌다. 지워지면 이 저장소의 갈라지기가 사라진다.
그래서 원본은 세 겹으로 지킨다 — 이름에 결정을 적은 테스트(`tests/ProgressionSave`), README §11의 결정 기록,
그리고 그것을 실제로 쓰는 원본 샘플(대사 기록 줄을 누르면 그 장면에서 갈라진다).
원본에서 이 셋을 건드리는 변경이 오면 이 저장소의 `ManualSaveFlow.ForkFromBacklogAsync`와 함께 본다.

> 이전 원본이던 `ked-progression`(순수 C# 저장소)은 **더 이상 쓰지 않는다.**
> 그쪽으로 `diff`를 돌리면 `ChapterProgression` / `ChapterDefinition` 개명 때문에 전부 갈린 것처럼 보인다.

## 왜 UPM 패키지가 아닌가

`0.2.0` 태그를 git URL로 물리는 방식으로 한 번 세웠고 실제로 섰다
(`feat/progression-driver` 브랜치 `5b3aa2c8`). 그런데 진행 코어는 그때 **매일 바뀌는 중**이라,
한 줄 고칠 때마다 커밋 → 태그 → 푸시 → 유니티 재해결이 붙었다.

복사 반입은 그 왕복을 0으로 만든다. 대신 **갈림을 사람이 지킨다** — 아래 대조를 쓴다.

## 대조

원본과 갈렸는지 본다. 두 저장소의 배치가 같으므로 폴더 하나를 통째로 비교한다(`.meta` 포함).

```bash
diff -r -q --strip-trailing-cr \
  -x README.md -x README.md.meta -x "Documentation~" \
  -x Samples -x Samples.meta -x PlayMode -x PlayMode.meta \
  ../ked-progression-runtime/Assets/Ked.Progression Assets/Ked.Progression
```

2026-11-07 기준 출력은 **0줄**이다. `.meta`가 하나라도 갈리면 그것도 표류다.

## 반입 — 원본 → 사본

```bash
for d in Core Save Tests/EditMode; do
  rm -rf Assets/Ked.Progression/$d
  cp -r ../ked-progression-runtime/Assets/Ked.Progression/$d Assets/Ked.Progression/$d
done
cp ../ked-progression-runtime/Assets/Ked.Progression/{Core,Save}.meta Assets/Ked.Progression/
```

asmdef는 양쪽이 같다(`Ked.Progression` · `noEngineReferences: true`). 이 저장소의 asmdef들은
`Ked.Progression`을 GUID가 아니라 이름으로 참조한다.

`Save/`는 자기 asmdef(`Ked.Progression.Save`, Newtonsoft 참조, 엔진 참조 없음)를 가진 두 번째 어셈블리다.
코어 `Ked.Progression`은 Newtonsoft를 모른다.

## 역반입 — 사본 → 원본

이 폴더에서 먼저 고쳤다면 같은 내용을 원본으로 보낸다. 방향만 바꾼 같은 명령이다.

```bash
cd ../ked-progression-runtime
for d in Core Save Tests/EditMode; do
  rm -rf Assets/Ked.Progression/$d
  cp -r ../ked-presentation-runtime/Assets/Ked.Progression/$d Assets/Ked.Progression/$d
done
cp ../ked-presentation-runtime/Assets/Ked.Progression/{Core,Save}.meta Assets/Ked.Progression/
```

`README.md`·`Documentation~`는 보내지 않는다(저장소마다 자기 것). 원본 README의 구조 설명(§3 경계, §8 계약)이
바뀐 코드와 맞는지, 원본 `tests/ProgressionSave`가 통과하는지도 함께 본다.

## 테스트

| 테스트 | 원본 | 사본 | 비고 |
|---|---|---|---|
| `AutoEdgeTests` | ✓ | ✓ | 자동 간선 규칙 |
| `SceneBoundaryTests` | ✓ | ✓ | 장면 경계 · 착지점 규칙 |
| `SceneProgressTests` | ✓ | ✓ | pending · 롤백 · 리플레이 · 경로 복원 · commit |
| `ProgressionStateFoldTests` | ✓ | ✓ | 선택 적용 · 스탯 범위 |
| `ScenePendingHistoryTests` | ✓ | ✓ | 양쪽 모두 주석 처리 상태 |

호스트 `SceneRunner`의 테스트는 여기 없다. 대상이 코어가 아니라 이 저장소의 `ProgressionHost/`에 있기 때문이다.
그 테스트는 `tests/ProgressionHost/`의 dotnet 하네스가 맡는다(아래 "자동 검증").
예전 `SceneRunnerTests`(코어 안에 있던 옛 `SceneRunner` 대상, 주석 처리 상태)는 이 하네스로 옮기면서 지웠다.

## 자동 검증

이 층은 `noEngineReferences: true`인 순수 C#이고 EditMode 테스트도 순수 NUnit이라,
유니티 없이 그대로 돌아간다.

```bash
dotnet test tests/ProgressionCore/ProgressionCore.csproj
```

`.github/workflows/progression-core.yml`이 이것을 CI 게이트로 세운다.

저장 겹은 `tests/SaveLifecycle`(이 저장소 — 연출 기억의 입구·브리지·런처·연출 단독 회차)과 원본의 `tests/ProgressionSave`(저장 정책)가 나눠 본다.
`Save/`를 고치면 둘 다 돌린다.

코어를 바꾸면 호스트 쪽 Scene 실행 순서도 함께 돌려 본다. 진짜 `SceneRunner` · `PlaybackUnit` · `RollbackHistory` · `BacklogRecorder`와
코어 전체를 컴파일하고, Unity·Yarn에 매인 것만 대역으로 둔다.

```bash
dotnet run --project tests/ProgressionHost/ProgressionHost.csproj
```

## 안 가져온 것

| | 왜 |
|---|---|
| `Samples/` (원본의 최소 진행 샘플) | 코어만으로 도는 예제다. 이 저장소에는 실제 호스트(`ProgressionHost/`)가 있다 |
| `Tests/PlayMode/` | `UnityEngine.TestTools`에 매인 smoke 1건. 어셈블리 이름만 확인하는 내용이라 여기서는 뜻이 없다 |

`Reachability/`는 **2026-09-17에 걷었다.** 도달성 증명의 주인은 저작 도구 하나다 — 이유는
[SCOPE-BOUNDARY.md](../../../SCOPE-BOUNDARY.md) §3.3에 있다.
