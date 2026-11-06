# Ked.Progression — 진행 판정과 저장 (반입본)

챕터 → 장면 → 에피소드의 **진행 규칙**(어디로 갈 수 있나, 선택과 스탯이 무엇을 바꾸나)과 **진행 저장**을 맡는다.
연출을 모른다 — 연출 쪽 상태는 저장에 `HostState` 문자열 한 칸으로만 실린다.

> **이 폴더는 복사 반입본이다.** 원본은 형제 저장소 `ked-progression-runtime`이다.
> 여기만 고치지 않는다 — 고치면 원본에도 같은 손을 댄다. 방법은 `Documentation~/vendoring.md`.

## 어셈블리 — 폴더 하나에 어셈블리 하나

| 폴더 | 어셈블리 | 하는 일 | 참조 |
|---|---|---|---|
| `Core/` | `Ked.Progression` | 진행 판정. 챕터 정의와 불변식, 선택지·조건·스탯, 챕터 JSON 로더, 장면 단위 진행과 커밋(`SceneRun/`), 챕터 실행 경계(`ProgressionDriver`). 순수 C# | 없음 |
| `Save/` | `Ked.Progression.Save` | 진행 저장. 플레이스루(append-only, 갈라지기), 슬롯, 이어하기 지점, 로컬 파일 저장소. 순수 C# + Newtonsoft.Json | Core |
| `Tests/EditMode/` | `Ked.Progression.Tests.EditMode` | 판정 규칙 테스트 | Core |

## 의존 방향

```
예제(ProgressionHost) ──> Save ──> Core
```

## 검증

순수 C#이라 Unity 없이 돈다: `dotnet test tests/ProgressionCore/ProgressionCore.csproj`.
저장은 `tests/SaveLifecycle`, 호스트 쪽 장면 실행은 `tests/ProgressionHost`가 본다(CI 워크플로 셋).
