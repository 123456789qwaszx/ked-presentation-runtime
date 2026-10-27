# 이 폴더는 복사 반입본이다 — Playback만 빼고

`Assets/Ked.Presentation/`의 **Playback을 뺀 전부**(Core · Runtime · Editor · Audio · Data · Prefabs · Resources · Shaders)와
루트의 `ExportedTuning/`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소와 같은 내용을 담은 한 벌이다(.meta GUID까지).

```
원본   ked-stage-runtime        (형제 폴더 · Unity 프로젝트 · 무대의 집)
반입   Assets/Ked.Presentation/{Core, Runtime, Editor, Audio, Data, Prefabs, Resources, Shaders} · ExportedTuning/   ← 이 저장소
원본   Assets/Ked.Presentation/Playback/   ← 재생은 여기가 원본이다. 무대 저장소에는 없다
```

실행기(`Assets/Ked.CommandExecutor/`)는 또 다른 원본(ked-command-executor)의 반입본이다 — 그쪽 `Documentation~/vendoring.md`.
진행(`Assets/Ked.Progression/`)과 같은 방식이다. 세 저장소 구도는 [`Runtime/ASSEMBLY-BOUNDARY.md`](../Runtime/ASSEMBLY-BOUNDARY.md) 머리말.

사본에서 먼저 고치는 일은 생긴다. 그때는 **원본에도 같은 손을 대서** 두 벌을 다시 맞춘다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-stage-runtime` · `dev` @ `1302c16` |
| 마지막 동기화 | 2026-10-27 · 사본 → 원본 (첫 분리 — 이 저장소 `refactor/stage-seam`에서 떼어 냈다) |
| 맞춘 것 | 위 폴더 전부 + `ExportedTuning/` |
| 코드 일치 | `diff -r` 0건 (아래 "대조" 명령) |

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

## 대조

```
diff -r ../ked-stage-runtime/Assets/Ked.Presentation Assets/Ked.Presentation --exclude=Playback --exclude=Playback.meta --exclude=Documentation~
diff -r ../ked-stage-runtime/ExportedTuning ExportedTuning
```
