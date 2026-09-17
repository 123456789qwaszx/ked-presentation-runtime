# 이 폴더는 복사 반입본이다

`Ked.Progression`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소의
`Assets/Progression/Runtime/`을 그대로 옮겨 놓은 한 벌이다.

```
원본   ked-progression-runtime   (형제 폴더 · Unity 프로젝트 · 진행 코어와 런타임의 집)
반입   Assets/Scripts/Ked.Progression/   ← 이 폴더
```

`Assets/Scripts/Ked.Presentation.Core`가 저작 도구(VnTool)에 `src/Ked.Presentation.Core`로
복사 반입돼 있는 것과 같은 방식이다. **다만 방향이 반대다** — 연출 코어는 여기가 원본이고,
진행 코어는 여기가 사본이다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-progression-runtime` · `dev` @ `581233e` |
| 마지막 동기화 | 2026-09-17 · **원본 → 사본** |
| 가져온 것 | 코어(Spec/State/Transition/Vocabulary/Loading) + Scene · Contracts · Response · ProgressionDriver + EditMode 테스트 |
| 코드 일치 | `diff -r` 0건 |

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

> ⚠ 이전 원본이던 `ked-progression`(순수 C# 저장소)은 **더 이상 쓰지 않는다.**
> 그쪽으로 `diff`를 돌리면 `ChapterProgression` / `ChapterDefinition` 개명 때문에 전부 갈린 것처럼 보인다.

## 왜 UPM 패키지가 아닌가

`0.2.0` 태그를 git URL로 물리는 방식으로 한 번 세웠고 실제로 섰다
(`feat/progression-driver` 브랜치 `5b3aa2c8`). 그런데 진행 코어는 지금 **매일 바뀌는 중**이라,
한 줄 고칠 때마다 커밋 → 태그 → 푸시 → 유니티 재해결이 붙는다.
그 왕복이 지금 작업의 가장 큰 비용이다.

복사 반입은 그 왕복을 0으로 만든다. 대신 **갈림을 사람이 지킨다** — 아래 대조를 쓴다.

## 대조와 반입

원본과 갈렸는지 본다(양쪽 모두 `Library`·`obj`가 섞이지 않는 경로만 본다):

```bash
diff -r --strip-trailing-cr \
  ../ked-progression-runtime/Assets/Progression/Runtime \
  Assets/Scripts/Ked.Progression
```

`Reachability/`(사본에만 있음)와 `Tests/`(양쪽 구성이 다름)는 차이로 나온다. 그 둘은 아래를 본다.

원본 → 사본으로 다시 받는다:

```bash
rm -rf Assets/Scripts/Ked.Progression/{Contracts,Loading,Response,Scene,Spec,State,Transition,Vocabulary}
cp -r ../ked-progression-runtime/Assets/Progression/Runtime/. Assets/Scripts/Ked.Progression/
```

asmdef는 양쪽 이름과 설정이 같으므로(`Ked.Progression` · `noEngineReferences: true`) 덮어써도 안전하다.

⚠ **테스트는 위 명령에 딸려 오지 않는다.** 원본에서 테스트는 `Runtime/` 밖(`Assets/Progression/Tests/`)에 있다.
Scene/Runtime 층의 characterization은 그 세 파일이 전부이므로 함께 받아야 한다.

```bash
cp ../ked-progression-runtime/Assets/Progression/Tests/EditMode/{SceneProgressTests,SceneRunnerTests,ScenePendingHistoryTests}.cs* \
   Assets/Scripts/Ked.Progression/Tests/EditMode/
```

`Tests/EditMode/`의 나머지(`AutoEdgeTests` · `ProgressionStateFoldTests` · `SceneBoundaryTests`)는
**사본에만 있다.** 덮어쓰지 않는다.

여기서 고친 것은 **원본 저장소에도 같은 손을 대야 한다.**

## 자동 검증

이 층은 `noEngineReferences: true`인 순수 C#이고 EditMode 테스트도 순수 NUnit이라,
유니티 없이 그대로 돌아간다.

```bash
dotnet test tests/ProgressionCore/ProgressionCore.csproj
```

`.github/workflows/progression-core.yml`이 이것을 CI 게이트로 세운다.
따라서 "유니티 쪽엔 이 층의 테스트가 0"이던 시절의 서술은 더 이상 유효하지 않다.

## 안 가져온 것

| | 왜 |
|---|---|
| `Ked.Progression.asmdef` | 사본이 자기 것을 갖는다. 이름·설정이 같으므로 실질 차이는 없다 |
| `Debug/` (원본의 Progression Debug Host) | 원본의 uGUI 프레임워크(`UIManager`·`UIBase`·`UIRoot`)가 이 저장소의 동명 타입과 충돌한다. 통로 검증은 여기서 실제 Presentation으로 한다 |
| `Tests/PlayMode/` | `UnityEngine.TestTools`에 매인 smoke 1건. 여기서는 어셈블리 이름만 확인하는 내용이라 뜻이 없다 |

`Reachability/`는 **사본에만 있다.** 런타임 참조가 0건이고 저작 도구의 도달성 증명 쪽 자산이다.
원본으로 옮길지 버릴지는 아직 정하지 않았다.

## `.meta` GUID — 항상 갈려 보이는 네 개

새로 반입한 것(`Contracts` · `Scene` · `Response`와 그 아래 파일들)은 `.meta`까지 원본에서 그대로 받아
GUID가 두 저장소에서 같다. 반면 **이전 원본(`ked-progression`) 시절부터 있던 폴더**는 사본이 자기 GUID를
계속 쓴다. 그래서 `diff -r`를 돌리면 아래 넷은 **언제나** 갈린 것으로 나온다.

```text
Ked.Progression.asmdef.meta
Spec.meta
State.meta
Vocabulary.meta
```

이것은 표류가 아니다. **`.cs`가 하나라도 갈리면 그때가 표류다.** 코드만 보려면 이렇게 본다.

```bash
diff -r -q --strip-trailing-cr -x "*.meta" \
  ../ked-progression-runtime/Assets/Progression/Runtime \
  Assets/Scripts/Ked.Progression | grep -v "^Only in"
```

2026-09-17 기준 이 명령의 출력은 **0줄**이다.
