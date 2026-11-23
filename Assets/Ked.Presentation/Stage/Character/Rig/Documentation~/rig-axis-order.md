# 리그 축 순서 — 계층이 규약인 이유

`CharacterRigSchema.Nodes`의 22줄짜리 배열은 **순서만 보여주고 순서의 이유는 보여주지 않는다.**
트랜스폼 계층은 결과만 남고 의도가 안 남는 구조라, 이유를 여기 남긴다.

## 0. 두 문장

> **리그 계층은 프리팹이 아니라 스키마가 소유한다.** 프리팹은 손으로 만지면 조용히 망가지고,
> 망가진 계층은 에러가 아니라 "연출이 좀 이상하다"로 나타난다.
>
> **노드의 자리가 그 노드의 뜻이다.** 같은 연산(스케일·회전·이동)이 계층 여러 곳에 나오고,
> 무엇에 곱해지느냐가 자리로 정해진다. 자리를 옮기면 뜻이 바뀐다.

---

## 1. 기원 — 왜 프리팹이 아니라 스키마인가

### 1.0 목적이 먼저다 — 연출은 쌓아서 재활용하는 자산이다

이 시스템은 커맨드 조합으로 **연출 자산을 쌓아 올려 재사용하는 것**이 목적이다.
한 장면에서 만든 연출이 다른 화면에서, 다른 프로젝트에서 같은 결과를 내야 자산이 된다.

그러려면 **연출 자체의 균일성**이 전제다. 리그 계층이 화면마다·프로젝트마다 조금씩
다르면 같은 커맨드 조합이 다른 결과를 내고, 쌓아 온 연출은 그 순간 재사용할 수 없는
일회용이 된다.

**프리팹은 이 전제를 지킬 수 없다.** 프리팹은 씬·프로젝트마다 사본이 생기고, 사본은
따로 편집되고, 편집된 것끼리 조용히 갈린다. 그래서 계층의 소유권을 **어디서 실행되든
같은 하나뿐인 것** — 코드의 스키마로 옮겼다.

아래 1.1~1.4는 이 결론에 도달하기까지 실제로 치른 값이다.

### 1.1 프리팹이 소유하던 시절의 비용

처음(`aa81d38e`, 2026-03-09)에는 계층이 프리팹에 있었다. 코드는 프리팹을 `Instantiate`하고
이름으로 노드를 찾아 참조를 채웠다. 그때 치른 비용이 커밋 로그에 그대로 남아 있다 —
**"프리팹 최신화"가 붙은 커밋이 7회**다.

```
2026-05-19  fix: CharacterRig 프리팹 최신 버전으로 갱신
2026-06-13  chore: characterRig, BackgroundRig 내 framing계층 제거, 프리팹최신화
2026-06-15  fix: characterRig Emoji계층구조 변경 및 프리팹 최신화
2026-06-23  fix: BackgroundRig 계층 구조 수정 및 프리팹 최신화
2026-06-25  refactor: PresentationUIRoot 정리 및 프리팹 최신화
2026-08-19  refactor: Blur 레거시 코드 제거, 프리팹 최신화
2026-08-20  chore: 커맨드 및 리그 프리팹 구조 Json 갱신
```

계층을 한 번 고칠 때마다 **코드와 프리팹 두 곳을 같이 고쳐야 했다.** 그리고 프리팹 쪽을
빠뜨리면 컴파일은 통과한다. 문제는 실행 중에 좌표계가 어긋난 채로 그냥 돌아간다는 것이다.

> **두 곳에 있으면 갈린다** — 저장소 전체가 쓰는 원칙이 여기에도 그대로 적용된다.
> ([SCOPE-BOUNDARY.md](../../../../../SCOPE-BOUNDARY.md) §2)

### 1.2 결정적 하루 — 2026-05-15

소유권을 옮긴 작업이 하루에 다섯 커밋으로 끝났다. 순서에 논리가 있다:

| 커밋 | 한 일 | 왜 이 순서인가 |
|---|---|---|
| `3ece3d90` | 생성과 바인딩 책임 분리 | 옮기기 전에 **무엇이 옮겨갈지**를 먼저 갈랐다 |
| `ad3f6e7b` | `CharRig`·`CharRigTarget`·`CharRigRefs`를 한 스크립트로 통합 | 흩어진 정의를 **한 자리에 모았다** |
| `67e9aea6` | Builder 검증·복구 기능 보완 | 스키마가 소유하려면 **어긋남을 감지할 수 있어야** 한다 |
| `97dfc62c` | **graph definition을 schema로 이동** | 소유권 이전 |
| `b470fc88` | 훼손된 프리팹에서도 재생성되도록 수정 | 이전을 **실전에서 검증** |

마지막 `b470fc88`이 이 설계의 성격을 가장 잘 보여준다. 커밋 본문 전문:

```
- 증상: 훼손된 Prefab 사용시 CharacterRig가 재생성되지 않음
- 원인: Destroy()로 삭제 예약된 child가 같은 프레임 동안 hierarchy에 남아 있어,
        EnsureGraph()의 FindByName()이 삭제 예정 노드를 재사용
- 수정: Destroy() 전에 child.SetParent(null, false)로 rigRoot에서 분리해,
        재생성 과정의 검색 대상에서 삭제 예정 노드를 제거
```

`CharacterRigBuilder.EnsureValidGraphMap`에서 **파괴 전에 `SetParent(null, false)`를 부르는
줄은 장식이 아니다.** 지우면 훼손된 프리팹의 복구가 조용히 실패한다.

### 1.3 스키마 방식이 다른 리그로 퍼졌다

계약이 좋으면 전파된다. `821f1e2b`(2026-06-24)에서 **화면 효과 리그도 같은 모양으로 갈렸다**
(`refactor: screen effects를 schema기반 rig로 분리`). 한때 네 리그가 전부 같은 네 이름을
썼다 — `Definition` · `Builder` · `Registry` · `Resolver`. 지금은 둘(캐릭터 · 화면 효과)이다 — 아래 1.5.

2026-11-05에는 **무대(샷 계층)가 같은 길을 밟았다** — 씬에 놓여 있던 카메라 · 깊이 층 76노드를
`StageRigSchema` · `StageRigBuilder` · `StageRigRegistry`가 세운다. 이유도 같다: 노드의 존재와 항등을
코드가 보장하면 기준값을 잴 필요가 없다. [stage-rig.md](../../StageRig/Documentation~/stage-rig.md).

2026-11-11에는 **이모지 리그**가 같은 방식으로 섰다(`EmojiRigSchema` · `EmojiRigBuilder`) — 다만 틀은 고정이고
리그의 수가 이모지마다 다르다. 캐릭터 리그의 `CharacterPortrait_DepthScale` 밑에 서지만(인물의 크기를 따라간다) 이 스키마의 노드는 아니다.
[emoji-rig.md](../../../Emoji/Documentation~/emoji-rig.md).

### 1.5 그리고 배경 리그가 캐릭터 리그로 합쳐졌다 — 2026-10-29

같은 계약이 **거꾸로도** 작동했다. 배경 리그(2026-05-21 `f8fca379`)는 캐릭터 리그를 본떠 만든
쌍둥이였고, 둘이 달라야 했던 이유는 06-09의 컷인 창(마스크 안에 배경과 캐릭터를 함께 넣고 창째
움직이는 연출)이었다. 그 기능이 08-18 `3a75d4f6`에서 지워진 뒤 남은 배경 리그의 일은
far 레이어에 이미지 한 장을 높이 맞춤으로 깔고·갈아 끼우고·페이드하는 것 — 전부 이 리그로 된다.

그래서 **배경은 캐스팅하지 않은 캐릭터 슬롯**이 됐다. 하이어라키에 `bg_main_CharacterPortraitSprite_Image`가
보이는 것은 그 결과이고, 이름을 `StageRig`류로 바꾸지 않은 것은 노드 이름이 3자 이름 동등성(§0)과
튜닝 덤프에 걸려 있기 때문이다. 대본 어휘 `bg_*`는 남았다 — 파서(`StageCommandParser.Background.cs`)와
코어(`StageReducer.Background.cs`)가 이 리그의 같은 노드로 펼친다:

| 대본 | 노드 |
|---|---|
| `bg_fade_in/out` | `CharacterPortraitSprite_Root` (α) |
| `bg_move` | `CharSlot_Track` — 본래 자리에 상대값(배경은 움직임 노드를 쓰지 않는다) |
| `bg_scale` | `CharacterPortrait_ActingScale` — 가운데 피벗. 깊이(`CharacterPortrait_DepthScale`)는 발 피벗이라 배경이 아래 가운데에서 커진다 |
| `bg_sprite` | `CharacterPortraitSprite_Image` — 종횡비는 초상 DB가 아니라 `background-dimensions.json`, 레이캐스트는 끈다 |

캐릭터와 다른 점은 둘이다(세우는 방식은 같다 — 숨은 채 서고 `bg_fade_in`으로 들인다).
- 배역이 없다 — 그래서 배역 기반 축(`depth_focus_on`)이 배경을 건너뛴다. 배경이 배역 있는 슬롯을 덮으면 배역을 풀고 경고한다.
- 이미지가 클릭을 받지 않는다(화면 전체를 덮으므로).

재선언(같은 키로 다시 세우기)은 캐릭터와 같다: 새 리그를 숨긴 채 세우고 옛 리그를 부순다 — 그림만 바꾸는 것은 `bg_sprite`다.
코어는 그동안 "재사용한다"로 접고 있었다 — 같은 날 바로잡았다.
복제본이 덜 돌봐져 `bg_place`가 넉 달째 깨져 있었던 것이 "두 곳에 있으면 갈린다"의 실제 사례였다.
전말은 재생 저장소의 `docs/PLAN_background-as-slot.md`.

### 1.4 그리고 계층이 데이터가 됐다 — 2026-08-08

스키마가 코드에 있으면 **런타임만 안다.** 순수 코어와 저작 툴은 못 본다. 그래서 계층을
JSON으로 내보내고 코어가 그것을 읽게 했다. 사흘 만에:

| 커밋 | |
|---|---|
| `a4b879e2` | Unity의 RectTransform 계층 좌표 변환을 Core로 포팅 |
| `597ea9e7` | Core에 RectTransform 계층을 표현하는 상태 트리 제작 (`RectNodeTree`) |
| `90ad838c` | 노드 계층 JSON을 `RectNodeTree`로 변환 |
| `eb66649a` | **계층 생성 테스트 및 검증** (`RigSchemaTreeUnityParityTests`) |

이걸로 계층의 단일 출처가 네 세계를 관통하게 됐다:

```
CharacterRigSchema.Nodes            코드      ← 유일한 소유자
        ↓ CharacterRigBuilder
    씬의 실제 계층                   Unity
        ↓ PresentationTuningExporter
  ExportedTuning/rig-schemas.json    데이터
        ↓ RigSchemaLoader
  StageReducer의 slot 폴드           순수 코어
```

**손으로 미러링한 구간이 하나도 없다.** 전부 코드가 뽑는다. 그래서 갈릴 수가 없고,
그래도 갈리는지 `RigSchemaTreeUnityParityTests`가 지킨다:

* `실제_리그에_있는_노드가_덤프에서_빠지지_않았다`
* `덤프의_measuredRectSize가_트리_계산과_일치한다`

---

## 2. 이 구조가 보장하는 것 둘

### 2.1 동작 균일성 — 커맨드가 노드의 존재를 전제할 수 있다

`CharacterRigBuilder`는 프리팹을 받아도 **믿지 않는다.** 그래프를 검사한다:

```
루트에 CanvasGroup이 있나 → 각 노드가 존재하나 → 부모가 스키마와 일치하나
  → 노드가 요구하는 컴포넌트(Image · CanvasGroup)가 붙었나
     하나라도 틀리면 → 자식 전체 파괴 → 스키마로 재구축
```

부모까지 대조하는 게 핵심이다. 노드가 다 있어도 **하나가 엉뚱한 부모 밑에 있으면 좌표계가
통째로 어긋난다.** 그리고 그건 에러를 안 낸다.

이 검사가 통과한 뒤에는 모든 리그가 **정확히 같은 모양**이므로, 커맨드는
`refs.CharFocus_Scale`이 있는지 묻지 않고 그냥 쓴다. 리그마다 다른 예외 경로가 없다.

경고 메시지가 원인 후보를 셋 다 적는 이유도 여기 있다 —
`broken, outdated, or saved with another role prefix`. 셋 다 실제로 겪은 것이다.

`rolePrefix`는 하나로 세 가지를 푼다:
노드 이름이 전역 유일해져 `FindByName`이 안전하고, 하이어라키에서 누가 누군지 보이고,
등가성 하네스가 **노드 키로 대조**할 수 있다(슬롯 키가 코어 노드 키의 prefix가 된다).

### 2.2 연출 항등성 — 같은 yarn은 같은 화면이 된다

계층이 리그마다·실행마다 같아야 "같은 대본 → 같은 결과"가 성립한다. 계층이 흔들리면
`NodeTarget`의 세 갈래가 **서로 다른 좌표계 위에서** 같은 값을 보게 되고, 그러면
같은 값인데 다른 화면이 나온다.

```
              NodeTarget
   ┌────────────────┼────────────────┐
① 장부 게시      ② 트윈 종점      ③ 상태 폴드
(정착 예약)      (실제 재생)      (정지 프레임)
```

세 갈래가 **같은 계층 위에 있다는 전제**가 스키마다. 코어의 폴드가 `rig-schemas.json`으로
리그를 세우는 것이 이 전제를 데이터로 못박은 것이고, 그래서 등가성 하네스의 판정이
의미를 갖는다.

> 목표 계산 쪽 규약은 [target-boundary.md](../../../Core/Documentation~/target-boundary.md).

---

## 3. 축 순서 — 자리가 뜻을 정한다

> 2026-11-21에 세 덩어리로 다시 세웠다(재생 저장소 `docs/PLAN_rig-layers.md`). 그 전의 계층(25노드 — 깊이가 맨 위에서 슬롯의 이동까지
> 곱하고, 좌우 · 상하가 따로였다)은 그 계획서 §3에 있다. 2026-11-23에 슬롯의 움직임 노드 둘을 더했다(20 → 22노드, `docs/PLAN_move-axes.md`).

### 3.1 세 덩어리

리그는 세 덩어리다. 커맨드는 이 계층에서 나온다 — 어느 덩어리의 어느 노드를 적힌 값으로 놓는가.

| 덩어리 | 노드 | 뜻 |
|---|---|---|
| **포커스** `CharFocus_*` | `_Track` · `_Rotation` · `_Scale` | 대화할 때만 서는 자리. 멀리 있던 인물이 말할 때만 나왔다가 본래 자리로 돌아간다. 실제 무대로는 표현하기 힘든 **작가 허용**의 공간이라 이동 · 회전 · 배율을 다 갖고, 쓰고 나면 `focus_reset`으로 0에 돌린다 |
| **슬롯** `CharSlot_*` | `_Track` · `_Move_X` · `_Move_Y` · `_Rotation` · `_SwayPivot` | 무대 위의 본래 자리(`Track` — `place`)와 그 위의 움직임(`Move_X` · `_Y` — `left` · `right` · `up` · `down`) |
| **초상** `CharacterPortrait_*` | 깊이 · 배역 앵커 · 연기 · 반전 · 그림 | 그 사람의 크기와 연기 |

### 3.2 한 노드 = 한 축

같은 프로퍼티를 두 트윈이 잡으면 서로를 덮는다. 리그 커맨드는 시작할 때 그 노드의 트윈을 끝값으로 보내고(`DOKill(true)`) 제 것을 건다.
그래서 **동시에 돌아야 하는 축은 다른 노드로 나누고, 한 노드에서 겹쳐야 하는 움직임은 클립 하나로 짠다** — 좌우와 상하를 다른 박자로
움직이는 일은 한 채널 안의 x · y 키다. 더하기만 하던 층(`Track_X` · `_Y` · `Track_Move_X` · `_Y`)은 이렇게 해서 걷었다.

슬롯에서는 2026-11-23에 다시 나눴다(`docs/PLAN_move-axes.md`). 본래 자리와 그 위의 움직임이 한 노드(`Track`)에 있으면 `place`를 다시 걸 때
움직임이 덮이고 `move_reset`이 본래 자리까지 0으로 보낸다. 또 한 노드에 트윈 하나라 가로와 세로를 따로 걸 수 없었다. 그래서 `place`(절대)는
`Track`, `left` · `right`는 `Move_X`, `up` · `down`은 `Move_Y`(상대 — 쌓인다)를 쓴다. 움직임 노드는 슬롯에만 있다 — 초상의 이동(`Track_Move`)은
모션의 것이라 섞지 않는다.

### 3.3 전체 계층과 커맨드 대응

```
CharFocus_Track                     focus_move                 ← 포커스: 맨 위 — 무대 값 그대로
 └ CharSlot_Track                   place(절대) — 시작 자리                ← 슬롯: 본래 자리
    └ CharSlot_Move_X               left · right(상대 — 쌓인다) · move_reset   ← 슬롯: 그 위의 움직임
       └ CharSlot_Move_Y            up · down(상대 — 쌓인다) · move_reset
          └ CharFocus_Rotation      focus_rotate               [발밑]   ← 포커스
             └ CharFocus_Scale      focus_scale                [발밑]   ← 포커스
                └ CharSlot_Rotation       (쓰는 커맨드 없음)
                   └ CharSlot_SwayPivot   rotate_by · rotate_reset   [발밑]
                      │
                      └ CharacterPortrait_DepthOffset   size의 Y                ← 초상: 그 사람의 크기
                         └ CharacterPortrait_DepthScale size의 배율    [발밑]   ← 이모지가 서는 자리
                            └ CharacterPortrait_VisualOffset   cast(set_anchor) [발밑]   ← 배역 앵커 · 초점을 재는 노드
                               └ CharacterPortrait_Track         char_move_to — 놓고 머무는 자리
                                  └ ..._Rotation                 모션
                                     └ ..._Track_Move            모션 — 지나가는 몸짓
                                        └ ..._SwayPivot          char_rotate_to · 모션   [발밑]
                                           └ ..._Shake           모션
                                              └ ..._ActingScale  char_scale_to · 모션
                                                 └ ..._Mirror    mirror
                                                    ├ Sprite_Root  fade_in/out  [CG α=0]
                                                    │  └ Sprite_Image   cast · face · pose
                                                    └ Overlay_Root  face_swap   [CG α=0]
                                                       └ Overlay_Image
```

`focus_reset`은 포커스의 셋을 함께 되돌린다. `move_reset`은 슬롯의 움직임(`Move_X` · `_Y`)만 걷는다 — 본래 자리는 그대로다.
`cast`는 슬롯의 자리 · 움직임 · 회전과 초상의 연기 축을 되돌리고, 포커스와 깊이는 건드리지 않는다. `place`는 쌓인 움직임을 걷지 않는다 —
다시 세울 때 대본이 `move_reset 슬롯 0fr` → `place`로 적는다(커맨드 하나가 한 노드를 놓는다).

### 3.4 깊이가 초상에 있는 이유 — 슬롯을 곱하지 않는다

깊이(`DepthOffset` · `DepthScale`)는 슬롯 아래, 배역 앵커 바로 위다. 곱하는 것은 **그 사람**(배역 앵커 · 연기 · 그림)이고, 슬롯의 자리는 곱하지 않는다.

- `place` · `left` · `right` · `up` · `down`의 값은 어느 크기 · 기울기에서나 **화면 값 그대로**다. `right 1u`는 far에서도 close에서도 화면 1u다. 대본에 적힌 자리를 그대로 읽을 수 있다.
- 크기를 바꾸면 그 사람이 발밑을 중심으로 커지고, 배역 앵커 · 몸짓의 진폭도 함께 커진다(멀리 있는 사람은 덜 흔들린다).
- 이모지가 `DepthScale` 밑에 서므로 머리 위의 이모지도 인물의 크기를 따라간다.
- 초점(가슴 · 얼굴 점)을 재는 노드(`VisualOffset`)가 깊이 아래이므로 크기를 바꾸면 초점도 따라온다.

깊이가 배역 앵커의 **위**인 것도 정한 일이다. 아래에 두면 배역 앵커의 배율(인물마다 0.498 · 5 · 10 …)이 이모지와 크기의 중심에까지 곱해진다.

`size`는 레벨(라벨)의 Y와 배율을 놓거나(`keep_y`면 배율만), 배율을 직접 받는다(`size 슬롯 x1.04` — Y는 건드리지 않는다). 무대를 재지 않는다 —
크기를 바꾸며 어느 지점을 제자리에 두려면 자리 명령으로 따로 적는다. 깊이 배율의 진실은 `presets/depth.json`의 **`level` 커브** 한 장이다
(`DepthLevelLabels` — 라벨은 커브 위의 눈금일 뿐이다).

### 3.5 포커스의 이동은 맨 위, 회전 · 배율은 슬롯 아래

맨 위 노드의 중심은 화면 아래 가운데다. 옆에 선 인물을 거기서 키우면 옆으로 밀리고, 돌리면 호를 그린다. 그래서 **회전 · 배율은 슬롯의 자리
아래에 두어 중심이 그 인물의 발밑이 되게 했다.** 이동은 맨 위라 무대 값 그대로다. 노드는 셋이지만 계층은 하나다 — `focus_reset`이 셋을 함께 걷는다.

### 3.6 스케일이 셋인 이유

같은 곱셈인데 **무엇에 곱해져야 하는가**가 달라서 자리가 다르다.

| 노드 | 커맨드 | 아래에 곱해지는 것 |
|---|---|---|
| `CharFocus_Scale` | `focus_scale` | 그 사람 전부(슬롯의 회전 · 깊이 · 연기) — 작가 허용으로 잠깐 키운다. 발밑 중심 |
| `CharacterPortrait_DepthScale` | `size` | 배역 앵커 · 연기 · 그림 · 이모지 — 그 사람의 크기 |
| `CharacterPortrait_ActingScale` | `char_scale_to` · 모션 | **그림만** — 몸짓의 이동량은 그대로 |

**`ActingScale`이 아래인 것도 의도다.** 연기용 확대가 이동량까지 키우면 "크게 그려진 채로 크게 움직이는" 것이 되어 연기가 아니라 확대가 된다.
`Mirror`는 그 아래 따로 있다 — 합치면 배율 모션이 뒤집힌 부호를 덮는다.

### 3.7 회전이 여럿인 이유

| 노드 | 자리 | 성질 |
|---|---|---|
| `CharFocus_Rotation` | 슬롯의 자리 아래 | 작가 허용의 기울기. 발밑 중심 |
| `CharSlot_SwayPivot` | 슬롯 축 바닥 | `rotate_by` — 무대의 자리는 그대로 두고 그 사람이 기운다 |
| `CharacterPortrait_Rotation` | `Track_Move`의 **위** | 기울면 연기 움직임이 기울기를 **따라간다** |

무대 배치에서는 회전이 이동에 새면 안 된다 — "오른쪽으로 100" 했는데 캐릭터가 기울어 있다고 비스듬히 가면 배치가 무너진다.
그래서 슬롯의 `Track` · `Move_X` · `Move_Y`가 회전들보다 위다. 연기에서는 정반대다. 몸이 기울었으면 그 위에서 일어나는 흔들림도 같이 기울어야 실제 몸처럼 보인다.
그래서 초상의 `Rotation`이 위, `Track_Move`가 아래다. **같은 회전인데 요구가 반대라 자리가 반대다.**

### 3.8 나머지 자리들

**초상의 이동이 둘** — `CharacterPortrait_Track`(회전 **위**, 놓고 머무는 자리 — `char_move_to`)과 `Track_Move`(회전 **아래**, 지나가는 몸짓 — 모션).
머무는 값 위에서 몸짓이 돌 수 있어야 하고, 몸짓은 기울기를 따라야 한다. idle 같은 되풀이 몸짓도 `Track_Move`의 클립으로 짠다.

**`VisualOffset`이 두 축의 이음매** — 깊이 아래, 연기 위. 스프라이트마다 그려진 중심이 다른 것을 보정하는 자리이고, 연기 **위**에 있어서
연기가 보정된 위치를 기준으로 일어난다. `cast`가 내는 set_anchor가 여기를 잡는다.

**bottom pivot 여섯 곳** (`CharFocus_Rotation` · `CharFocus_Scale` · `CharSlot_SwayPivot` · `DepthScale` · `VisualOffset` · `Portrait_SwayPivot`)
— 서 있는 인물의 기준점은 발이다(이모지의 로케이터도 `DepthScale`의 발에 붙는다). 작아져도, 흔들려도, 커져도 발이 땅에 붙어 있어야 한다.
`NeedsBottomPivot`이 스키마의 필드인 이유.

**`Sprite_Root` / `Overlay_Root`가 형제이고 둘 다 초기 α=0** — `face_swap`이 위 층을 페이드로 겹쳐 표정을 바꾼다. 등장 전에는 둘 다 안 보인다.

---

## 4. 초점을 지점에 두는 값은 도구가 푼다

옛 `place <초점> <지점>`은 "이 캐릭터의 얼굴을 화면 왼쪽 지점에"처럼 **무대를 읽어 값을 푸는** 커맨드였다. 2026-11-21에 걷었다 —
커맨드는 한 노드를 인자에 적힌 값으로 놓는 것만 둔다. 그 값을 푸는 계산은 코어의 순수 함수 `NodeTargets.TrySolveFocusPlacement`
(`SettledFocusMath.SolveFocusPlacement` 위)이고, 부르는 쪽은 저작 도구다. 대본에는 풀린 값이 적힌다:
본래 자리면 `place 슬롯 x y`, 대화할 때만 서는 자리면 `focus_move 슬롯 x y`.

깊이가 슬롯 아래로 내려가서 이 값은 읽기 쉬워졌다 — 화면 오른쪽 지점(1920 × 0.24 = 460.8px)에 가슴을 두는 `place`는 x가 `11.52u` 그대로다.

`CharacterRigRefs.SettledDepthScale`은 남는다 — 트윈이 도는 중에도 "끝나면 어느 배율인가"를 착란원(`depth_focus_on`) 계산이 읽는다.
트윈 중에는 현재 스케일이 계속 변하므로, 현재값을 읽으면 매 프레임 흔들린다.

---

## 5. 합치면 안 되는 것

이름이 비슷해서 중복으로 보이는 쌍들이다. **전부 다른 노드여야 한다.**

| 합치고 싶어지는 것 | 합치면 생기는 일 |
|---|---|
| `CharFocus_Track` + `CharSlot_Track` | 나온 자리와 본래 자리가 한 노드에 있으면 **돌아갈 곳이 사라진다** |
| `CharSlot_Track` + `CharSlot_Move_*` | 본래 자리와 그 위의 움직임이 한 노드면 `place`가 움직임을 덮고, `move_reset`이 본래 자리까지 0으로 보낸다 |
| `CharSlot_Move_X` + `_Y` | 한 노드에 트윈 하나 — 가로와 세로를 따로 걸 수 없다(같은 때의 둘은 뒤 것이 앞 것을 끝값으로 보낸다) |
| `DepthScale` + `ActingScale` | 연기 확대가 배역 앵커 · 이모지까지 키운다 — 연기가 확대로 보인다 |
| `DepthScale`을 슬롯 위로 | 적힌 자리가 크기만큼 곱해진다 — `right 1u`가 크기마다 다른 거리가 된다 |
| `ActingScale` + `Mirror` | 배율 모션이 뒤집힌 부호를 덮는다 |
| `CharSlot_SwayPivot` + `Portrait_Rotation` | 슬롯 회전이 이동 방향을 비틀거나, 연기가 몸 기울기를 무시한다 |
| 포커스의 회전 · 배율을 맨 위로 | 옆에 선 인물이 화면 아래 가운데를 중심으로 커지고 돈다 — 옆으로 밀리고 호를 그린다 |

**이 사고는 전부 "에러"가 아니라 "연출이 좀 밋밋하다"로 나타난다.** 컴파일도 되고
예외도 안 나고 캐릭터도 잘 움직인다. 그래서 원인 추적이 거의 불가능하다.
노드를 줄이고 싶어지면 이 표를 먼저 볼 것.

---

## 6. 노드를 추가·이동할 때

### 6.1 순서를 정하는 질문

새 노드의 자리는 취향이 아니라 두 질문으로 정해진다:

1. **이 노드의 값이 무엇에 곱해져야 하는가?** — 곱해질 대상이 아래로 온다.
2. **이 노드와 동시에 돌 수 있는 축이 있는가?** — 있으면 반드시 다른 노드로.

### 6.2 고쳐야 하는 자리 (현재 여섯 곳)

`CharacterRigDefinition.cs` 한 파일 안에 다섯, `CharacterRigBuilder.cs`에 하나다.

| 순 | 자리 | 무엇을 정하는가 |
|---|---|---|
| 1 | `CharacterRigSchema.Refs` enum | **계층 정의** — 이 그래프에 어떤 노드가 있는가 |
| 2 | `CharacterRigSchema.Nodes` 배열 | **빌드 정의** — 부모·컴포넌트 요구·초기 α |
| 3 | `CharacterRigNode` enum | **커맨드 어휘** — 커맨드가 겨눌 수 있는 대상 |
| 4 | `CharacterRigRefs`의 필드 | **실행 계층** — 해석된 런타임 참조 |
| 5 | `CharacterRigRefsExtensions.GetComponent` | 3 → 4 해석 |
| 6 | `CharacterRigBuilder.BuildRefs` | 1 → 4 바인딩 |

**이 반복은 중복이 아니라 의도다.** 넷은 서로 다른 것을 정한다:

* **존재하는 노드 ≠ 겨눌 수 있는 노드.** 1과 3이 지금 22개로 겹치는 것은 우연이고,
  타입은 그 둘이 갈라질 수 있음을 표현한다. 하나로 합치면 구조용 보조 노드를 추가하는
  순간 **커맨드가 그것을 겨눌 수 있게 된다.** `CharacterRigNode`은 API 표면이다 —
  화면 효과 리그에 이 enum이 없는 것이 그 증거다(거기서는 `ControllerKind`로 겨눈다).
* **필드 이름 = 하이어라키 오브젝트 이름.** `refs.CharFocus_Scale`을 보고 하이어라키에서
  `<role>CharFocus_Scale`을 이름 대조만으로 찾을 수 있다. 딕셔너리로 접으면 이 성질이
  사라지고, Unity에서 계층을 눈으로 좇는 디버깅이 어려워진다.
* **레포 전체가 같은 관용구를 쓴다.** `UIBase`의 enum 키 참조 바인딩
  (`UIRefValidation.AppendMissing<TRef> where TRef : struct, Enum`)과 같은 모양이고,
  리그 넷이 전부 `Definition` · `Builder` · `Registry` · `Resolver`로 선다.

> ⚠ **다만 넷 사이의 대응은 아직 검사되지 않는다.** `Refs`에 노드를 넣고
> `CharacterRigNode`에 안 넣어도, `GetComponent`의 switch에서 빠뜨려도 컴파일은 통과한다.
> 경계는 유지하되 **대응만 고정하는** 테스트가 필요하다 —
> `모든_Refs가_Target으로_겨눌_수_있다` · `모든_Target이_Refs를_해석한다`.
> (`RigSchemaTreeUnityParityTests`의 `실제_리그에_있는_노드가_덤프에서_빠지지_않았다`와 같은 모양이다.)

### 6.3 고친 뒤 확인할 것

1. `PresentationTuningExporter`로 `rig-schemas.json` 재덤프 — 안 하면 코어 폴드가 옛 계층을 본다
   (`Ked/U12/Export Presentation Tuning Dump`). 같은 때 고칠 것: 노드 이름이 든 코어의 상수(`NodeTargets` · `StageReducer.Anchor`의 리셋 목록 ·
   `MotionRules`) · 시험의 합성 리그 · 모션 클립(`.anim`의 바인딩 경로 — `Ked/Motion/Create Authoring Rig`로 제작 리그를 다시 세운다) · `motions.json`
2. `RigSchemaTreeUnityParityTests` — 덤프와 실제 리그가 같은지
3. 등가성 하네스 랩드스킵 재판정 — 리포트가 변경 전과 같은지
   (절차적 축이 안 섞이도록 랩드스킵이 기준 프로토콜이다)

---

## 참고

* [ASSEMBLY-BOUNDARY.md](../../ASSEMBLY-BOUNDARY.md) — 어셈블리 경계
* [target-boundary.md](../../../Core/Documentation~/target-boundary.md) — 목표 계산 규약
* [SCOPE-BOUNDARY.md](../../../../../SCOPE-BOUNDARY.md) — 저장소 경계
