# 이동의 축 — 슬롯의 Move_X · Move_Y와 left · right · up · down (2026-11-23)

> 상태: **계획 확정(소유자 2026-11-23 — M1 ~ M10). X1 · X2 끝(리그 22노드 · 커맨드 넷) — X3 남음** · 브랜치 `feat/move-axes`(`dev`에서) · 단계별 기록은 §11
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

## 11. 단계별 기록

### 구현을 시작하며 맞춘 것 (소유자, 2026-11-23)

계획서와 코드가 어긋난 곳을 고치기 전에 물었다.

| # | 어긋난 곳 | 정한 것 |
|---|---|---|
| Q1 | §6 · §7에 프리팹 · 제작 씬 · `.anim`이 없다 — 노드 둘이 `CharSlot_Track`과 `CharFocus_Rotation` 사이에 끼어 그 아래 노드의 경로가 모두 바뀐다. 덤프도 X3에서야 다시 내면 `RigSchemaTreeUnityParityTests`가 X1부터 실패한다 | R2처럼 X1에서 손으로 맞추고, X3의 덤프 다시 내기로 확인한다 |
| Q2 | "공개 대본 11곳" 가운데 git이 추적하는 대본은 6곳이다(`move_by` 3 · `slide_in` 3 · `slide_out` 0). 남은 5곳(`slide_in` 2 · `slide_out` 3)은 `.git/info/exclude`로 빠진 `Assets/Dialogue/RealTest/real_test_Mirage_Tavern.yarn` — at 없는 옛 표기라 R4도 손대지 않았다 | 그대로 둔다 |
| Q3 | 데모 6-1 · 6-2의 `move_by s1 7.092u`가 `right`(Move_X)로 가면 이어지는 7번 줄 `place s1 11.52u 1.092u 60fr`이 그 이동을 덮지 않는다(하린이 25.7u에 선다) — "`place` 50은 그대로"가 이 한 곳에서는 서지 않는다 | 그 `place` 앞에 `move_reset s1 60fr` — 길이 · 이징(OutCubic)이 같아 두 트윈의 합이 전과 같은 화면이다 |
| Q4 | M7의 "`slot`으로 앵커를 다시 놓을 때"는 코드에서 `cast`다 — Track · Rotation을 되돌리는 목록은 `SetAnchor`(resetSlotPos)의 것이고 `cast`가 낸다. `slot` · 장면 경계는 리그를 새로 세워(스키마 초기값) Move가 저절로 0이다 | `SetAnchor`의 목록(런타임 · 코어 `StageReducer.Anchor`)에 더한다 |

### X1 — 리그

- 스키마 · `Refs` · `CharacterRigNode` · `CharacterRigRefs` · `GetComponent` · 빌더의 `BuildRefs`에 `CharSlot_Move_X` · `CharSlot_Move_Y`. `CharFocus_Rotation`의 부모가 `CharSlot_Move_Y`로. 커맨드는 그대로다(`move_by`가 아직 Track).
- 코어의 노드 상수 `NodeTargets.SlotMoveXNodeId` · `SlotMoveYNodeId`.
- 손으로 맞춘 것(Q1): `rig-schemas.json`(노드 둘 — `CharSlot_Track`의 값 그대로, 부모만), `CharacterRig.prefab` · `MotionAuthoring.unity`(게임 오브젝트 둘을 끼우고 다시 이었다), `nod.anim` · `bow.anim`(경로와 그 CRC32).
- 시험: 합성 리그 셋(리듀서 · 배경 · 포커스)에 노드 둘. Unity 시험 하나를 더했다 — 스키마와 덤프가 같은 사슬(Track → Move_X → Move_Y → 포커스 회전)을 갖는가.
- 확인: 순수 시험 598을 .NET 8로 돌려 통과. Unity 쪽 코드는 클라우드에서 엔진 없이 컴파일해 전과 견줬다(새 오류 0) — Unity 컴파일 · EditMode는 소유자.

### X2 — 커맨드 넷 · `move_reset` · 초기화 목록

- 코어: 넷의 노드와 방향을 표 하나로 둔다 — `NodeTargets.TryGetSlotMove`(left −X · right +X · up +Y · down −Y). 파서와 폴드가 같은 표를 본다.
  폴드 `ApplySlotMove`(상대, `UnitToken.TryParseSignedPixels` — 음수는 반대 방향). 거리에는 생략값이 없다 — 빠졌거나 못 읽으면 Unhandled.
- `move_reset`은 Move_X · _Y에 절대 0(스펙 둘) — 본래 자리(Track)는 그대로다(M6). 코어 폴드도 같다.
- 초기화 목록(Q4): `SetAnchor`의 resetSlotPos에 Move_X · _Y — 런타임 `SetAnchorCommandCharR`과 코어 `StageReducer.Anchor`가 같은 차례(Track · Move_X · Move_Y).
- 파서 표 · 브리지 등록 · 전달. 런타임 스펙은 지금의 `MoveByCommandSpecCharR`(상대) 그대로다 — 노드만 Move_X · _Y. Move_X의 세로 · Move_Y의 가로는 아무도 쓰지 않으므로
  `MoveByCommandCharR`에 축 하나만 거는 길을 따로 두지 않았다(§6의 그 줄).
- 명세 표: 넷(자리와 구도 · 슬롯 · 거리(꼭) · 길이 0.4s · 이징), `move_reset`의 요약. `command-specs.json` 85 → 89(X3에서 −3 → 86).
  손으로 고치지 않고 코어의 `CommandSpecsFile.ToDto`를 .NET에서 돌려 냈다 — 같은 생성기가 고치기 전 파일을 바이트까지 같게 낸다.
- 시험: 리듀서(넷이 쌓인다 · 음수 · 거리 없음 · `move_reset`은 움직임만 · `place`는 움직임을 걷지 않는다 · 기울기와 크기에 곱해지지 않는다 · `cast`가 움직임을 되돌린다),
  파서(넷의 노드 · 부호 · 생략값 · 거리 없음 · `move_reset`의 두 스펙), 명세 표(넷의 칸), 앵커 목록.
  "걷은 이름"에서 넷을 뺐다 — 슬롯의 움직임으로 돌아왔다.
- 확인: 순수 시험 605 통과(.NET 8). 명세 표 ↔ 파서 표 대조(`StageCommandSpecTableParserTests`)는 파서를 표의 원문에서 읽은 이름으로 바꿔 끼워 돌려 통과.
  Unity 쪽 코드는 엔진 없이 Roslyn으로 메서드 본문까지 진단해 고치기 전(dev)과 견줬다 — X1 · X2를 합쳐 새 오류 0(X1의 확인은 선언만 보는 컴파일이었다).

