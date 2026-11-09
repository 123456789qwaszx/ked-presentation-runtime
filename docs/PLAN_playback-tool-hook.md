# 재생 도구 훅 — 조립이 끝난 재생에 개발 도구를 붙이는 자리 (2026-11-09)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/subSystem` @ `f93d2c8a` |
| 본보기 | 등가성 하네스(`StageEquivalenceHarness`) — 조립 루트가 안쪽 부품을 `Initialize`로 넘기고, 도구는 재생을 막지 않는다 |
| 왜 | 영상 재현 — 비공개 작업공간의 녹화기가 원본 영상의 넘김 시각대로 줄을 넘기며 화면을 찍는다. 녹화기 · 시각표는 원본 영상과 대조하는 작업공간의 일이라 이 저장소에 두지 않는다 |
| 결정 | 아미야(소유자) 2026-11-09: 런타임에는 조립이 끝난 뒤 재생 부품을 내주는 일반 훅 하나만 둔다. 녹화기 · 시각표 · 프레임 저장은 작업공간에 둔다 |

## 목표 한 문장

> 조립 루트가 조립을 마치면, 인스펙터에 꽂힌 개발 도구마다 재생 안쪽 부품 묶음을 한 번 넘긴다.

## 모양

| 파일 | 하는 일 |
|---|---|
| `Playback/Host/IPlaybackToolHook.cs` | `void OnPlaybackAssembled(PlaybackToolServices services)` — 도구의 계약. 첫 줄이 서기 전에 한 번 불린다 |
| `Playback/Host/PlaybackToolServices.cs` | 읽기 전용 묶음: `DialogueRunner` · `AdvanceDispatcher`(넘김) · `LineFlow`(`LineDisplayed` = 글자가 다 나온 순간) · `RollbackHistory`(`Points[^1].lineId` = 지금 줄 ID) |
| `SampleBootstrap.cs` | 칸 `playbackTools`(MonoBehaviour 목록, `[Header("재생 도구")]`) |
| `SampleBootstrap.Presentation.cs` | `AssemblePresentation`에서 하네스 다음에 `BootstrapPlaybackTools()` — 목록의 도구마다 부른다. 인터페이스가 아니거나 빈 칸이면 오류를 낸다 |

## 본보기 대조

| | 등가성 하네스 | 재생 도구 훅 |
|---|---|---|
| 붙는 자리 | `enableEquivalenceHarness` → `BootstrapEquivalenceHarness()`가 만들어 `Initialize` | `playbackTools` → `BootstrapPlaybackTools()`가 `OnPlaybackAssembled` |
| 받는 것 | 안쪽 부품 아홉 개를 인자로 | 묶음 하나(지금 넷) |
| 실패 | 못 서면 이유를 남기고 빠진다(하네스 안의 try/catch) | 같은 규약 — 구현이 지킨다. 부르는 쪽은 감싸지 않는다 |
| 누가 만드나 | 조립 루트가 `AddComponent` | 씬에 놓인 도구를 칸에 꽂는다 — 도구가 다른 저장소(작업공간)에 살 수 있다 |

`PresentationRuntime`에 얹지 않은 이유: 그 클래스는 롤백 기록 같은 안쪽 부품을 호스트에 내놓지 않는다(클래스 주석). 개발 도구만 조립 루트에서 받는다.

## 하지 않은 것 · 남은 것

- **하네스를 이 훅으로 옮기기** — 하지 않았다. 옮기면 예제 씬의 `enableEquivalenceHarness` 칸이 바뀐다. 옮길 때 묶음에 하네스가 받는 나머지(session · stage · shot · stageRigs · motions · 대본 폴더)를 더한다.
- **녹화기** — 작업공간(ked-vn-workspace `runtime/Assets/Workspace/Recording/`)에 둔다. 이 저장소는 그 존재를 모른다.

## Unity 확인 목록 (아미야)

1. 컴파일 오류 0. 새 파일 둘의 `.meta`는 커밋에 들어 있다.
2. 예제 씬(`PresentationSample`)을 그대로 재생 — `playbackTools`가 비어 있으니 전과 같다.
3. `[VNAppBootstrap]` 인스펙터에 "재생 도구" 칸이 보인다.
