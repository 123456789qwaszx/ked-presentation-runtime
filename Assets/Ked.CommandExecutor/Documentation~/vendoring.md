# 분리본 — ked-command-executor

이 저장소가 원본이다. 실행기(`Assets/Ked.CommandExecutor/`)를 포트폴리오에서 따로 보이려고 **ked-command-executor**에 떼어 두었다.
그쪽의 `Assets/Ked.CommandExecutor/`는 이것과 같은 한 벌이다(.meta GUID까지). 무대 분리본(ked-stage-runtime)에도 같은 한 벌이 들어 있다.

```
원본     Assets/Ked.CommandExecutor/   ← 이 폴더
분리본   ked-command-executor   (형제 폴더 · Unity 프로젝트 · 실행기만 + LinePlayback 예제)
         ked-stage-runtime/Assets/Ked.CommandExecutor/   (무대가 참조하는 한 벌)
```

고치는 곳은 여기다. 고친 뒤 분리본 둘을 이쪽에 맞춘다.

## 스냅샷

| | |
|---|---|
| 분리본 | `ked-command-executor` · `claude/fervent-thompson-1cqiis` @ `284dac9` (LinePlayback 예제를 지금 재생 계층으로 다시 옮긴 것도 같은 커밋) · 무대 분리본은 그쪽 `Assets/Ked.Presentation/Documentation~/vendoring.md`의 판과 같다 |
| 마지막으로 맞춘 날 | 2026-11-04 · 이 저장소 → 분리본 둘 (S2 스텝 세대 가드 · S3/E5 신호·시간·입력 포트와 시간 배율 삭제 · E6 Run 수명 삭제 · S6 첫 진입 try). 그 전: 2026-10-27 처음 떼어 냄(분리본 `main` @ `d12e098`) |
| 맞춘 것 | `Assets/Ked.CommandExecutor/` 전체 |
| 일치 | `.cs`·`.meta`·`.asmdef` 기준 `diff -r` 0건 (아래 "대조" 명령) |

**갈리면 이 표를 고친다.** 표가 낡으면 분리본이 어느 판인지 아무도 모른다.

## 대조

```
diff -r ../ked-command-executor/Assets/Ked.CommandExecutor Assets/Ked.CommandExecutor --exclude=Documentation~
diff -r ../ked-stage-runtime/Assets/Ked.CommandExecutor Assets/Ked.CommandExecutor --exclude=Documentation~
```
