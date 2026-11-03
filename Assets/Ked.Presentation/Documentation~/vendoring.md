# 이 폴더는 복사 반입본이다 — Playback만 빼고

`Assets/Ked.Presentation/`의 **Playback을 뺀 전부**(Core · Runtime · Editor · Audio · Data · Prefabs · Resources)와
루트의 `ExportedTuning/`은 **다른 저장소가 원본**이다. 여기 있는 것은 그 저장소와 같은 내용을 담은 한 벌이다(.meta GUID까지).

```
원본   ked-stage-runtime        (형제 폴더 · Unity 프로젝트 · 무대의 집)
반입   Assets/Ked.Presentation/{Core, Runtime, Editor, Audio, Data, Prefabs, Resources} · ExportedTuning/   ← 이 저장소
원본   Assets/Ked.Presentation/Playback/   ← 재생은 여기가 원본이다. 무대 저장소에는 없다
```

실행기(`Assets/Ked.CommandExecutor/`)는 또 다른 원본(ked-command-executor)의 반입본이다 — 그쪽 `Documentation~/vendoring.md`.
진행(`Assets/Ked.Progression/`)과 같은 방식이다. 세 저장소 구도는 [`Runtime/ASSEMBLY-BOUNDARY.md`](../Runtime/ASSEMBLY-BOUNDARY.md) 머리말.

사본에서 먼저 고치는 일은 생긴다. 그때는 **원본에도 같은 손을 대서** 두 벌을 다시 맞춘다.

## 스냅샷

| | |
|---|---|
| 원본 | `ked-stage-runtime` · `refactor/canonical-commands` @ `316602e` (`dev` @ `7aeef7f` 위 2커밋 — 대본 커맨드 정본만, 프레임 대기 · char_visual_<효과>는 되살림) |
| 마지막 동기화 | 2026-11-03 · 원본 → 사본 (별칭 · `<<Nfr>>` · `ui_patch`를 걷고 정본만 남김. 함께 `dev`의 `ToolExportMotionParityTests`(M6)도 들어왔다 — 그 전에는 사본에 없었다). 그 전: 2026-10-29 배경 리그 제거, 2026-10-28 `dev` @ `640ac60`, 2026-10-27 첫 분리 |
| 맞춘 것 | 위 폴더 전부 + `ExportedTuning/` |
| 코드 일치 | `diff -r` 0건 (아래 "대조" 명령) |
| 사본에서 먼저 (2026-11-04) | **`Shaders/`를 걷었다.** 셰이더 그래프 넷과 `CharacterBlur.hlsl`은 짝 재질 곁 `Resources/VisualEffects/`로(.meta 그대로), `BLUR-DECISIONS.md`는 `Documentation~/`로. 쓰이지 않던 `M_UISeparableBlur.mat`(셰이더가 없는 블러 v2 잔재)은 지웠다. 같이 `ScreenEffectRigBuilder`가 프리팹 컨트롤러의 원본 재질을 먼저 쓰고 없을 때만 Resources에서 읽는다(`IScreenEffectController.SourceMaterial`). 원본도 같게 하면 이 줄을 지운다 |

**갈리면 이 표를 고친다.** 표가 낡으면 사본이 어디서 왔는지 아무도 모른다.

## 대조

```
diff -r ../ked-stage-runtime/Assets/Ked.Presentation Assets/Ked.Presentation --exclude=Playback --exclude=Playback.meta --exclude=Documentation~
diff -r ../ked-stage-runtime/ExportedTuning ExportedTuning
```
