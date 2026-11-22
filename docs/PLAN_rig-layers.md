# 리그의 계층 — 포커스 · 슬롯 · 초상

2026-11-21 · 브랜치 `feat/rig-layers`(dev에서) · 상태: **R1~R5 끝 — dev에 합쳤다(2026-11-22). 남은 것: R6 작업공간(N4와 함께) · R7 모션 툴** · 앞선 일: [PLAN_line-anchor.md](PLAN_line-anchor.md)

## 0. 한 문장

**캐릭터 리그는 세 덩어리다 — 포커스(대화할 때만 서는 자리, 작가 허용의 공간) · 슬롯(무대 위의 본래 자리) · 초상(그 사람의 크기와 연기).**
커맨드는 이 계층에서 나온다: 어느 계층의 어느 노드를 적힌 값으로 놓는가.

## 1. 왜 다시 쓰는가

`feat/command-prune`(합치지 않았다)에서 커맨드를 "무대를 읽는가 · 여럿을 내는가"로만 재단해 걷었다. 그 커맨드가 리그의 어느 계층을 위해
있었는지는 묻지 않았고, 그 결과 `place`(포커스 계층)를 `move_by`(슬롯 계층)로 풀어 적어 두 계층을 섞었다. 본래 자리와 나온 자리가 한 노드에
있으면 돌아갈 곳이 사라진다. 그래서 커맨드가 아니라 계층에서 다시 출발한다.

## 2. 정한 것 (소유자, 2026-11-21)

| # | 결정 |
|---|---|
| L1 | **포커스 계층**은 대화할 때만 서는 자리다. 멀리 있던 인물이 말할 때만 그 자리에 나왔다가 본래 자리로 돌아간다. 실제 무대를 조작해서는 표현하기 힘든 작가 허용의 연출을 위한 공간이라 이동 · 회전 · 배율을 모두 가진다. 쓰고 나면 0으로 되돌린다. 슬롯 · 액팅 계층과 따로 둔다. |
| L2 | 포커스 계층의 노드는 **셋으로 나눈다**(한 노드에 트윈 둘을 걸 수 없다). 이동은 맨 위, 회전 · 배율은 슬롯의 자리 아래에 두어 중심이 그 인물의 발밑이 되게 한다. |
| L3 | 포커스 계층의 커맨드는 놓기 셋과 걷기 하나다 — `focus_move` · `focus_rotate` · `focus_scale` · `focus_reset`. 값은 절대값이다. 블러의 `focus_on` · `focus_clear`는 `depth_focus_on` · `depth_focus_clear`로 바꾼다(쓰는 대본이 없다). |
| L4 | **`place 슬롯 x y`** 를 둔다 — 슬롯의 본래 자리(`CharSlot_Track`)를 절대값으로 놓는다. 초점 프리셋 · 화면 지점 이름을 받던 옛 `place` 계열(역산)은 걷는다. |
| L5 | **깊이는 슬롯에 영향을 주지 않게 내린다.** 깊이가 곱하는 것은 초상(배역 앵커 · 연기 · 그림)이다. `CharSlot_DepthY`는 `DepthOffset`으로 부르고, 초상(`CharacterPortrait_*`)에 속한다. 자리는 `VisualOffset` **바로 위**다 — `CharSlot_Scale`이 있던 자리(§5) |
| L6 | **`size`만 남긴다.** 깊이의 Y(`DepthOffset`)와 배율(`DepthScale`)을 놓는다. Y 옵션을 끄면 Y는 지금 값 그대로 둔다. 레벨(라벨) 말고 **배율도 직접 받는다**. 무대를 재서 초점을 붙잡는 일은 하지 않는다. |
| L7 | `CharSlot_Scale`을 걷는다 — `scale_by` · `scale_reset`이 함께 사라진다. 크기는 `size`(그 사람의 크기)와 `focus_scale`(작가 허용) 둘이다. |
| L8 | `CharSlot_Track_Idle`을 걷는다. idle이 필요하면 `CharacterPortrait_Track_Move_*`를 쓴다. |
| L9 | 이모지는 `DepthScale` 밑에 선다 — 인물이 커지면 머리도 올라가니 크기를 따라가야 한다. |
| L10 | 초점(가슴 · 얼굴 점)을 재는 노드는 깊이 아래여야 한다 — 크기를 바꾸면 초점도 따라와야 한다. 깊이가 `VisualOffset` 위로 가므로 재는 노드는 지금의 `VisualOffset` 그대로다(§5) |
| L11 | 화면 지점 · 캐릭터 초점으로 값을 푸는 일은 런타임 커맨드로 두지 않는다. 계산은 코어의 순수 함수로, 부르는 쪽은 도구다. AI의 경로는 지금 고려하지 않는다. |
| L12 | **좌우 · 상하를 따로 둔 노드를 걷는다** — `CharSlot_Track_X` · `CharSlot_Track_Y` · `CharacterPortrait_Track_Move_X` · `CharacterPortrait_Track_Move_Y`. 더하기만 하던 층이고, 좌우와 상하를 다른 박자로 움직이는 일은 클립 하나(채널 안의 x · y 키)로 짠다. `left` · `right` · `up` · `down`도 함께 걷는다 — `move_by`가 대신한다. |
| L13 | **겹치는 이동은 클립 하나로 짠다.** 한 노드에는 한 번에 트윈 하나다 — 서로 다른 때에 시작한 이동이 한 노드에서 겹치면 뒤의 것이 앞의 것을 끝값으로 보낸다. 이 규칙은 그대로 두고, 겹쳐야 하는 움직임은 한 스텝의 클립 하나로 적는다. |
| L14 | `CharacterPortrait_ActingScale_Y`를 걷는다(쓰는 것이 없다). `CharSlot_Rotation`은 남긴다. |
| L15 | `CharacterPortrait_ActingScale_X`는 `mirror`의 노드다 — 이름을 **`CharacterPortrait_Mirror`** 로 바꾼다. 배율 연기(`ActingScale`)와 합치지 않는다: 합치면 배율 모션이 뒤집힌 부호를 덮는다. |

## 3. 계층

지금(dev):

```
CharSlot_Track_Focus            place 계열(역산)
 └ CharSlot_DepthY              size
    └ CharSlot_DepthScale       size                       [발밑]
       └ CharSlot_Track_Idle    (쓰는 것 없음)
          └ CharSlot_Track      move_by · slide_in/out
             └ CharSlot_Track_X / _Y   left · right · up · down
                └ CharSlot_Rotation
                   └ CharSlot_SwayPivot   rotate_by         [발밑]
                      └ CharSlot_Scale    scale_by          [발밑]   ← 이모지가 서는 자리
                         └ CharacterPortrait_VisualOffset   cast(배역 앵커) ← 초점을 재는 노드
                            └ CharacterPortrait_Track … 액팅 … Sprite / Overlay
```

바꾼 뒤(25노드 → 20노드):

```
CharFocus_Track                 focus_move                 ← 포커스: 맨 위, 무대 값 그대로
 └ CharSlot_Track               place(절대) · move_by(상대) · slide_in/out      ← 슬롯: 본래 자리
    └ CharFocus_Rotation        focus_rotate               [발밑]              ← 포커스
       └ CharFocus_Scale        focus_scale                [발밑]              ← 포커스
          └ CharSlot_Rotation   (쓰는 커맨드 없음 — 남긴다)
             └ CharSlot_SwayPivot   rotate_by              [발밑]
                └ CharacterPortrait_DepthOffset   size의 Y(옵션)               ← 초상: 그 사람의 크기
                   └ CharacterPortrait_DepthScale size     [발밑]   ← 이모지가 서는 자리
                      └ CharacterPortrait_VisualOffset     [발밑]   cast(배역 앵커) ← 초점을 재는 노드
                         └ CharacterPortrait_Track         char_move_to — 놓고 머무는 자리
                            └ CharacterPortrait_Rotation   모션
                               └ CharacterPortrait_Track_Move      모션 — 지나가는 몸짓
                                  └ CharacterPortrait_SwayPivot    char_rotate_to · 모션  [발밑]
                                     └ CharacterPortrait_Shake     모션
                                        └ CharacterPortrait_ActingScale   char_scale_to · 모션
                                           └ CharacterPortrait_Mirror     mirror (옛 ActingScale_X)
                                              ├ CharacterPortraitSprite_Root → _Image          fade · face · pose
                                              └ CharacterPortraitSpriteOverlay_Root → _Image   face_swap
```

| | 노드 |
|---|---|
| 걷는다(7) | `CharSlot_Track_Idle` · `CharSlot_Track_X` · `CharSlot_Track_Y` · `CharSlot_Scale` · `CharacterPortrait_Track_Move_X` · `CharacterPortrait_Track_Move_Y` · `CharacterPortrait_ActingScale_Y` |
| 새로 둔다(2) | `CharFocus_Rotation` · `CharFocus_Scale` |
| 이름 · 자리를 바꾼다 | `CharSlot_Track_Focus` → `CharFocus_Track` · `CharSlot_DepthY` → `CharacterPortrait_DepthOffset`(자리도) · `CharSlot_DepthScale` → `CharacterPortrait_DepthScale`(자리도) · `CharacterPortrait_ActingScale_X` → `CharacterPortrait_Mirror` |

- 포커스의 회전 · 배율이 맨 위가 아닌 까닭: 맨 위 노드의 중심은 화면 아래 가운데다. 옆에 선 인물은 키우면 옆으로 밀리고 돌리면 호를 그린다.
  슬롯의 자리 아래에 두면 중심이 그 인물의 발밑이다. 계층은 하나다 — `focus_reset`이 셋을 함께 되돌린다.
- 깊이가 곱하는 범위가 줄어든다. 지금은 슬롯의 이동(`move_by` …)까지 곱하지만, 바꾼 뒤에는 초상(배역 앵커 · 연기 · 그림)만 곱한다.
  `move_by` · `place`의 값은 어느 크기에서나 화면 값 그대로다. (`rig-axis-order.md` §3.3 · §4는 이 반대를 적고 있다 — 다시 쓴다.)
- 초상의 위치 노드는 둘이 남는다 — 놓고 머무는 자리(`CharacterPortrait_Track`)와 지나가는 몸짓(`CharacterPortrait_Track_Move`). 머무는 값 위에서 몸짓이 돌 수 있어야 한다.
- 리그 계층의 정본은 프리팹이 아니라 `CharacterRigSchema.Nodes`(코드)다. 빌더가 프리팹을 거기에 맞춘다 — 구조 변경은 그 배열을 고치는 일이다.

## 4. 커맨드

| 계층 | 커맨드 | 인자 | 노드 | 값 |
|---|---|---|---|---|
| 포커스 | `focus_move` | `슬롯 x y [길이] [이징]` | `CharFocus_Track` | 절대 — 0 0이 본래 자리 |
| 포커스 | `focus_rotate` | `슬롯 도 [길이] [이징]` | `CharFocus_Rotation` | 절대 — 0이 본래 각도 |
| 포커스 | `focus_scale` | `슬롯 배율 [길이] [이징]` | `CharFocus_Scale` | 절대 — 1이 본래 크기 |
| 포커스 | `focus_reset` | `슬롯 [길이] [이징]` | 위 셋 | 셋을 함께 걷는다 |
| 슬롯 | `place` | `슬롯 x y [길이] [이징]` | `CharSlot_Track` | 절대 — 본래 자리 |
| 슬롯 | `move_by` · `move_reset` | (그대로) | `CharSlot_Track` | 상대 / 0으로. `move_reset`은 포커스를 건드리지 않는다 |
| 슬롯 | `slide_in` · `slide_out` | (그대로) | `CharSlot_Track` | |
| 슬롯 | `rotate_by` · `rotate_reset` | (그대로) | `CharSlot_SwayPivot` | 상대 / 0으로 |
| 초상 | `size` | `슬롯 <레벨·라벨·x배율> [keep_y] [길이] [이징]` | `DepthOffset` · `DepthScale` | 절대 |
| 초상 | `size_reset` | (그대로) | 〃 | mid로 |
| 초상 | `char_move_to` · `char_rotate_to` · `char_scale_to` · `motion` · `mirror` · `face` … | (그대로) | `CharacterPortrait_*` | |
| 블러 | `depth_focus_on` · `depth_focus_clear` | (옛 `focus_on` · `focus_clear` — 이름만) | | |

`size`의 세부(제안 — 확인 받을 것):

- `size s1 14` · `size s1 front` — 레벨(라벨). level 커브의 Y와 배율을 놓는다.
- `size s1 14 keep_y` — 배율만 놓고 Y는 지금 값 그대로 둔다.
- `size s1 x1.04` — 배율을 직접 놓는다(절대값). 레벨이 없어 Y의 근거가 없으므로 **Y는 건드리지 않는다**.
- 셋째 칸이 `keep_y`가 아니면 길이로 읽는다.

걷는 것:

| 커맨드 | 까닭 |
|---|---|
| `place <초점> <지점>` · `place_left` · `place_center` · `place_right` | 역산. 초점을 화면 지점에 놓는 값은 도구가 풀어 `place x y`나 `focus_move x y`로 적는다(L11) |
| `scale_by` · `scale_reset` | 노드(`CharSlot_Scale`)가 사라진다(L7) |
| `left` · `right` · `up` · `down` | 노드(`CharSlot_Track_X` · `_Y`)가 사라진다(L12) — `move_by x 0u` · `move_by 0u y` |
| `size`의 셋째 칸(보존할 초점) | 무대를 재지 않는다(L6) |
| `show` · `shot_to` · `shot_focus_to` | 앞서 정한 것 — §8 |

## 5. 깊이의 자리 — (나)로 정했다

> 소유자, 2026-11-21: `VisualOffset` 바로 위로 둔다. 아래는 그렇게 정한 까닭의 기록이다.

L5는 "`VisualOffset`보다도 아래"로 정했다. 문서를 쓰며 배역 앵커의 실값을 보니 그 자리에 대가가 있다. 둘 다 **슬롯에 영향을 주지 않는 것은 같다**
— 차이는 배역 앵커(`VisualOffset`)를 깊이가 곱하느냐 하나다.

배역 앵커는 21명 중 13명이 기본값이 아니다(런타임 · 작업공간 덤프가 같다): harin · sigure · kasmi는 오프셋 y 271 · 배율 0.498,
tyrant는 (−30, −800) · 5, Amber는 (330, 0) · 10.

| | (가) `VisualOffset` **아래** — L5에 적힌 대로 | (나) `VisualOffset` **바로 위** — `CharSlot_Scale`이 있던 자리 |
|---|---|---|
| 이모지(`DepthScale` 밑, L9) | 배역 앵커의 배율을 같이 받는다 — harin의 이모지는 절반, Amber는 10배가 된다. 머리 자리 표(발에서 잰 px)도 같이 곱해진다. 이모지 쪽에서 앵커를 되돌리는 보정이 새로 필요하다 | 지금과 같다(지금 서는 `CharSlot_Scale`이 바로 그 자리다) |
| 크기의 중심 | 앵커가 옮긴 뒤의 바닥이다 — harin은 지금보다 271px 위. 같은 배율에서 서는 자리가 지금의 `scale_by`와 달라진다 | 지금의 `scale_by`와 같다 |
| 작업공간(`scale_by` 1,445곳) | `size x배율`로 바꾸면서 `script.py`의 자리 식도 바꿔야 한다 | `size x배율`로 바꾸기만 하면 된다(상대값 → 절대값) |
| 초점을 재는 노드(L10) | `VisualOffset`에서 깊이 아래의 노드로 옮긴다. 초점 표의 값(`VisualOffset` 기준 px)의 뜻이 달라진다 | 그대로 `VisualOffset`이다 — 이미 깊이 아래다 |

**(나)로 정했다.** 정한 뜻(슬롯에 영향 없음 · 초상 소속 · 이모지가 크기를 따라감 · 초점이 크기를 따라감)이 모두 그대로이고, 새로 생기는 보정이 없다.
이름은 초상 소속(`CharacterPortrait_DepthOffset` · `_DepthScale`)으로 둔다. §3의 그림은 (나)다.

## 6. 바뀌는 곳

| 자리 | 무엇 |
|---|---|
| `Stage/Character/Rig/CharacterRigDefinition.cs` · `CharacterRigBuilder.cs` | `CharacterRigSchema.Nodes` · `Refs` · `CharacterRigNode` · 참조. 프리팹은 빌더가 다시 세운다 |
| `Stage/Grammar` · `Playback/YarnBridge` | 파서 표 · 브리지 등록 · 전달 — `place` · `size` · `focus_*` · `depth_focus_*`, 걷는 이름들 |
| `Stage/Character/Commands` | `SetDepthCommandCharR`(Y 옵션 · 배율 · 초점 보존 걷기) · `SetAnchorCommandCharR`(리셋 목록에서 걷는 노드들) · `MirrorCharacterCommandCharR`(노드 이름) · `PlaceCharacterFocusCommandCharR`(걷기). 포커스 커맨드는 있는 것(`MoveBy` 절대 · `RotateTo` · `ScaleTo`)에 노드만 준다 |
| `Stage/Emoji` | 설 자리를 `CharSlot_Scale` → `DepthScale`(다섯 파일) |
| `Stage/Character/Placement` · `PortraitStudio` · `Diagnostics` | 초점을 재는 노드(§5가 (가)일 때만) |
| `Stage/Blur` | 커맨드 이름만(`depth_focus_*`). 깊이 값(`SettledDepthScale`)은 `size`가 그대로 적는다 |
| `Core/Reduce` · `Core/Motion` | 폴드(`place` · `size` · `focus_*` · `move_reset` · 리셋 목록) · 노드 이름 상수 · `MotionRules`의 키잉 금지 노드 |
| `Tests/EditMode` | 합성 리그를 적어 둔 시험(리듀서 · 초점 · 배경 · 슬라이드 · 이모지 …)과 Unity 대조 시험 |
| `ExportedTuning/rig-schemas.json` | 리그가 바뀌면 **Unity에서 다시 내야 한다**(소유자) |
| `Assets/Dialogue` | 데모 · 예제 4편 — `place` 계열 31곳 · `size` 19곳 · `shot_focus_to` 1곳 · `up` 2곳. 포커스 계층을 쓰는 예를 하나 둔다. `motions.json`의 `nod`는 `CharacterPortrait_Track_Move`로 옮긴다(키 값은 그대로) |
| `Tests/EditMode/UnityParity/ToolExport~/motions.json` | 모션 툴이 내보낸 견본 — `CharSlot_Track_X` · `CharSlot_Scale`에 키가 있다. 남는 노드의 견본으로 다시 받는다(R7과 맞물린다) |
| 문서 | `rig-axis-order.md`(다시 쓴다) · `settled-chain.md` · `target-boundary.md` · `stage-rig.md` · `schema.md` |

## 7. 단계 (§5가 정해진 뒤)

Unity는 한 묶음이 함께 컴파일돼야 하므로, 커밋은 나누되 R1~R3은 한 번에 확인한다.

| 단계 | 무엇 | 확인 |
|---|---|---|
| R1 | 코어 — 노드 이름 · 폴드 · 리셋 목록 · 걷는 커맨드 · 시험의 합성 리그 | 코어 시험(.NET) |
| R2 | 리그 — 스키마 배열 · 참조 · 빌더 · 이모지의 설 자리 | Unity(소유자) |
| R3 | 커맨드 — 파서 표 · 브리지 · 실행 | Unity(소유자) |
| R4 | 데모 · 예제 대본을 새 어휘로, 문서 | 옛 대본(옛 코어) ↔ 새 대본(새 코어)의 무대 대조 — `feat/command-prune`에서 쓴 방법 |
| R5 | 덤프 다시 내기 · 등가성 하네스 | Unity(소유자) |
| R6 | 작업공간 — 포크 들이기 · `script.py`(`scale_by` → `size x배율`) · `yarn_check.py` · 다시 내기 · 녹화 | 줄의 앵커 N4와 함께. 그 전까지 작업공간은 영향이 없다 |
| R7 | 모션 툴의 코어 사본 · 스키마 | 런타임이 정해진 뒤 |

## 8. `feat/command-prune`에서 가져올 것

| 그 브랜치의 일 | 여기서 |
|---|---|
| `show` 걷기(`a50034ea`) · `shot_to` 걷기(`c6a8aa3d`) | 계층과 무관하다 — 그대로 가져온다 |
| `shot_focus x y`(`31aacead`) · `shot_focus_to` 걷기 | 뜻은 그대로. 커밋이 `place` 걷기 위에 얹혀 있어 다시 적는다 |
| `size`에서 초점 보존 떼기(`b688df44`) | 방향은 같다(L6). 인자와 노드가 달라져 다시 한다 |
| `place` 계열 걷기(`8a44f6ed`) | 역산을 걷는 것은 같다(L4). **데모를 `move_by`로 풀어 적은 것은 버린다** — `place x y`와 포커스 계층으로 다시 적는다 |
| 값을 푸는 변환기 · 두 겹의 대조(옛 코어 ↔ 새 코어) | 그대로 쓴다 |
| 계획서 `PLAN_command-prune.md` | 이 문서가 대신한다. 그 브랜치는 합치지 않는다 |

## 9. 남은 물음

| # | 무엇 | 제안 |
|---|---|---|
| ② | `cast`가 되돌리는 것 — 지금은 슬롯의 이동 축과 초상의 연기 축이다. 포커스와 깊이는 건드리지 않는다 | 그대로 둔다 |
| ③ | 포커스 노드에 모션 클립의 키를 허용할지 — 지금 `Track_Focus`는 금지다(역산의 해였기 때문) | 금지를 그대로 둔다. 필요해지면 연다 |
| ④ | `size_reset`의 뜻 — 지금은 mid 레벨로 간다 | 그대로 둔다 |
| ⑤ | 정착 장부(리그 커맨드가 끝값을 적어 두는 곳) — 역산 커맨드가 사라지면 읽는 쪽이 진단 뷰와 대조 시험뿐이다. depth 보정의 계산(`SettledFocusMath.SolveDepthYPreservingFocus`)도 부르는 곳이 시험뿐이다(2026-11-22 확인) | 이 일이 끝난 뒤 따로 정한다 |
| ⑥ | 샷의 초점은 줌과 한 전환이다 — 한 줄에 `shot_zoom`과 `shot_focus`를 다른 길이로 적으면 뒤의 길이로 함께 간다 | 그대로 둔다. 거슬리면 초점을 제 축으로 나눈다 |

## 10. 단계별 기록

| 커밋 | 단계 | 한 일 |
|---|---|---|
| `f6a998fc` · `1c0661ed` | §8 | `show` · `shot_to`를 걷었다(`feat/command-prune`에서 가져왔다) |
| `45e25d09` | R1 · R2 | 리그를 20노드로. 스키마 · 참조 · 빌더 · cast의 리셋 목록(런타임 · 코어) · 이모지의 설 자리 · 키잉 금지(포커스 셋) · 노드와 함께 사라지는 커맨드(`left` · `right` · `up` · `down` · `scale_by` · `scale_reset`). `rig-schemas.json` · 프리팹 · 제작 씬 · `nod.anim` · `bow.anim`은 손으로 맞췄다 — R5에서 Unity로 다시 낸다 |
| `0d6c790e` | R1 · R3 | 커맨드 — `place x y` · `focus_move` · `focus_rotate` · `focus_scale` · `focus_reset` · `size`의 `keep_y` · `x배율`(코어 `SizeToken`) · `move_reset`은 슬롯만 · `depth_focus_on` · `depth_focus_clear` · `shot_focus x y`. 옛 `place` 계열 · `shot_focus_to` · 초점 보존을 걷었다. 보조 계산 `NodeTargets.TrySolveFocusPlacement` |
| `35db98dd` | R4 | 데모 · 예제 4편을 새 어휘로, 문서 |
| `c9f448ab` | R5 | Unity가 싣는 NUnit 3.5에 없는 `Using<T>(bool 비교식)`을 축마다 `Within`으로 — 순수 시험을 NUnit 3.5로도 컴파일해 본다 |
| (이 커밋) | R5 | 소유자가 Unity에서 확인했다(컴파일 · 시험). 덤프를 다시 냈다 — `rig-schemas.json`은 손으로 맞춘 것과 내용이 같다. 같이 바뀐 것: `emoji-locator.json`(harin a의 자리) · `surface-layout.json`(새 필드) · `layer-profile.json`(소수 표기). Unity가 다시 만들던 `StageCutInFlashGraphic.cs.meta`(`6fe3b861`에서 빠졌다)를 넣었다 |
| N5의 마무리 커밋(줄의 앵커) | R5 뒤 | depth 대조 시험의 전제를 새 계층에 맞췄다 — 예약한 포커스 배율(`CharFocus_Scale`)이 `CharacterPortrait_DepthOffset`의 조상이 되어, 보정 벡터를 라이브 부모로 옮기는 오라클이 1.2배 어긋났다. 시험은 예약을 리그에 실제로 놓은 오라클의 답과 견준다([PLAN_line-anchor.md](PLAN_line-anchor.md) §12 「N5의 Unity 확인과 마무리」) |

### R4 — 대본을 옮긴 방법과 결과

정답은 **옛 대본을 옛 코어(dev)로 접은 무대**다. 대사 · 스텝마다 슬롯의 측정 노드(`VisualOffset`)의 꼴(원점 · 두 축)과 샷을 적어 두고,
새 대본을 새 코어로 접어 같은 무대가 나오게 슬롯의 자리를 풀었다(변환기는 저장소 밖 — 클라우드 작업 폴더). 리그가 바뀌어 같은 `move_by`도
다른 자리를 내므로(깊이가 슬롯을 곱하지 않는다) 값을 그대로 옮길 수 없다.

- 옛 `place` 계열 → `place 슬롯 x y`(그 묶음의 끝값, 절대값). 같은 때의 `move_by` · `up`은 하나로 합쳤다(한 노드에 트윈 하나).
  화면의 지점이 그대로 읽힌다 — 오른쪽 가슴은 `place s1 11.52u 1.092u`(1920 × 0.24 = 460.8px).
- 묶음에 `place`가 없으면 `move_by`로 다시 적었다. 옛 `move_by 3u`는 깊이 배율(2.364)만큼 곱해져 보였으므로 `move_by 7.092u`가 됐다.
- `size 슬롯 깊이 초점 …` → `size 슬롯 깊이 …`. 초점 보존이 하던 자리 보정은 같은 묶음의 `place`에 들어갔다.
- `shot_focus_to` → `shot_focus` · `shot_zoom` · `shot_pan`(정답의 샷 값 그대로).
- 배역이 없는 슬롯(같은 인물을 다른 슬롯에 캐스팅해 배역이 빠진 `s2` · `s3`)의 `place` 세 곳은 무대에 그려지지 않아 대조할 수 없다 —
  옛 뜻(초점을 그 지점에)대로 도구의 계산으로 풀었다.
- `slide_in`과 같은 묶음의 `place`(두 곳)는 같은 노드라 `place`를 `slide_in` 앞에 적었다 — 놓은 자리로 미끄러져 들어온다.
- 포커스 계층의 예를 하나 더했다(데모 L104~110 — 시구레가 말할 때만 앞으로 나왔다가 `focus_reset`으로 돌아간다).
- 「장부 확인」 절의 설명 줄 · 하린의 한 줄(ln_cjzch0sn — "장부가 기울기를 알아서")은 걷은 동작을 말하고 있어 고쳤다.

대조: 4편의 대사 · 스텝 136곳(데모 124 · 예제 12, 더한 두 줄 제외) — 135곳이 0.02px 안에서 같다. 한 곳(데모 #22, `size s2 front` 줄)은
0.567px 어긋난다: 옛 초점 보존의 Y와 새 레벨 Y의 차이인데, 1px 미만이라 보정을 적지 않았다(`move_by s2 0u 0.014u`가 대본에 잡음으로 남는다).
더한 포커스 예는 앞뒤의 무대를 바꾸지 않는다(그 두 줄을 빼면 나머지가 같다). 좌우 반전(`mirror`)과 모션은 두 코어가 다 접지 않아 대조에서 빠진다 —
세 배역(harin · sigure · kasmi)은 초점 오프셋에 x가 없어 반전이 자리에 영향을 주지 않는다.

`.gitignore`된 `Assets/Dialogue/RealTest/real_test_Mirage_Tavern.yarn`(옛 명령 27곳)은 손대지 않았다.

### R5 — Unity에서 할 일(소유자)

1. 컴파일 · EditMode 시험 전체(파서 · 브리지 · 실행 커맨드 · Unity 대조 시험은 클라우드에서 컴파일하지 못했다 — 고친 .cs는 Roslyn으로 문법만 읽었다).
2. `Ked/U12/Export Presentation Tuning Dump` — `rig-schemas.json`이 손으로 맞춘 것과 같은지(같아야 한다: 스키마에서 그대로 나오는 값).
3. 프리팹 `CharacterRig.prefab` · 제작 씬 `MotionAuthoring.unity`를 열어 계층을 본다(문서 단위로 다시 이었다 — 빌더가 경고 없이 받아야 한다).
   `Ked/Motion/Create Authoring Rig`로 제작 리그를 다시 세워도 된다.
4. `nod.anim` · `bow.anim`의 바인딩이 살아 있는지(경로와 그 CRC32를 새 계층으로 고쳤다). `Ked/Motion/Export motions.json` 뒤 `motions.json`이 지금 것과 같은지.
5. 데모를 재생 · 되감기 · 빨리 넘기기 — 특히 「장부 확인」 1~7, 포커스 예, 컷인(`c1` · `c2`의 `place`).
6. 등가성 하네스 랩드스킵 재판정.

