# ExportedTuning — 스키마 문서 (U12-전체)

이 폴더는 `Ked/U12/Export Presentation Tuning Dump` 메뉴(또는 batchmode
`-executeMethod PresentationTuningExporter.ExportAll`)가 생성한다.
**VnTool은 이 문서만 보고 모든 파일을 해석할 수 있어야 한다.** 해석이 안 되는
필드를 만나면 이 문서의 결함이다 — 추측하지 말고 문서를 고칠 것.

이 JSON들의 스키마가 곧 `Ked.Presentation.Core`의 `Tuning/` 타입 모양이 된다
(phase2-design-brief.md §6.2 코어 계약 2).

## 공통 규약

- **좌표 단위는 전부 기준 해상도 픽셀**이다. 기준 해상도는 `base-resolution.json`.
  호스트(유니티/VnTool)가 뷰포트로 스케일한다 — 데이터에는 스케일이 없다.
- y축은 위가 +다 (유니티 UI 좌표).
- 색은 `{r,g,b,a}` 0~1 float.
- `Vector2`는 `{x,y}`, `Vector3`은 `{x,y,z}`.
- 값은 내보낸 시점의 실물 값 그대로다. 덤프 과정에서 튜닝·보정하지 않았다.

## base-resolution.json

프레젠테이션 캔버스(`PresentationUIRoot`의 부모 `CanvasScaler`)에서 읽었다.

| 필드 | 의미 |
|---|---|
| `canvasName` | 캔버스 오브젝트 이름 (정보용) |
| `uiScaleMode` | `ScaleWithScreenSize`여야 한다. 아니면 export가 경고를 남긴다 |
| `referenceResolution` | **기준 해상도.** 모든 픽셀 좌표의 기준. UnitToken의 기준 폭(1u = x ÷ 48)도 이것 |
| `matchWidthOrHeight` | CanvasScaler match (0=폭, 1=높이). 뷰포트 스케일 시 참고 |

## rig-schemas.json

리그 4종(character / background / overlay / screenEffect)의 노드 트리 초기 상태.
**빌더 로직을 전사한 것이 아니라, 게임이 쓰는 실제 프리팹·빌더로 리그를 세워
실물 `RectTransform`에서 읽은 값이다.** `sourcePrefab`이 비어 있으면 프리팹 없이
스키마 베이크로 세운 것이고, 그것이 곧 런타임 경로다.

- `capturedUnderParentSize`: 캡처 시 리그 루트가 딛고 있던 부모 rect 크기
  (= 기준 해상도). 스트레치 노드의 `measuredRectSize`는 이 부모에 종속된 파생값이다.
- `rigs[].nodes[]`: 노드별 상태. `id`는 스키마 enum 이름 그대로(논리 키로 쓸 것),
  `parent`는 부모 노드의 `id`. 루트는 `__root`(parent 없음)이다.

노드 필드는 코어 `RectNodeState`와 1:1이다:

| 필드 | 의미 · 단위 |
|---|---|
| `anchoredPosition` | 앵커 기준점→pivot 오프셋, px. **커맨드가 위치를 쓰는 자리** |
| `anchorMin` / `anchorMax` | 부모 rect에 대한 비율 0~1. (0,0)-(1,1)=스트레치, min==max=고정점 |
| `pivot` | 자기 rect 안의 기준점 비율. (0.5,0)=바닥 가운데 |
| `sizeDelta` | px. 스트레치면 앵커 간격 대비 증감, 고정 앵커면 크기 그 자체 |
| `localScale` | 배율. z는 항상 1 |
| `localEulerAngles` | 도(degree). 적용 순서 Z→X→Y (Unity Quaternion.Euler) |
| `measuredRectSize` | 캡처 시점 실측 rect 크기, px. **파생값·정보용** — 재현 입력으로 쓰지 말 것 |

위치 재현 수식(앵커 수학 포함)은 코어가 이미 구현했다:
`Ked.Presentation.Core/Transforms/RectChainMath.cs` + 대조 하네스,
규약 문서는 `Ked.Presentation.Core/Documentation~/transform-math-and-epsilon.md`.

`NeedsImage`/`NeedsCanvasGroup` 같은 구성 플래그는 좌표 재현에 불필요해 담지 않았다.
필요해지면 코드 스키마(`CharacterRigDefinition.cs` 등)가 원천이다.

## presets/*.json

유니티 직렬화(`EditorJsonUtility`) 그대로다 — 전사 실수를 없애기 위해서다.
공통 껍데기: `{"MonoBehaviour": { ..., <아래 필드들> }}`. `m_*` 필드는 유니티
내부 메타데이터이니 무시할 것. `entries[]`를 가진 파일은 전부 `key` 문자열이
Yarn 커맨드에서 쓰는 프리셋 키다.

`AnimationCurve` 필드는 `{ "m_Curve": [키프레임…], … }` 모양이다. 키프레임은
`{time, value, inSlope, outSlope, …}`이고 구간 보간은 Hermite. 키 범위 밖은
끝 키의 기울기로 외삽한다(코드 주석 명시).

### depth.json (`CharacterDepthTuningSO`)

`presets` — far/back/mid/front/close/exp1/exp2 7단:

| 필드 | 의미 |
|---|---|
| `depthY` | 깊이 이동, px. 실사용은 y 성분 (CharSlot_DepthY의 anchoredPosition으로 감) |
| `depthScale` | 깊이 배율 (CharSlot_DepthScale의 localScale) |
| `preserveFocusPreset` | 스케일 때 고정할 초점(enum: 0=Feet 1=Body 2=Bust 3=Face — `CharacterFocusPreset`) |
| `preserveFocusOffset` | 그 초점의 추가 보정, px |

`levelTuning` — 연속 depth level(0~10) 입력용: `yCurve`/`scaleCurve`(AnimationCurve,
level→px / level→배율), 구간별 preserveFocus 4개(≤2.5 far, ≤6.5 mid, ≤8.5 close, 그 위 front).

### role-anchor.json (`RoleAnchorTuningDBSO`)

`entries[]`: `key`(캐릭터 또는 `캐릭터:포즈`), `offset`(px, 배치 보정),
`visualScale`(캐릭터별 추가 배율).

### focus-tuning.json (`CharacterFocusTuningDBSO`)

focus 지점 해석: 최종 오프셋 = `baseOffsets[preset]` + (엔트리 있으면
`defaultOffset` + `offsets[preset]`) + 커맨드 인자 오프셋. 전부 px.
`baseOffsets`/`offsets`는 Feet/Body/Bust/Face 각각의 Vector2.

### visual-focus.json (`CharacterVisualFocusPresetDBSO`)

캐릭터 시각 포커스(dim/rim) 프리셋. `dim`(0~1), `dimTintColor`,
`outerRim`/`innerRim`(0~1), `outerRimColor`/`innerRimColor`.
**상태는 순수 데이터, 그리기는 셰이더** — 2b 등급: dim은 근사(표시), rim은 미표시(뱃지).

### mask-motion.json (`StageMaskMotionPresetDBSO`)

무대 마스크 전환 프리셋. `kind`(`StageMaskKind` enum), `fromOffset`/`toOffset`(px),
모양별 파라미터(strip 높이/폭·bleed·iris 반경·segment 등 — 필드명이 곧 의미,
`…Pixels` 접미사는 px), `edge*`(테두리 표시). 2b 등급: 미표시(뱃지).

### screen-flash.json / screen-noise.json / screen-vignette.json

화면 오버레이 이펙트 프리셋. 공통 `key`, `amount`(0~1 강도), `color`.

- flash: `attackDuration`/`holdDuration`/`releaseDuration`(초), `attackEase`/`releaseEase`
  (DOTween `Ease` enum 정수 — 정지 프레임에는 무관, 최종 상태만 보면 된다)
- noise: `scale`(노이즈 입자 크기), `speedX`/`speedY`(스크롤 속도), `contrast`
- vignette: `radius`(0~1), `softness`(0.001~1), `aspect`(가로세로 비)

2b 등급: flash/vignette는 근사(표시) 축, noise는 미표시(뱃지) 축 (D-2b-2).

### surface-layout.json (`DialogueSurfaceLayoutPresetDBSO`)

대사창 배치 프리셋. `entries[]`의 `line*`/`name*` 필드가 본문/이름 rect의
anchorMin/Max·pivot·anchoredPosition·sizeDelta(위 rig-schemas와 같은 의미·px)와
타이포그래피(폰트 크기 px, `lineAlignment` 등은 TextMeshPro enum 정수).
`useName`: 이름칸 표시 여부. 2b 등급: 재현 축(대사창 배치).

## export-report.json

- `warnings[]`: 내보내지 못했거나 의심스러운 항목. **비어 있지 않으면 반드시 읽을 것.**
- `knownButNotExported[]`: 존재를 알지만 이번 범위(U12 지시 7묶음) 밖이라 안 내보낸 것.
  조용히 빠뜨린 게 아니라 여기 기록돼 있다.

## BGM 키 (파일 아님 — 런타임 상태)

U12-전체의 네 번째 항목 "BGM 문자열 키 보존"은 덤프가 아니라 런타임 변경이다:
`BgmPlayer.CurrentClipKey`가 현재(페이드가 끝나면) 재생 중일 BGM의 문자열 키를 담는다.
"예약된 최종값" 의미론 — Play 수락 시점에 목표 키가 되고 Stop이면 null.
U15 상태 스냅샷이 이것을 읽는다.
