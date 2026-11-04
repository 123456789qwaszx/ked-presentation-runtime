# 분리본 — ked-stage-runtime

이 저장소가 원본이다. `Assets/Ked.Presentation/`의 **Playback을 뺀 전부**(Core · Runtime · Editor · Audio · Data · Prefabs · Resources)와
루트의 `ExportedTuning/`을 포트폴리오에서 따로 보이려고 **ked-stage-runtime**에 떼어 두었다. 그쪽은 이것과 같은 한 벌이다(.meta GUID까지).

```
원본     Assets/Ked.Presentation/{Core, Runtime, Editor, Audio, Data, Prefabs, Resources} · ExportedTuning/   ← 이 저장소
분리본   ked-stage-runtime   (형제 폴더 · Unity 프로젝트) — Playback과 예제는 없다
```

실행기(`Assets/Ked.CommandExecutor/`)도 같은 방식으로 ked-command-executor에 떼어 두었다 — 그 폴더의 `Documentation~/vendoring.md`.
진행(`Assets/Ked.Progression/`)은 다르다 — 그 폴더의 `Documentation~/vendoring.md`.

고치는 곳은 여기다. 고친 뒤 분리본을 이쪽에 맞춘다.

## 스냅샷

| | |
|---|---|
| 분리본 | `ked-stage-runtime` · `refactor/canonical-commands` @ `3ab6b2e` (분리본 `dev`에는 아직 합치지 않았다) |
| 마지막으로 맞춘 날 | 2026-11-04 · 이 저장소 → 분리본 (11-03~04에 여기서 고친 것 전부 — face_swap · 샷 의도 · 화면 효과 의도 · 블러 의도 · BGM · 보이스 · 무대 마스크 · S2~S6 · `Shaders/`를 `Resources/VisualEffects/`로. 분리본 안의 `Ked.CommandExecutor`도 함께). 그 전: 2026-11-03 분리본 쪽에서 먼저 한 정본 커맨드 정리를 들여옴, 2026-10-29 배경 리그 제거, 2026-10-28 `dev` @ `640ac60`, 2026-10-27 처음 떼어 냄 |
| 맞춘 것 | 위 폴더 전부 + `ExportedTuning/` |
| 일치 | `diff -r` 0건 (아래 "대조" 명령) |

**갈리면 이 표를 고친다.** 표가 낡으면 분리본이 어느 판인지 아무도 모른다.

## 대조

```
diff -r ../ked-stage-runtime/Assets/Ked.Presentation Assets/Ked.Presentation --exclude=Playback --exclude=Playback.meta --exclude=vendoring.md
diff -r ../ked-stage-runtime/ExportedTuning ExportedTuning
```
