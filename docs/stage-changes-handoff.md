# 런타임 밖에 반영할 것 — 화면 덮개 · 어둡게 · 컷인 · 팝업 · 무대 마스크 · 줄의 자리 · 명령 이름 · 옛 표기 걷기 (2026-11-17 ~ 22)

> **2026-11-22 — 줄의 앵커 N5: `<<step>>`과 대사 줄 앞의 at 없는 명령을 걷었다. 소비처가 할 일은 §5** — 이 아래 §1.4의 "줄 머리 · 무대 스텝" 이야기는 그 전의 것이다.

> 상태: **반영 중** — 런타임은 끝났고(공개 런타임 `refactor/actor-view`), 작업공간이 포크 · 줄의 자리를 반영했다(§4, 2026-11-17). **명령 이름을 바꿨다(§2)** — 소비처는 아직이다. 덮개 · 어둡게 카드와 툴 · 추출기도 아직이다.
> 반영하는 쪽은 이 문서 하나를 보고 일을 시작한다. 반영을 마치면 §4 표에 적는다.
> 대사창 초상(2026-11-18 — `portrait` · `#portrait`)은 따로 적었다: [portrait-handoff.md](portrait-handoff.md).
>
> 근거 계획서: [PLAN_screen-cover.md](PLAN_screen-cover.md)(덮개 · 어둡게) · [PLAN_stage-visibility.md](PLAN_stage-visibility.md)(컷인 · 팝업 · 무대 마스크) · [PLAN_stage-code-homes.md](PLAN_stage-code-homes.md)(코드의 집 · 명령 이름 §6) · [PLAN_line-beat.md](PLAN_line-beat.md)(줄의 자리 — 진행 중, §1.4).

## 1. 대본에서 바뀐 것

### 1.1 새 어휘 — 덮개 · 어둡게

```
<<screen_cover [프리셋=black] [길이=0.5s] [이징]>>        화면 전체(무대 셋 · 컷인 · 팝업)를 한 색으로 덮는다 — 기다린다(덮일 때까지 다음으로 가지 않는다)
<<screen_cover_clear [길이=0.5s] [이징]>>                   걷는다 — 기다리지 않는다
<<stage_dim <층|all> <세기> [길이=0.3s] [이징] [무대=stage00]>>   층마다 선 검은 판 — all = 그 무대 전체, far = 배경만. 세기 0이면 걷는다
<<stage_dim_reset [길이=0.3s] [이징]>>                     모든 무대 · 층의 판을 걷는다
```

- 프리셋은 `black` · `white`(`ScreenCoverPresetDB`). 덮인 동안 무대를 바꾸고 걷는다.
- 어둡게는 캐릭터의 색을 바꾸지 않는다 — 판이 위에서 가린다. 그 무대의 마스크에 잘린다(stage_mask로 좁힌 영역 안만 어둡게 할 수 있다).
- 쓰는 법과 재생 방식(보통 · 빨리 넘기기 · 되감기)은 PLAN_screen-cover.md §3, 데모 대본의 "화면 덮개" · "어둡게" 절.

### 1.2 동작이 바뀐 것 — 같은 어휘, 다른 결과

| 어휘 | 전 | 지금 | 대본이 할 일 |
|---|---|---|---|
| `popup` · `popup_close` | 뒷막(거의 검정 0.47)과 그림이 한 페이드로 함께 | **그림 하나**(그 무대의 close 층 리그). 뒷막 없다 | 뒤를 어둡게 깔려면 대본이 쓴다 — `<<stage_dim front 0.47 0.45s Linear 무대>>` 다음 `popup`, `popup_close` 다음 `<<stage_dim front 0 0.3s Linear 무대>>`. `all`은 그림 앞의 판이라 그림까지 어둡다 — `front`를 쓴다 |
| 열린 팝업에 다시 `popup` | 그림 · 자리만 바로 바뀜 | 그림 리그를 새로 세운다 — 새 그림이 페이드 인 | 없다(보이는 것이 조금 다르다) |
| `cutin_close` | 창을 배율 0으로 남김 | 창은 원래 자세로, 그 무대는 **마스크가 숨긴다**. 컷인 판은 무대 안의 리그(`{무대}_cutin_panel`)로 **남는다** | 같은 무대를 `stage_mask` 인서트로 다시 쓰기 전에 판을 치운다 — `<<bg_fade_out stage01_cutin_panel 0fr>>`(인물을 치우는 것과 같다) |
| `stage_mask`(옛 `tx`) | 그 무대의 컷인 · 팝업을 먼저 껐다(퇴장 없이) | **마스크만** 움직인다. 컷인 판 · 팝업 그림도 무대 안이라 함께 잘린다. 열린 컷인은 연 채로 남는다 | 열린 컷인 · 팝업 위에 `stage_mask`를 썼던 대본은 뒤에 `cutin_close` · `popup_close`가 있어야 정리된다 |
| `stage_mask_reset`(옛 `tx_stage_mask_clear`) | 모든 무대의 컷인 · 팝업을 껐다 | 마스크만 — 모든 무대가 다 보인다 | 덮는 무대에 남은 것(인물 · 컷인 판 · 팝업 그림)이 다 보인다 — 쓰기 전에 치운다 |
| 컷인 판 아래 외곽선 | 판 그림의 것 | 마스크 테두리가 다시 그린다(판이 마스크 안이라) | 없다 |

### 1.3 폴드(코어)에서 알아 둘 것

- 컷인 판 · 팝업 그림은 캐릭터 리그로 서지만 **폴드 밖**이다 — 코어는 끝 상태 표(`CutIns` · `Popups`)로만 안다. 툴 미리보기는 그 표를 그대로 쓴다(표는 바뀌지 않았다).
- 대본이 판을 치우는 줄(`bg_fade_out stage01_cutin_panel …`)은 폴드가 그 슬롯을 몰라 **Unhandled**로 남는다 — 불일치가 아니다.
- 어둡기 판 · 덮개 · 마스크는 지금처럼 접지 않는다(`Core/Documentation~/target-boundary.md`).
- 코어의 코드는 이번 일로 바뀌지 않았다(주석 · 문서만) — 코어 사본(모션 툴)은 이 일 때문에 동기화할 것이 없다. 줄의 자리(§1.4)는 코어를 바꾼다.

### 1.4 줄의 자리 — `<<at>>` · `[beat]` · `#auto=길이` (PLAN_line-beat.md, 진행 중)

> 뒤에 바뀌었다 — [PLAN_line-anchor.md](PLAN_line-anchor.md)(줄의 앵커): `start` 앵커 · 어느 앵커에서나 `pause` · `<<gap>>` · 앞 앵커(2026-11-21), 그리고 `<<step>>` · at 없는 명령 걷기(2026-11-22, §5).

글 안에 명령을 두던 `[cmd="명령"/]`을 **자리와 명령으로 가른다** — 번역 파일에 명령이 실리지 않게. 런타임은 L1–L5까지 했다 — 옛 `[cmd]`는 걷었다.
L3부터는 옛 대본이 그대로 돌지 않는다 — 대사 줄 머리의 `pause`가 오류가 되고 `<<Nfr>>`은 모르는 명령이다. 작업공간은 L3을 L4(다시 낸 대본)와 **함께** 들인다.

| | 꼴 | 상태 |
|---|---|---|
| 자리 표지 | 글 안 `[beat=a/]`(이름은 영문 소문자 · 숫자, 그 줄 안에서만) + 줄 앞 `<<at a 명령 한 줄>>`. `<<at end …>>`는 타자기 끝 | **L2 — 된다.** 순서: 머리(at 없는 명령) → 표지(글자 자리 → 글의 표지 순서 → 쓴 순서) → end. 폴드도 같은 순서(코어 `LineBeatSchedule`) |
| 옛 인라인 | `[cmd="명령"/]` | **L5 — 걷었다.** 재생도 폴드도 읽지 않는다 — Yarn은 모르는 마크업을 글에서 지우기만 하므로, 남은 것은 소리 없이 빠진다 |
| 자동 줄의 머무름 | 줄 태그 `#auto=1.8s`(표기는 `pause`와 같다 — `54fr`도) | **L1 — 된다.** 줄이 들어온 때부터 그만큼 머물고, 글이 다 보이면 넘긴다 |
| 대사 줄 머리의 기다림 | 줄 머리의 `pause` · `screen_cover` | **L3 — 금지했다**(런타임). 재생은 오류를 남기고 기다리지 않고 실행한다 — `pause`는 없는 것과 같고, 덮기는 덮되 기다리지 않는다. 폴드의 원문 검사(코어 추출기)도 경고한다. 시간으로 짜는 일은 줄 앞의 `<<step>>`으로 |
| `<<Nfr>>` 별칭 | `<<12fr>>` | **L3 — 걷었다**(런타임). 모르는 명령이 된다 — `<<pause 12fr>>`로 |

- 자리의 명령에는 기다리는 명령(`pause` · `screen_cover`)을 쓸 수 없다.
- `<<at>>` 다음이 대사 줄이 아니면(무대 스텝 · 노드 끝) 경고하고 그 스텝의 머리 뒤에 실행한다. 선택지(`->`)를 건너면 다음 대사 줄로 간다 — 원문 검사(코어 추출기의 경고)가 막을 일이다.
- 번역문에 표지가 빠지면 그 명령은 줄 끝에서 실행하고 경고한다. 원문의 표지 목록과 집합 · 순서가 같은지는 번역 검사 도구가 본다(PLAN_line-beat.md R4).

## 2. 명령 이름 — 바꿨다 (소유자, 2026-11-17)

무대 · 층 단위로 작동하는 명령의 이름을 `stage_`로 바꿨다. **옛 이름은 받지 않는다**(문법 표는 별칭을 두지 않는다) — 바꾼 런타임을 들이는 날 대본 · 카드 · 도구도 함께 바꾼다.
원칙: `screen_`은 화면 전체(화면 효과 리그 — 덮개 · 플래시 · 비네트 · 노이즈, 그대로), `stage_`는 무대 · 층 단위(무대 리그).

| 옛 이름 | 새 이름 | 인자(그대로) |
|---|---|---|
| `tx` | **`stage_mask`** | `<프리셋> [무대=stage01] [길이]` |
| `tx_stage_mask_clear` | **`stage_mask_reset`** | 없음(모든 무대) |
| `screen_dim` | **`stage_dim`** | `<층\|all> <세기> [길이=0.3s] [이징] [무대=stage00]` |
| `screen_dim_reset` | **`stage_dim_reset`** | `[길이=0.3s] [이징]` |
| `screen_blur` | **`stage_blur`** | `[층=mid] [양=1] [무대=stage00]` |
| `screen_blur_reset` | **`stage_blur_reset`** | 없음 |

- 마스크 지우기는 끝말도 바꿨다(`_clear` → `_reset`). `_clear`는 얹은 효과를 걷을 때(`screen_*_clear` · `focus_clear`), `_reset`은 원래 상태로 돌릴 때(`shot_reset` · `size_reset` …) 쓴다 — 이 명령은 모든 무대를 다 보이는 모양(장면 경계의 기준 모양)으로 돌린다.
- 마스크 프리셋 이름은 그대로다(`iris_in` · `slant_in` …). 프리셋 이름 앞의 `tx_`를 걷어 주던 코드(`StageMaskMotionPresetDBSO.NormalizeKey`)는 걷었다 — `<<stage_mask tx_iris_in>>`처럼 쓰면 모르는 프리셋이다.
- 동작은 바뀌지 않았다. 런타임은 문법 표 · Yarn 브리지 등록 · 데모 대본 · 문서 · 시험을 바꿨다.
- 코어 폴드는 이 이름들을 따로 알지 않는다(모르는 이름은 Unhandled) — 코어 코드는 그대로다(주석 · `target-boundary.md` 회계 표의 이름만).
- 소비처에서 바꿀 곳(2026-11-17 셈): 작업공간 대본 `<<tx` 31 · `<<tx_stage_mask_clear` 1(`origin/dev`), `screen_dim` · `screen_blur` 0 · 무대 추출기 `effects.py`의 `screen_blur` · 저작 툴은 확인 못 함.

## 3. 소비처마다 할 일

### 3.1 작업공간 — `ked-vn-workspace`

**런타임 포크(`runtime/`)를 먼저 맞춘다.** 포크는 공개 런타임 `refactor/actor-view` `85d7dc4`까지 들였다(`runtime/FORK.md` §1). 그 뒤 공개 런타임에 36커밋이 더 있다(`85d7dc4..bcbe5d8a`, 2026-11-17) — 덮개 · 어둡게(S1–S5), 코드의 집(R1–R4), 보이는 범위(V1–V5), 블러 폴더, 시험 고침, 줄의 자리(L1–L3, §1.4). 들인 뒤 `FORK.md`에 적는다.
- 줄의 자리가 포크의 손댄 파일과 겹친다: `LinePresentationFlow.cs`(줄 머리 `Play` 앞에 `ReleaseLineWaits` 한 줄 — 메신저 · 글 카드 줄도 이 흐름을 지나면 같이 걸린다. 인라인 트랙의 `Begin` · `PlayRemaining`은 이름 · 모양이 그대로다), `CommandBridge.cs`(`at` 등록, `<<Nfr>>` 등록이 빠짐, `TryParseInline`의 인자가 `StageCommand`로), `StageStepFlow.cs`(생성자에 인라인 트랙 — 남은 `at`), 조립(`SampleBootstrap`). L1 · L2까지는 대본이 그대로 돌고, L3(`bcbe5d8a`)은 다시 낸 대본과 함께다(아래).
- 포크에서 조립을 고친 곳이 있으면 겹친다: `SampleBootstrap`(팩토리 셋 — 무대 리그 · 무대 틀 · 블러, 덮개 프리셋, 팝업 프리셋이 파서로), 무대 틀(창만 남음), 파서 생성자(팝업 프리셋).
- 프리셋 에셋: `ScreenCoverPresetDB.asset`(새로) · `PopupPreset.asset`(필드가 바뀜 — 배율 · 열기 · 닫기 길이) · `CutInPreset`(외곽선 색 필드가 더해짐 — 없으면 기본값).

**연출 카드(`catalog/cards.json`)**

| 카드 | 지금 | 바꿀 것 |
|---|---|---|
| B01 장소 바꾸기 · B02 시간 건너뛰기 · B03 장제목 · 장소 카드(암전) | 검은 배경 슬롯(`bg_top`)을 `fade`로 흉내 낸다 | `screen_cover black <길이>` … `screen_cover_clear <길이>`. 장면 머리의 `bg_black` · `bg_dim` · `bg_top` 슬롯 세우기가 필요 없어진다. 잰 길이(덮이는 데 · 머무름 · 걷히는 데)는 카드 글의 값 그대로 |
| W01 회상 넘나들기(흰 화면) | `screen_flash memory` | 머무는 흰 화면이면 `screen_cover white` … `screen_cover_clear`. 번쩍이고 끝나는 것이면 그대로 |
| D01 속말 · D02 회상 · 설명 살짝 · D03 어두운 바탕에 자막 · D04 배경만 | 흉내 — 검은 슬롯 `bg_dim`을 `fade`로 올리거나(D01 · D02), 배경 `bg_main`을 투명하게 해 뒤의 검정을 비친다(D03 · D04) | `stage_dim all <세기>`(D01 · D02 · D03) · `stage_dim far <세기>`(D04 — 인물은 밝다). 잰 밝기 → 세기 = 1 − 밝기(선형 색 공간에서 판의 투명도 α는 밝기를 ×(1 − α)로 만든다). 배경을 투명하게 하는 흉내는 걷는다 |
| 컷인 · 팝업 | 카드가 없다(대본에 직접 — 아래) | 쓸 것이면 카드로: 팝업은 `stage_dim front` 쌍과 함께, 컷인 뒤 인서트는 판 치우기와 함께 |

**대본(`runtime/Assets/Workspace/Dialogue/`)** — 2026-11-17 셈: `cutin_open` 50 · `cutin_close` 50 · `tx` 31(→ `stage_mask`, §2) · `popup` 18 · `popup_close` 18.
- `popup` 18곳: 뒤가 어두웠던 자리면 `stage_dim front` 쌍을 더한다(원본에서 팝업 뒤는 어두웠다 — PLAN_popup.md §1).
- `tx`(→ `stage_mask`) 31곳 중 같은 무대에서 컷인 뒤 인서트를 여는 곳: 판 치우기를 더한다. 열린 컷인 · 팝업 위의 `stage_mask`: 정리 줄이 있는지 본다.
- 덮기 135곳 · 어둡게 223줄(PLAN_screen-cover.md §1)을 카드를 따라 새 어휘로.
- 대본을 내는 도구(`tools/episode` · `tools/stage` · `tools/catalog`)가 카드 명령을 그대로 찍으므로 카드를 먼저 고친다.
- 작업공간의 규칙대로 "통과"는 녹화 비교로만 판정한다.

**줄의 자리(§1.4)** — PLAN_line-beat.md L4. 런타임 L3(줄 머리 기다림 금지 · `<<Nfr>>` 걷기)과 **함께** 들인다 — 따로 들이면 줄 머리 `pause` 1,979곳이 오류가 된다.
- 대본을 내는 도구(`tools/stage/script.py` 등)가 다시 낸다: `[cmd]` 986곳(+ 9장 초안 41) → `<<at>>` + `[beat]`, 줄 머리의 시각(뒤 명령을 늦추던 `pause` 1,919) → 표지 또는 줄 앞 `<<step>>`, 자동 줄을 붙잡던 `pause` 60 → `#auto=길이`.
- 런타임 포크는 L1 · L2를 지금 들여도 대본이 그대로 돈다(옛 꼴을 받는다). L3은 다시 낸 대본과 함께 — 그 전에 들이면 줄 머리 `pause` 1,979곳이 오류를 내고 기다리지 않는다.
- 스펙의 `wait`: 무대 파서가 `pause` · `screen_cover`(덮기)에 세운다(전에는 두 커맨드가 기다림을 코드에 박아 두었다). 포크에서 스펙을 직접 만드는 곳이 있으면 `wait = true`를 넣는다.

### 3.2 저작 툴 — `ked-story-compositor`

이 세션에서 접근하지 못했다(무엇을 고쳐야 하는지는 이 문서로 넘긴다).
- 대본을 내는 쪽: 새 어휘(§1.1), 팝업 뒤 깔기 · 판 치우기(§1.2), 새 이름(§2 — 옛 이름은 받지 않는다). 툴은 정본 어휘만 낸다(문법 표 주석).
- 미리보기: 컷인 · 팝업은 끝 상태 표(`CutIns` · `Popups`)로 그릴 수 있다 — 표는 바뀌지 않았다. 덮개 · 어둡기 판은 폴드 밖이다.

### 3.3 무대 추출기 — `ked-staging-extractor`

- `ked_staging/effects.py`가 `screen_blur`를 낸다 — `stage_blur`로 고친다(§2 — 옛 이름은 받지 않는다).
- 줄 안의 시각(`timing.py`)을 낼 때 §1.4의 규칙을 따른다 — 대사 줄 머리에는 시간이 없다(표지 또는 줄 앞 무대 스텝), 자동 줄의 머무름은 `#auto=길이`.
- 컷인 · 팝업 · `stage_mask` · 덮개 · 어둡게는 내지 않는다(2026-11-17 확인). 원본의 암전 · 어둡게를 뽑게 되면 §1.1의 어휘로 낸다.

### 3.4 연출 감독 — `ked-story-director`

- 이 어휘를 쓰지 않는다(2026-11-17 확인). 계약에 무대 어휘가 들어오면 §2의 이름 결정을 따른다.

### 3.5 모션 툴 — `ked-motion-tool-characterRig`

- 코어 사본이다. 덮개 · 컷인 · 팝업 · 무대 마스크와 명령 이름 일로는 코어의 코드가 바뀌지 않았다(§1.3 · §2).
- 줄의 자리(§1.4)는 코어를 바꿨다 — `AutoAdvanceTag` · `LineBeatSchedule`(새로) · `YarnCommandTextExtractor` · `StageReducer`(`Nfr` 갈래). 툴의 코드(`src/Mt.*`)는 이것들을 부르지 않는다(2026-11-17 확인) — 툴에서 할 일은 없다. 툴의 사본은 원래 옛 `ked-stage-runtime`(2026-10-29)에 이모지 파일만 더 들인 판이라, 통째로 맞출지는 툴 쪽에서 따로 정한다(툴의 `src/Ked.Presentation.Core/vendoring.md`).

## 4. 반영 기록

| 날 | 소비처 | 무엇을 | 어디(커밋) |
|---|---|---|---|
| 11-17 | 작업공간 `ked-vn-workspace` | 런타임 포크를 `85d7dc4..bc167638`(L2까지, 35커밋) 들였다 — 둘 다 고친 9개는 3-way, 충돌 4(`LinePresentationFlow` 주석 · `StageCutInSlot` · `ScreenEffectCommandFactory` · `StageCommandParser.CutIn`). 포크의 `cutin_panel`은 V2에서 판이 무대 안의 리그가 되어 `StageCutInSlot.SetPanel`을 걷고, 파서가 `{무대}_cutin_panel`에 `SetSpriteCommandSpecCharR`을 내게 다시 이었다(`cutin_open`의 판 그림과 같은 스펙). 작업공간 씬 20개에 `screenCoverPresetDb`, 작업공간 팝업 프리셋을 새 칸으로(`openSeconds` · `closeSeconds` 0.3 — 뒷막은 원래 투명도 0이라 모습은 같다). Unity 확인 전 | `b87341f` |
| 11-17 | 작업공간 `ked-vn-workspace` | 줄의 자리 L4 — 대본을 내는 도구(`tools/stage/script.py` · `drafts/*/compose.py`)를 고쳐 다시 냈다. `[cmd]` → `<<at>>` + `[beat]`(재현 14편 표지 620 · `<<at>>` 1,110 — 그중 `end` 244, 9장 두 편 표지 83). 대사 줄 머리의 시각 → 0.5초 안은 머리, 그 뒤는 표지, 덮개 · 흰 화면 아래에서 무대를 바꾸는 묶음은 줄 앞 `<<step>>`(31 — 흰 화면은 스텝 경계에서 끊기지 않게 끝날 때까지). 자동 줄의 머무름 → `#auto=길이`(60). 손 대본(`ch05_01`)과 시험 대본도 옮겼다. 연출 카드의 커맨드 예도 새 꼴로. 대본 21개의 대사 줄 머리 기다림 · `[cmd]` · `<<Nfr>>` 0 | `8dee2fd` |
| 11-17 | 작업공간 `ked-vn-workspace` | 런타임 포크에 L3(`bcbe5d8a`)을 L4 대본 다음 커밋으로 들였다 — 충돌 0. 포크 코드가 기다리는 스펙을 직접 만드는 곳은 없다(`wait`를 넣을 자리 없음). Unity 확인 전 | `8c18efd` |

- 커밋 해시는 작업공간 저장소(비공개)의 것이다.
- 작업공간의 `[cmd]`는 0곳이다 — 작업공간 쪽에서는 L5(`[cmd]` 걷기)를 막지 않는다. 다른 소비처(모션 툴의 코어 사본 · 추출기 · 저작 툴)는 따로 본다.
- 작업공간에 아직 반영하지 않은 것: §1.1 덮개(`screen_cover` — B01~B03 · W01, 작업공간 Unity 확인 뒤에 한다) · 어둡게(`screen_dim` — §2 이름이 정해진 뒤). §1.2는 지금 대본에 걸리는 곳이 없다 — 열린 컷인 · 팝업 위의 `tx` 0, 컷인을 쓴 무대를 `tx` 인서트로 다시 여는 곳 0, `tx_stage_mask_clear`는 시험 대본 한 곳뿐.

## 5. 줄의 앵커 N5 — 옛 표기를 걷었다 (2026-11-22)

근거: [PLAN_line-anchor.md](PLAN_line-anchor.md) §9 N5 · §12 「N5에서 한 것」 · 「N5의 Unity 확인과 마무리」. 런타임 `dev`(`feat/drop-old-notation`을 합쳤다). 소비처가 모두 옮긴 것을 소유자가 확인한 뒤에 걷었다.

### 5.1 대본의 규칙 — 지금

| | 규칙 |
|---|---|
| 줄의 명령 | **모두 `<<at 앵커 명령 …>>`이다.** 앵커는 앞 앵커(첫 글자 앞의 `[beat=이름/]`) · `start`(표지 없음 — 대사창이 열리며) · 글 표지 · `end`(표지 없음 — 타자기 끝). 간격은 `<<gap 앵커 길이>>` |
| at 없는 명령 | **노드 끝에만 쓴다** — 붙을 줄이 없이 남은 명령이 암묵 스텝 하나로 쓴 순서대로 실행된다(대사창을 닫고, 기다리는 명령이 끝날 때까지). 대사 줄 앞에 쓰면 **오류이고 실행하지 않는다** — 재생 로그 `[줄의 자리] <<…>> — at 없는 명령이다. 줄의 명령은 <<at 앵커 명령 …>>으로 쓴다(at 없이 쓰는 것은 노드 끝뿐이다). 실행하지 않는다.`, 원문 검사(코어 추출기 · 줄 읽기)도 같은 말을 낸다 |
| at 없이 쓰는 것이 맞는 것 | 바로 실행하는 명령 — 명세 표의 `Immediate`(공개: `surface_layout` · `surface_reset`, 포크: `messenger_open` · `messenger_close`). Yarn의 문 · 흐름(`set` · `declare` · `call` · `wait` · `enum` · `if` · `else` · `jump` · `detour` …). 여기에 `<<at>>`을 붙이면 파서로 넘어가 모르는 명령이 된다 |
| `<<step>>` | **없다** — 등록을 걷었다. 남아 있으면 Yarn이 모르는 명령으로 소리 낸다(원문 검사에서는 at 없는 명령의 오류 하나). 대사창이 열리기 전의 연출은 그 줄의 앞 앵커로, 길이는 `<<gap>>`으로(그 구간의 `pause` · `screen_cover` 길이의 합 — 간격은 앵커가 열린 때부터 잰다) |
| 옛 `<<step>>` 구간의 옮기는 법 | 공개 대본을 옮긴 규칙(작업공간 N4와 같다): 구간 → 그 줄의 첫 글자 앞 표지 `[beat=in/]`(화자 이름 뒤) + `<<at in …>>`, 구간의 맨 끝 `pause` → `<<gap in 합>>`, 그 밖의 `pause`는 `<<at in pause …>>`, 머리 명령 → `<<at start …>>`. 차례가 같으면 끝 상태가 같다 |
| 줄의 보이스 | `<<at start voice 키>>`가 그 줄의 보이스다(백로그의 다시 듣기 키). 다른 앵커의 `voice`는 줄 가운데서 트는 소리다 |

### 5.2 코어에서 바뀐 것 — 코어 사본을 쓰는 곳

| 걷었다 | 대신 |
|---|---|
| `LineBeatSchedule.Build` · `ScheduledCommand` | `LineBeatSchedule.BuildAnchors` · `Flatten` |
| `LineBeatSchedule.LegacyStepName` · `LineAnchor.Legacy` · `BuildAnchors(…, legacySteps)` | 없다 — 앞 앵커는 글의 표지뿐이다 |
| `YarnCommandTextExtractor.StageStepCommand` · `YarnNodeGroups.LegacySteps` · `BareCommands` | 대사 줄 앞의 at 없는 명령은 경고하고 접지 않는다. 노드 끝의 것은 꼬리 그룹(암묵 스텝). 판정은 `IsYarnControl` · `IsYarnStatement`(새로, 공개) · `StageCommandSpecTable.IsImmediate`. 오류의 말은 `UnanchoredProblem` |
| `LineRead.LegacySteps` · `BareCommands` · `LineReadStep.Legacy` · `LineReadCommand.Bare` | `LineRead.Unanchored`(대사 줄 앞의 at 없는 명령 — 줄의 문제에도 적힌다) |
| `command-specs.json`의 `legacyStructure` | 없다(`lineStructure`만) |

추출기가 접는 열도 조금 바뀌었다 — `if` · `detour` · `set` · `surface_*` 같은 것이 명령 열에 실리지 않는다(전에는 Unhandled로 남았다). 선택지(`->`) 앞의 `<<at>>`은 다음 대사 줄로 간다(재생과 같다).

### 5.3 소비처마다 할 일

| 소비처 | 할 일 |
|---|---|
| 작업공간 `ked-vn-workspace` — 포크 | `feat/drop-old-notation`을 들인다. 겹치는 파일: `YarnPlaybackDriver.cs`(`Enqueue(StageCommand, specs)` · `TryDropUnanchored` — `CloseStep` · `TryPlayNextPreBatch` · `PreBatchCount` · `PendingAll`이 없다), `CommandBridge.cs` · `CommandBridge.Control.cs`(`step` 등록 · `CloseStageStep`이 없다, `Forward`가 `Collected`를 문맥으로), `StepHead.cs`(`DropUnanchored`, `HasPreBatch` · `TryPlayNextPreBatch`가 없다), `LinePresentationFlow.cs`(줄에 들어서면 `DropUnanchored`, `start`는 `PlayStart` 뒤에 `Settle`), `StageStepFlow.cs`(`TryPlayTail`), `LineVoiceController.cs`(보이스 키를 `AtCommand`에서). 포크 코드가 드라이버의 옛 이름을 부르면 고친다. 포크의 `StageCommandSpecTable.Fork.cs`는 그대로 — 다시 낸 `command-specs.json`(`runtime/ExportedTuning/`)에서 `legacyStructure`가 빠진다 |
| 작업공간 — 대본 | **`ch05_01` 노드 끝의 `<<step>>` 한 줄을 지운다.** 꼬리 명령 넷은 at 없이 그대로 두면 암묵 스텝 하나로 실행된다. `<<step>>`이 넷 사이에 있었다면 두 구간이 한 묶음이 된다 — 차례와 기다림은 같고, 구간 사이에서 앞 구간의 남은 전환을 마무리하던 경계만 없어진다. 그 밖의 대본은 at 없는 명령이 바로 실행하는 것뿐이어야 한다(2026-11-22 셈: 22곳) — 코어 추출기의 경고 0으로 확인한다 |
| 작업공간 — 도구 | `tools/stage/yarn_check.py` — 대사 줄 앞의 at 없는 명령은 **오류**(바로 실행하는 명령 · Yarn의 문은 뺀다), `<<step>>`은 모르는 명령. 대본을 내는 도구(`script.py` · `anchors.py` · 초안의 `compose.py`)는 `<<step>>`과 at 없는 머리 명령을 내지 않는다(노드 끝의 꼬리만 at 없이). 카드의 커맨드 글도 같다 |
| 작업공간 — 재생 확인 | 첫 줄의 `start`에 `bgm`이 있는 장면 — 전에는 장면 BGM을 `start`의 명령보다 먼저 맞춰 끊었다가 다시 틀 수 있었다(N4 이후의 새 표기 대본). 이제 `start`의 명령 뒤에 맞춘다 — 녹화에서 BGM이 이어지는지 본다. 백로그의 보이스 다시 듣기가 `<<at start voice …>>` 줄에서 된다 |
| 모션 툴 `ked-motion-tool-characterRig` | 코어 사본(R7에서 전체 동기화)에 위 §5.2가 들어온다. 툴의 코드(`src/Mt.*`)는 이 타입들을 부르지 않는다(2026-11-17 확인) — 사본과 사본의 시험만 맞춘다 |
| 추출기 `ked-staging-extractor` | 내는 대본이 §5.1을 따른다 — `<<step>>`을 내지 않고, 대사 줄의 명령은 모두 `<<at 앵커 …>>`, 줄 앞의 시간은 앞 앵커 표지와 `<<gap>>`으로. §3.3의 "줄 앞 무대 스텝"은 앞 앵커로 읽는다 |
| 저작 툴 `ked-story-compositor` | 내는 대본이 §5.1을 따른다. **이 저장소의 예제 대본(데모 · festival_eve)은 이 툴의 `samples/RuntimeSamples`가 정본이다(README)** — 여기서 새 표기로 옮겼으므로 정본도 같게 옮겨야 다음 저장 때 옛 표기로 돌아가지 않는다. **데모의 대사 줄 93개에 `#line:`을 새로 붙였다**(이제 116줄 모두) — 정본에도 같은 ID로 옮긴다. 다시 내면서 ID를 새로 지으면 저장 · 되감기 · 백로그의 좌표와 연출 고치기 툴의 주소가 또 바뀐다. 오라클 `Vn.StageOracle`과 코어 사본은 §5.2 — `Build` · `LegacySteps` · `BareCommands`를 부르는 곳을 고친다 |
| 연출 고치기 툴 `ked-story-tools` | 코어 사본(`src/Ked.Presentation.Core` · 시험 사본 `tests/Ked.Presentation.Core.Tests`)을 다시 들인다. `src/St.Authoring/Editing/LineEditor.cs`가 `LegacySteps` · `BareCommands`로 옛 표기의 줄을 막는다 — `LineRead.Unanchored`로 바꾼다 — 툴은 묶음의 한 줄씩 고치므로 at 없는 명령 줄은 그대로 남는다(실행되지 않는다). 그 줄을 막을지, 고치며 그 명령을 보이게 할지 정한다. 명세 표 JSON의 `legacyStructure`를 읽는 곳이 있으면 뺀다. 런타임 데모 대본의 대사 줄은 이제 모두 `#line:`이 있다(계획서 U4) |
| 연출 감독 `ked-story-director` | 할 일 없다 — 무대 어휘를 쓰지 않는다 |

### 5.4 반영 기록

| 날 | 소비처 | 무엇을 | 어디(커밋) |
|---|---|---|---|
