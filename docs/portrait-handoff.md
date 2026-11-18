# 대사창 초상 — 런타임 밖에 반영할 것 (2026-11-18)

> 공개 런타임에 대사창 초상이 들어갔다. 설계 · 규칙은 [PLAN_portrait-capture.md](PLAN_portrait-capture.md).
> 이 문서는 소비처(작업공간과 그 런타임 포크 · 툴)가 **무엇을 언제 어떻게** 바꾸는지만 적는다.
> 앞서 채팅으로 보낸 안내(11-18 — `slot pov studio` · `#protagonist → #portrait`, 맨 위 `079da84`)는 낡았다. 이 문서가 대신한다.

## 0. 지금 들이지 않는다

| | |
|---|---|
| 들이는 때 | 공개 쪽에서 **속말(P4)** 이 선 뒤 한 번에. 소유자가 알린다 |
| 까닭 | 재현 대본의 속말이 83줄이다. 지금 들이면 `#thought`를 임시로 풀었다가 다시 내야 한다. 초상이 없어도 점수 · 카드에는 영향이 없다 |
| 그 전에 해도 되는 것 | 이 문서를 읽고 시험으로 합쳐 보는 것. 포크 · 대본은 바꾸지 않는다 |
| 하면 안 되는 것 | 대본만 먼저 바꾸는 것 — 포크에 `portrait` 명령이 없어 "모르는 명령"이 된다 |

## 1. 어디까지가 무엇인가

브랜치 `feat/portrait-capture`. `0faca84`(포크가 이미 들인 30fps)에서 갈라졌다 — 자동 간선 없이 이것만 들일 수 있다.

| 범위 | 내용 | 상태 |
|---|---|---|
| `0faca84..1a046a7` | 촬영소 · 카메라 · 초상 칸 · 프리셋 에셋. **`.meta`와 에셋이 들어 있다**(`1a046a7`) | `dev`에 있다. Unity 확인 끝 |
| `1a046a7..d33c2d5` | 화자 `나`, 명령 `portrait` · `portrait_close`, 슬롯만 묶는 캐스팅 | Unity 확인 전 |
| 그 뒤(예정) | 속말 — 종류 `Thought`, 배치 항목의 값 | 설계만 정했다(PLAN §1.5) |

들일 때는 그때의 맨 위까지 한 번에 들인다. 중간 상태(대본이 `slot pov studio`로 세우던 P1)는 걷힌 어휘다 — 그 상태로 들이지 않는다.

## 2. 대본에서 바뀌는 것

```yarn
<<portrait female_tyrant z 02>>
…… #portrait
<<face portrait 05>>
…… #portrait
…… #thought
```

| 포크의 지금 | 공개 런타임 | |
|---|---|---|
| `<<slot pov stagebox mid>>` `<<cast pov female_tyrant z 02>>` `<<fade_in pov 0fr>>` | `<<portrait female_tyrant z 02>>` | 한 줄. 파서가 세우기 · 캐스팅 · 보임으로 푼다 |
| `<<move_by pov -15.75u -8.5u 0fr>>` 등 자리 · 크기 | **쓰지 않는다** | 구도는 프리셋 에셋의 값이다. 써도 보이는 것이 없다 |
| `<<face pov 05>>` · `<<motion pov …>>` | `<<face portrait 05>>` · `<<motion portrait …>>` | 슬롯 키가 `portrait` 하나로 정해져 있다 |
| `#protagonist` | `#portrait` | 대사창 종류 `Portrait`(공개 런타임에 이미 있던 종류) |
| `#thought` | `#thought` | 그대로 — 공개 쪽 속말이 선 뒤 |
| (주인공을 치우는 줄이 있으면) | `<<portrait_close>>` | 초상 인물을 숨긴다 |

- **`studio` · `pov`는 대본에 쓰지 않는다.** 촬영소의 무대 키는 파서의 것이다 — `<<slot … studio>>`는 경고하고 건너뛴다.
- `portrait`를 다시 쓰면 초상 인물을 새로 세운다(인물 · 옷을 바꿀 때).
- 화자 이름으로 초상 줄을 고르는 길(화자 정책 표 — 예제의 화자 `나`)도 있다. 재현 대본은 주인공 줄에 화자를 쓰지 않으므로 지금처럼 **줄마다 태그**를 쓴다.
- 초상 인물은 폴드 밖이다 — 하네스는 `portrait` · `portrait_close`와 슬롯 `portrait`에 쓴 명령을 "접지 못한 명령"으로 남긴다. 불일치가 아니다.

## 3. 작업공간 — `ked-vn-workspace`

### 3.1 런타임 포크 `runtime/`

"합치기"가 아니라 **"바꾸기"** 다 — 포크의 주인공 대사창(11-12)을 걷고 공개 것으로 바꾼다.

걷는 것:

- `SampleBootstrap.Presentation.cs`의 `DialogueStageKey`(`stagebox`) 조립
- `KedUIHost.DialogueStageMount`
- `PlayDialogueBox`의 `StageMount` · `FitStage` · `SetStyle`의 주인공 분기 · 초상 칸(RectMask2D) — 그대로 두면 초상 칸이 둘이 된다
- `IPresentationDialogueBoxView.ApplyBoxKind`와 `KedDialogueBoxView`의 그 구현
- 종류 `Protagonist`. `Thought`는 공개 쪽 속말이 같은 이름으로 들어온다 — 포크의 것을 걷고 공개 것을 받는다
- `DialogueBoxPresentationController`가 주인공 줄의 글 자리를 Surface의 배치에서 빌리던 것(`FORK.md` 2절 — 공개 쪽은 배치 항목 `portrait_narrow`를 쓴다)

들이는 것 — 포크가 고친 파일과 겹치는 곳은 손으로 합친다:

| 파일 | |
|---|---|
| `Playback/DialoguePresenter/Box/DialogueBoxPresentationController.cs` | 겹친다 |
| `Playback/DialoguePresenter/Box/IPresentationDialogueBoxView.cs` | 겹친다 — `SetPortrait(Texture, Func<Rect>)` |
| `Samples/Scripts/Bootstrap/SampleBootstrap.Presentation.cs` · `SampleBootstrap.cs` | 겹친다 — 촬영소 조립, `portraitFramePreset` 칸 |
| `Samples/UI/App/Runtime/Flow/KedDialogueBoxView.cs` · `Samples/UI/Kit/Screens/Play/Scripts/PlayDialogueBox.cs` | 겹친다 |
| `Stage/StageRig/StageRigRegistry.cs` | `BuildOffscreen` |
| `Stage/Character/Rig/CastRegistry.cs` · `Stage/Character/Commands/CastCharacterCommand.cs` | 슬롯만 묶기 |
| `Stage/Grammar/StageCommandParser.Table.cs` · `StageCommandParser.CharRigSetup.cs` | 표에 둘, `slot … studio` 경고. 포크가 고친 표와 겹친다 |
| 새 파일 | `Stage/PortraitStudio/`(셋) · `Stage/Grammar/StageCommandParser.Portrait.cs` · `Samples/UI/Kit/Screens/Play/Scripts/PlayPortraitCell.cs` · `Samples/UI/App/Resources/KedUI/PortraitCapture.shader` · 시험 둘(`StageRigRegistryOffscreenTests` · `CastRegistrySlotOnlyTests`) |

지킬 것:

- **`.meta`는 공개 쪽 것을 그대로 들인다.** Unity가 새로 만들게 두면 GUID가 달라져 프리셋 에셋이 스크립트를 잃는다. `1a046a7` 뒤에 생긴 파일(`StageCommandParser.Portrait.cs` · `CastRegistrySlotOnlyTests.cs`)의 `.meta`는 소유자가 공개 쪽에서 커밋한 뒤에 들인다.
- **프리셋 에셋은 포크 것을 따로 둔다.** 공개 예제의 값(`Samples/Settings/PortraitFramePreset.asset`)은 예제 대사창과 예제 인물에 맞춘 것이다. 포크의 대사창 · 주인공 그림에 맞는 값은 `Workspace/Presets/`에 에셋을 하나 두고 조립의 `Portrait Frame Preset`에 잇는다. 값은 재생 중에 에셋을 고치면 바로 보이고 남는다.
- 코어는 바뀌지 않았다 — 코어 사본을 맞출 것이 없다.
- 들인 뒤 `runtime/FORK.md`의 주인공 대사창 절을 "걷음 → 공개 런타임의 초상"으로 고치고, `runtime/docs/PLAN_actor-view.md` 첫머리에 "가 · 나 모두 하지 않는다 — 공개 런타임 `PLAN_portrait-capture.md`가 대신한다"를 적는다.

### 3.2 대본 도구 `tools/stage/`

| 파일 | 고칠 것 |
|---|---|
| `script.py` | 주인공을 세우는 줄을 `<<portrait 캐릭터 포즈 표정>>`로. 표정 · 변형을 바꾸는 줄의 대상을 `pov` → `portrait`로. `move_by pov …` 등 자리 명령을 뺀다. 줄 태그 `#protagonist` → `#portrait`(`#thought`는 그대로). 치우는 줄을 `<<portrait_close>>`로 |
| `pov.py` | 재는 것 가운데 **변형 · 표정만** 쓴다. 자리 값(`POV_X` · `POV_Y`)과 들여쓰기 값은 필요 없어진다 — 걷는다 |
| 대본 | 14편을 다시 낸다. 손 대본(1편)과 `Msg_Test`는 손으로 고친다 |

### 3.3 확인

- 녹화에서 주인공 줄의 초상이 보이는가. 대사창 자리를 가리고 재던 비교는 그대로 두되, 초상이 다시 사라지는 것을 잡을 수 있게 초상 칸 자리를 보는 검사를 하나 둔다(이번에 사라진 것이 점수에 잡히지 않았다).
- 되감기 · 시크로 초상 줄에 닿은 첫 프레임에 빈 칸이나 앞 자세가 비치지 않는가(PLAN §6-3) — 녹화를 프레임 단위로 볼 수 있는 쪽이 작업공간이다. 비치면 공개 쪽에 알린다.
- Game 뷰 1920×886에서 초상이 흐리지 않은가.

## 4. 저작 툴 · 연출 감독 · 추출기

| 소비처 | 할 일 |
|---|---|
| `ked-story-compositor` · `ked-story-director` | 지금은 없다. 코어가 바뀌지 않았고 초상 인물은 폴드 밖이다 — 미리보기에 나오지 않는다. 대본에 `portrait` · `portrait_close` · `#portrait`가 있어도 접지 못한 명령으로 지나간다. 초상을 미리 볼 필요가 생기면 그때 코어에 초상 표를 둔다 |
| `ked-staging-extractor` | 원본에서 주인공 초상을 뽑는다면 낼 어휘는 §2다 |
| `ked-motion-tool-characterRig` | 없다 |

## 5. 반영 기록

| 날 | 소비처 | 한 일 |
|---|---|---|
| | | |
