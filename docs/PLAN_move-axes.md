# 이동의 축 — 슬롯의 Move_X · Move_Y와 left · right · up · down (2026-11-23)

> 상태: **계획 확정(소유자 2026-11-23 — M1 ~ M10). 구현 전.** 코드는 아직 고치지 않았다 · 브랜치(예정) `feat/move-axes`(`dev`에서)
> 앞선 일: [PLAN_rig-layers.md](PLAN_rig-layers.md)(리그의 계층 — 20노드. 이 계획이 그 L-결정 가운데 "축별 노드를 걷는다"를 슬롯 계층에서만 뒤집는다)

## 0. 한 문장

> **본래 자리는 `place`가 놓고, 그 위의 움직임은 `left` · `right` · `up` · `down`이 제 노드에서 한다.** `move_by`는 걷는다.

## 1. 왜

- `move_by`는 쓰기 불편하다(소유자, 2026-11-23 — ked-story-tools로 초안을 고쳐 보고). 한 줄이 가로 · 세로를 함께 받아 "왼쪽으로 3"이 `move_by s1 -3u 0u`가 되고,
  부호를 읽어야 방향이 보인다.
- `move_by`(상대)와 `place`(절대)가 **같은 노드**(`CharSlot_Track`)를 쓴다. 본래 자리와 그 위의 움직임이 한 값에 섞여, `place`를 다시 걸면 움직임이 덮이고
  `move_reset`은 본래 자리까지 0으로 돌린다.
- 한 노드에 트윈은 하나다. 가로와 세로를 따로 걸 수 없다(같은 때의 `move_by` 둘은 뒤 것이 앞 것을 끝값으로 확정한다).
- 작업공간의 대본은 첫 자리 잡기 · 등장 · 옆걸음을 모두 `move_by`로 적는다(재현 대본 2,888 · 스토리 공간 2,718, `place` 0 · `slide_in` 0) —
  첫 자리는 `move_by … 0fr`, 그 뒤 다시 `move_by`. 무엇이 자리이고 무엇이 움직임인지 대본에서 읽히지 않는다.

## 2. 정한 것 (소유자, 2026-11-23)

| # | 결정 |
|---|---|
| M1 | **X · Y 노드는 슬롯 계층에 둔다** — `CharSlot_Track` 바로 아래. 초상 계층(`CharacterPortrait_Track_Move`)은 건드리지 않는다: 이동 계층은 모션 계층과 섞이면 안 된다 |
| M2 | 이 이동은 **깊이(크기 배율)의 영향을 받지 않는다** — 어느 크기에서나 화면 값 그대로. 회전의 영향도 받지 않는다(포커스 · 슬롯의 회전 노드보다 위) |
| M3 | `left` · `right`는 `CharSlot_Move_X`, `up` · `down`은 `CharSlot_Move_Y`를 쓴다. 상대값이다 |
| M4 | **`move_by`를 완전히 걷는다.** 다른 이름의 고급 커맨드로도 남기지 않는다 |
| M5 | 본래 자리는 `place 슬롯 x y`(절대 — 이미 있다)가 놓는다 |
| M6 | `move_reset`은 `CharSlot_Move_X` · `_Y`만 0으로 놓는다 — 본래 자리(`CharSlot_Track`)는 건드리지 않는다(지금은 Track을 0으로 놓는다) |
| M7 | 장면이 바뀌어 리그를 다시 세울 때 · `slot`으로 앵커를 다시 놓을 때 Move_X · _Y도 0으로 — Track · Rotation을 기본값으로 돌리는 그 목록에 더한다 |
| M8 | 옛 이름 `move_by`는 별칭을 두지 않는다 — 모르는 명령(오류). N5 · `stage_` 이름 바꾸기와 같은 결 |
| M9 | **`slide_in` · `slide_out`을 이번에 함께 걷는다**(X3) — 넷으로 적거나 뒤에 모션처럼 프리셋으로 두는 쪽이 낫다. 작업공간은 쓴 적이 없고 공개 대본은 8곳이다. 튐(`punch`) 인자는 사라진다. `SlideMotion` · `SlideCommandCharR` · 폴드 · 시험 둘(`SlideMotionParityTests` · `SlideTargetTests`)도 함께 걷는다 |
| M10 | **쓰는 규칙** — `place`는 **시작 자리를 정할 때만** 쓴다: 장면 · 에피소드의 머리, 퇴장했다가 다시 설 때, 아예 다른 자리로 배정할 때. 그 사이의 움직임(옆걸음 · 들고 남 · 몸을 내리고 올리기)은 넷으로 **쌓아 간다** — 작가가 쓰기에 이쪽이 편하다. 다시 `place`할 때는 쌓인 것을 먼저 걷는다(`move_reset 슬롯 0fr` → `place`). 이 규칙으로 쓴 대본이 뒤의 모든 작업물의 본보기다(§4.3) |

## 3. 계층 (20노드 → 22노드)

```
CharFocus_Track                 focus_move
 └ CharSlot_Track               place(절대) · slide_in/out            ← 본래 자리
    └ CharSlot_Move_X           left · right(상대)                     ← 새로 둔다
       └ CharSlot_Move_Y        up · down(상대)                        ← 새로 둔다
          └ CharFocus_Rotation  focus_rotate            [발밑]
             └ CharFocus_Scale … (아래는 그대로)
```

- 걷는 노드는 없다. `CharFocus_Rotation`의 부모가 `CharSlot_Track`에서 `CharSlot_Move_Y`로 바뀐다.
- 리그 계층의 정본은 `CharacterRigSchema.Nodes`(`Stage/Character/Rig/CharacterRigDefinition.cs`)다 — 배열 · `Refs` · `CharacterRigNode` · `CharacterRigRefs` · 빌더(`CharacterRigBuilder`)를 고친다.

## 4. 커맨드

| 커맨드 | 인자 | 노드 | 값 |
|---|---|---|---|
| `left` · `right` | `슬롯 거리 [길이=0.4s] [이징]` | `CharSlot_Move_X` | 상대 — 지금 값에서 그만큼. `left`는 −, `right`는 + |
| `up` · `down` | `슬롯 거리 [길이=0.4s] [이징]` | `CharSlot_Move_Y` | 상대 — `up`은 +, `down`은 − |
| `move_reset` | `슬롯 [길이]`(그대로) | `CharSlot_Move_X` · `_Y` | 둘을 0으로. **본래 자리(`CharSlot_Track`)는 건드리지 않는다** — 지금은 Track을 0으로 놓는다 |
| `place` | (그대로) | `CharSlot_Track` | 절대 — 시작 자리(M10) |
| ~~`slide_in` · `slide_out`~~ | — | — | 걷는다(M9) |
| ~~`move_by`~~ | — | — | 걷는다 |

- 거리는 양수로 적는다. 음수는 받되(반대 방향) 시험이 뜻을 못 박는다.
- 대각선은 같은 앵커에 둘을 나란히 적는다 — 노드가 달라 함께 돈다. 길이 · 이징을 같게 적으면 `move_by`의 한 줄과 화면이 같다.
- 종류는 넷 다 **자리와 구도**(명세 표). 인자 칸: 슬롯 · 거리 · 길이 · 이징.

### 4.1 그대로 두는 것

- `bg_move`(배경 슬롯 — `CharSlot_Track`에 상대값)는 이번 범위 밖이다. 배경은 본래 자리와 움직임을 가르는 쓰임이 없다.
- 컷인 · 팝업 · 초상이 안에서 쓰는 Track 놓기(파서의 `CutIn` · `Popup` · `CharRigSetup`)는 절대값이라 그대로다.

### 4.2 들고 나는 움직임을 적는 꼴

본래 자리는 **끝에 설 자리**다. 들어오는 움직임은 Move 노드를 밀어 두었다가 0으로 되돌리는 것이다 — `place`의 값이 움직임 때문에 바뀌지 않는다.

```yarn
// 옆에서 들어온다 — 오른쪽 바깥 3.27u에서 제자리로
<<at start place s1 8u 0u 0fr>>
<<at start right s1 3.27u 0fr>>
<<at start fade_in s1 0.6s>>
<<at start left s1 3.27u 0.37s Linear>>
// 옆으로 나간다 — 나간 만큼은 Move 노드에 남는다. 다시 세울 때 move_reset 0fr
<<at start right s1 3.43u 0.25s Linear>>
<<at start fade_out s1 0.5s>>
```

같은 노드에 `0fr`과 트윈을 잇달아 거는 것은 지금의 `move_by … 0fr` → `move_by …`와 같은 길이다(앞 것을 끝값으로 확정하고 시작한다).

### 4.3 쓰는 규칙 (M10)

| 때 | 적는 것 |
|---|---|
| 장면의 머리 · 에피소드의 머리에서 인물을 세운다 | `move_reset 슬롯 0fr` → `place 슬롯 x y 0fr` — 시작 자리 |
| 서 있는 동안의 움직임 — 비켜서기 · 모이기 · 다가서며 내리기 · 들고 나는 걸음 | `left` · `right` · `up` · `down`. 쌓인다 |
| 퇴장했다가(`fade_out`) 다시 선다 | `move_reset 슬롯 0fr` → `place 슬롯 x y 0fr` → 등장 |
| 아예 다른 자리로 배정한다(덮개 밑 · 보이지 않는 사이 · 순간에) | `move_reset 슬롯 0fr` → `place 슬롯 x y 0fr` |

- `place`는 `move_reset`과 짝으로 적는다 — `place`만 적으면 쌓인 움직임이 새 자리 위에 그대로 얹힌다. 커맨드 하나가 한 노드를 놓는다는 원칙을 지켜 `place`가 저절로 걷게 하지는 않는다.
- `place`에 길이를 적어 옮겨 가는 것은 본보기의 말씨가 아니다(받기는 한다) — 보이는 동안의 움직임은 넷이다.
- 쌓인 값 때문에 "지금 어디인가"를 대본에서 바로 읽을 수 없다. 읽는 것은 도구의 일이다 — 코어의 폴드가 줄마다 그 슬롯의 자리(Track + Move_X + Move_Y)를 안다(ked-story-tools가 보여 줄 수 있다).

## 6. 바뀌는 곳

| 층 | 파일 | 무엇 |
|---|---|---|
| 리그 | `Stage/Character/Rig/CharacterRigDefinition.cs` · `CharacterRigBuilder.cs` | 노드 둘 · 부모 바꾸기 · Refs |
| 커맨드 | `Stage/Character/Commands/MoveByCommandCharR.cs`(축 하나만 거는 길) · `SetAnchorCommandCharR.cs` · `ApplyTrackOffsetCommandCharR.cs` | 넷의 스펙 · 초기화 목록 |
| 파서 | `Stage/Grammar/StageCommandParser.Table.cs` · `.CharRigSetup.cs` | 넷을 더하고 `move_by`를 뺀다 · `move_reset` |
| 브리지 | `Playback/YarnBridge/CommandBridge*.cs` | Yarn 등록 |
| 코어 | `Core/Reduce/StageReducer.Staging.cs` · `StageReducer.cs` · `NodeTargets.Staging.cs` · 노드 이름 상수 | 폴드 — "재생 = 정지 프레임" |
| 명세 표 | `Core/Spec/StageCommandSpecTable.Core.cs` → `ExportedTuning/command-specs.json` | 85줄 → 86줄(+4 −3) |
| 덤프 | `ExportedTuning/rig-schemas.json` 외 | 다시 낸다(Unity 메뉴) |
| 대본 | `Assets/Dialogue` · `Assets/Samples` | `move_by` 3곳 · `slide_in` 5 · `slide_out` 3을 §4.2의 꼴로 옮긴다(`place` 50은 그대로) |
| 시험 | `StageCommandParserTests` · `StageCommandSpecTable*Tests` · `StageReducerTests` · 리그 빌더 시험 · 등가 시험 | 새 어휘 · 22노드 |
| 문서 | `README.md` · `docs/command-authoring.md` · `PLAN_rig-layers.md`(이 계획을 가리키는 한 줄) | |

## 7. 단계

| # | 내용 | 확인 |
|---|---|---|
| X1 | 리그 — 노드 둘 · 빌더 · 코어의 노드 상수. 커맨드는 그대로(`move_by`가 아직 Track) | 시험 통과 · 데모가 전과 같다 |
| X2 | 커맨드 넷 · `move_reset` · 초기화 목록 · 파서 · 브리지 · 폴드 · 명세 표 | EditMode 시험 |
| X3 | 대본 11곳을 옮기고 `move_by` · `slide_in` · `slide_out`을 걷는다 · 문서 · 덤프 다시 내기 | 소유자: Unity 컴파일 · EditMode · 데모 재생 · 되감기 · 빨리 넘기기 |

단계마다 시험이 통과한 채로 커밋한다.

## 8. 밖에서 따라올 것

| 저장소 | 일 |
|---|---|
| ked-vn-workspace | 포크에 들이기 → `tools/stage/script.py` · 카드(`cards.json`의 커맨드) · 초안 도구를 새 어휘로: 첫 자리 → `place`, 시작 자리 → `place`, 그 사이의 움직임 → 넷(§4.3의 규칙). 넘길 일은 작업공간 `docs/handoff-1123-move-axes.md`. 30편 · 스토리 공간을 다시 내고 대표 편을 녹화해 앞 녹화와 견준다 |
| ked-story-tools | 코어 사본을 다시 들인다(명세 표가 넷을 낸다) |
| ked-motion-tool-characterRig | 코어 사본 · 리그 덤프를 다시 들인다(22노드). 걷힌 노드가 없어 저작본의 키는 그대로다. 씬의 자리(`CharSlot_Track`)도 그대로 |

## 9. 하지 않는 것

- 초상 계층(`CharacterPortrait_Track` · `_Track_Move`)의 변경 — 모션의 노드다.
- 한 노드에 축마다 예약(트윈 둘) — 노드를 나눠 풀었다.
- 화면 지점을 이름으로 받는 `place`(역산) — L11 그대로, 계산은 도구가 한다. 화면 지점은 덧그림(`ScreenFocusGridOverlay`)이 u 값으로 보여 준다(따로 한다).

## 10. 화면 지점 덧그림 (따로 — 이 브랜치와 독립)

소유자(11-23): 이미 있는 `Samples/Scripts/Stage/ScreenFocusGridOverlay` · `Stage/Diagnostics/CharacterFocusDebugView`를 발전시킨다.

- 화면을 **적어도 4 × 4**로 나누고, 금과 교점마다 **`place`에 그대로 적는 값(u)**을 적는다 — 연출가가 덧그림을 보고 `place s1 x y`를 바로 쓴다.
- 값은 무대의 크기에서 센다(고정 1920이 아니다 — 작업공간의 재현 화면은 폭 2340이다). 1u의 px는 코어의 것(`UnitToken` · `ReferenceStageWidth`)을 쓴다.
- `place`가 놓는 것은 슬롯의 본래 자리이고 인물의 그림은 배역 앵커만큼 떨어져 선다 — 덧그림이 가리키는 것이 "슬롯의 자리"인지 "얼굴 · 가슴이 오는 곳"인지 화면에 적는다. 얼굴을 지점에 놓는 값은 코어의 `NodeTargets.TrySolveFocusPlacement`가 푼다(L11 — 부르는 쪽은 도구).
- 재생 중에 켜고 끈다(개발용 — 빌드에는 넣지 않는다). 구현을 시작할 때 두 파일을 읽고 이 절을 채운다.
