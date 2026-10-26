# 이 폴더는 복사 반입본이다

`Ked.Progression`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소의
`Assets/Progression/Runtime/`과 같은 내용을 담은 한 벌이다.

```
원본   ked-progression-runtime   (형제 폴더 · Unity 프로젝트 · 진행 코어의 집 + 최소 샘플)
반입   Assets/Scripts/Ked.Progression/   ← 이 폴더
```

`Assets/Scripts/Ked.Presentation.Core`가 저작 도구(VnTool)에 `src/Ked.Presentation.Core`로
복사 반입돼 있는 것과 같은 방식이다. **다만 방향이 반대다** — 연출 코어는 여기가 원본이고,
진행 코어는 여기가 사본이다.

사본에서 먼저 고치는 일은 생긴다. 그때는 **원본에도 같은 손을 대서** 두 벌을 다시 맞춘다.
2026-10-26의 동기화가 그 경우다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-progression-runtime` · `sync/from-presentation` @ `fb5c4cb` (코어 코드는 `99677cb`, 저장은 `fb5c4cb`에서 맞춤) |
| 마지막 동기화 | 2026-10-26 · **사본 → 원본** (역반입) |
| 맞춘 것 | `Runtime/` 전체(Spec · State · Transition · Vocabulary · Loading · Scene · Contracts · Response · `ProgressionDriver` · `ISceneRunner`) + `Save/`(`Ked.Progression.Save`) + EditMode 테스트 |
| 코드 일치 | `.cs` 기준 `diff -r` 0건 (아래 "대조" 명령) |
| 원본 쪽 정리 | 옛 `SceneRunner`·`SceneRunContext`·`ProgressionState`·디버그 호스트 삭제, `Assets/Progression/Samples/` 최소 샘플 추가, README 갱신 |
| 같은 날 두 번째 | 이 저장소의 `Assets/Scripts/Save/`에서 **진행 겹을 떼어** `Save/`로 반입본에 넣고 원본에 보냈다. 연출 겹(줄 좌표·Yarn 선택·백로그)은 연출 어셈블리 `Ked.Presentation.Playback`의 `PresentationMemory`가 `HostState` 문자열로 만든다(진행을 모른다) |

원본 브랜치 `sync/from-presentation`은 아직 원본 `dev`에 합치지 않았다. 합치면 이 표의 브랜치를 고친다.

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

> ⚠ 이전 원본이던 `ked-progression`(순수 C# 저장소)은 **더 이상 쓰지 않는다.**
> 그쪽으로 `diff`를 돌리면 `ChapterProgression` / `ChapterDefinition` 개명 때문에 전부 갈린 것처럼 보인다.

## 왜 UPM 패키지가 아닌가

`0.2.0` 태그를 git URL로 물리는 방식으로 한 번 세웠고 실제로 섰다
(`feat/progression-driver` 브랜치 `5b3aa2c8`). 그런데 진행 코어는 그때 **매일 바뀌는 중**이라,
한 줄 고칠 때마다 커밋 → 태그 → 푸시 → 유니티 재해결이 붙었다.

복사 반입은 그 왕복을 0으로 만든다. 대신 **갈림을 사람이 지킨다** — 아래 대조를 쓴다.

## 대조

원본과 갈렸는지 본다(양쪽 모두 `Library`·`obj`가 섞이지 않는 경로만 본다). 코드만 보려면:

```bash
diff -r -q --strip-trailing-cr -x "*.meta" \
  ../ked-progression-runtime/Assets/Progression/Runtime \
  Assets/Scripts/Ked.Progression | grep -v "^Only in"
```

2026-10-26 기준 이 명령의 출력은 **0줄**이다. `Only in`으로 나오는 `Documentation~`(이 문서)와 `Tests`는 사본에만 있는 자리다.

테스트는 따로 본다:

```bash
diff -r -q --strip-trailing-cr -x "*.meta" \
  ../ked-progression-runtime/Assets/Progression/Tests/EditMode \
  Assets/Scripts/Ked.Progression/Tests/EditMode
```

2026-10-26 기준 이 명령의 출력도 **0줄**이다.

## 반입 — 원본 → 사본

```bash
rm -rf Assets/Scripts/Ked.Progression/{Contracts,Loading,Response,Save,Scene,Spec,State,Transition,Vocabulary}
cp -r ../ked-progression-runtime/Assets/Progression/Runtime/. Assets/Scripts/Ked.Progression/
cp ../ked-progression-runtime/Assets/Progression/Tests/EditMode/*.cs* \
   Assets/Scripts/Ked.Progression/Tests/EditMode/

# 사본이 자기 GUID를 쓰는 .meta 넷은 되돌린다 (아래 ".meta GUID" 참고)
git checkout -- Assets/Scripts/Ked.Progression/{Ked.Progression.asmdef,Spec,State,Vocabulary}.meta
```

asmdef는 양쪽 이름과 설정이 같다(`Ked.Progression` · `noEngineReferences: true`). 이 저장소의 asmdef들은
`Ked.Progression`을 GUID가 아니라 이름으로 참조하므로 asmdef 파일을 덮어써도 참조는 끊기지 않는다.

`Save/`는 자기 asmdef(`Ked.Progression.Save`, Newtonsoft 참조, 엔진 참조 없음)를 가진 두 번째 어셈블리다.
코어 `Ked.Progression`은 Newtonsoft를 모른다. `Save/`의 `.meta`는 두 저장소에서 GUID가 같다.

## 역반입 — 사본 → 원본

이 폴더에서 먼저 고쳤다면 같은 내용을 원본으로 보낸다. 방향만 바꾼 같은 명령이다.

```bash
cd ../ked-progression-runtime
rm -rf Assets/Progression/Runtime/{Contracts,Loading,Response,Save,Scene,Spec,State,Transition,Vocabulary}
cp -r ../ked-presentation-runtime/Assets/Scripts/Ked.Progression/{Contracts,Loading,Response,Save,Scene,Spec,State,Transition,Vocabulary} \
      Assets/Progression/Runtime/
cp ../ked-presentation-runtime/Assets/Scripts/Ked.Progression/*.cs* Assets/Progression/Runtime/
```

`Documentation~`·`Tests`·asmdef는 보내지 않는다. 테스트는 아래 표대로 고른다.
원본 README의 구조 설명(§3 경계, §8 계약)이 바뀐 코드와 맞는지도 함께 본다.

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

저장 겹은 `tests/SaveLifecycle`(이 저장소, 연출 코덱·브리지·런처)과 원본의 `tests/ProgressionSave`(저장 정책)가 나눠 본다.
`Save/`를 고치면 둘 다 돌린다.

코어를 바꾸면 호스트 쪽 Scene 실행 순서도 함께 돌려 본다. 진짜 `SceneRunner` · `RollbackHistory` · `BacklogRecorder`와
코어 전체를 컴파일하고, Unity·Yarn에 매인 것만 대역으로 둔다.

```bash
dotnet run --project tests/ProgressionHost/ProgressionHost.csproj
```

## 안 가져온 것

| | 왜 |
|---|---|
| `Ked.Progression.asmdef` | 사본이 자기 것을 갖는다. 이름·설정이 같으므로 실질 차이는 없다 |
| `Samples/` (원본의 최소 진행 샘플) | 코어만으로 도는 예제다. 이 저장소에는 실제 호스트(`ProgressionHost/`)가 있다 |
| `Tests/PlayMode/` | `UnityEngine.TestTools`에 매인 smoke 1건. 어셈블리 이름만 확인하는 내용이라 여기서는 뜻이 없다 |

`Reachability/`는 **2026-09-17에 걷었다.** 도달성 증명의 주인은 저작 도구 하나다 — 이유는
[SCOPE-BOUNDARY.md](../../../../SCOPE-BOUNDARY.md) §3.3에 있다.

## `.meta` GUID — 항상 갈려 보이는 네 개

대부분의 `.meta`는 두 저장소에서 GUID가 같다.
반면 **이전 원본(`ked-progression`) 시절부터 있던 넷**은 두 저장소가 각자 GUID를 쓴다.
그래서 `.meta`를 포함해 `diff -r`를 돌리면 아래 넷은 **언제나** 갈린 것으로 나온다.

```text
Ked.Progression.asmdef.meta
Spec.meta
State.meta
Vocabulary.meta
```

이것은 표류가 아니다. **`.cs`가 하나라도 갈리면 그때가 표류다.** 테스트 asmdef의 `.meta`도 같은 이유로 갈려 보인다.
