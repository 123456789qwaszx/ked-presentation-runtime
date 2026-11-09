# 초상 사이징 — 높이 맞춤을 걷고 원본 픽셀 크기로 (2026-11-09)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/subSystem` @ `c89accfd` |
| 본보기 | 배경의 같은 전환 `cc927f38`(`PLAN_background-source-pixels.md`) — 같은 자리를 같은 방식으로 바꾼다 |
| 왜 | 영상 재현 — 다른 게임의 플레이 영상을 이 런타임의 어휘로 옮겨, Unity로 재생했을 때 원본과 거의 일치하게 한다 |
| 결정 | 아미야(소유자) 2026-11-09: 두 방식을 같이 두지 않고 높이 맞춤을 바꾼다(배경처럼). 예제 인물은 역할 앵커로 지금 모습을 지킨다. 코어의 식은 배경과 하나로 합친다. 원게임 값은 작업공간 전용 씬 · DB에 둔다 |

## 목표 한 문장

> 초상은 **원본 픽셀 크기 그대로** 그림 노드에 선다. 화면에 서는 크기와 자리는 역할 앵커(`visualScale` · `offset`)가 정한다.

## 근거 — 원본 게임 실영상

ch05 1편 영상(1920×886)을 높이 맞춤 캔버스(2340×1080)로 옮겨, 인물 그림 원본과 맞댔다.

| 인물 | 캔버스 px ÷ 그림 px | 그림 가운데 y | 그림 아래 끝 y |
|---|---|---|---|
| Iris | 0.815 | −438 | −1243 |
| Noya | 0.831 | −422 | −1250 |
| Fuyuka | 0.921 | −450 | −1268 |
| Darcia | 0.951 | −437 | −1391 |
| Firenze (변형 d) | 0.962 | −357 | −1324 |

- 배율이 인물마다 다르다 — 원본은 그림 픽셀에 인물별 배율을 곱한다. 높이 맞춤은 그림 높이를 지우고 모든 인물을 부모 높이(1080)에 맞추므로 이 모양을 낼 수 없다.
- 그림 높이는 1777~2017px로 기본 임포트 상한(2048)에 가깝다 — 상한을 올리고, 걸린 그림은 경고한다.

## 식

- **그림 노드** — `sizeDelta = 그림 크기 − 부모 크기 × (anchorMax − anchorMin)`. 늘림 앵커라 `그림 크기 − (1920, 1080)`. 배경과 같은 식이다.
- **역할 앵커** — `CharacterPortrait_VisualOffset`(아래 가운데 피벗 = 슬롯 좌표 y −540)에 배율과 위치가 걸린다.
  그림 가운데 y = −540 + 540 × visualScale + offset.y. 예: Iris는 offset.y ≈ −338.
- **예제 인물을 지키는 값** — `visualScale = 1080 / 그림 높이`, `offset.y = 540 × (1 − visualScale)`. 그러면 화면 높이 1080 · 가운데 0으로 전과 같다.

## 전과 후 (harin 1745×2170, 1920×1080 화면)

| | 전 — 높이 맞춤 | 후 — 원본 픽셀 + 역할 앵커 |
|---|---|---|
| 그림 노드 | `sizeDelta = (868.5, 0)` — rect 2788.5×1080, 보이는 크기는 `preserveAspect`가 정했다 | `sizeDelta = (−175, 1090)` — rect = 그림 크기 1745×2170 |
| VisualOffset | (0, 0) · 1 | (0, 271.2) · 0.4977 |
| 화면 | 868.5×1080, 가운데 0 | 868.5×1080, 가운데 0 — 같다 |

## 한 것

- **앞 커밋 `c89accfd`** — `cast`도 축 리셋 + 역할 앵커를 접는다(폴드를 런타임 파서의 팬아웃 cast → pose → face → SetAnchor에 맞춤).
  역할 앵커 키 조회도 런타임 DB와 맞췄다(대소문자 무시 · 나중 항목 우선). 예제 대본은 `show` 없이 `cast`로만 인물을 세우므로,
  이것 없이 예제 항목을 넣으면 등가성 하네스가 `cast` 줄마다 갈림을 신고한다.
- **코어** — `NodeTargets.SourcePixelsSizing` 하나를 초상과 배경이 같이 쓴다(`NodeTargets.Portrait.cs`). 배경의 `BackgroundSizing`은
  이 이름으로 옮기고 `NodeTargets.Background.cs`를 지웠다. `PortraitDimensionsFileDto.TryGetAspect` → `TryGetSize`.
- **런타임** — `SetPortraitSprite` · `SetEmotionPortraitWipe`의 기본값이 `SourcePixels`다. `HeightFitPreserveAspect`(=1)와
  `ApplyHeightFitPreserveAspect`를 지웠다 — 번호 1은 다시 쓰지 않는다(직렬화된 값이 엉뚱한 방식으로 읽히지 않게).
  그것만 쓰던 `HorizontalAlign`도 걷었다 — 스펙 셋의 `horizontalAlign`, 파서 `bg_sprite`의 값, `Apply`의 인자.
- **임포트** — `Editor/PortraitTextureImporter.cs`: 경로에 `/Portraits/`가 든 텍스처의 `maxTextureSize`를 4096으로 올린다(내리지 않는다).
  초상 뿌리는 `PortraitBuildSettings.scanFolders`가 정하지만, 임포트 도중에 설정 에셋을 읽지 않으려고 배경처럼 경로 표식 하나로 건다.
- **익스포터** — 초상 텍스처가 원본보다 작게 임포트됐으면(`GetSourceTextureWidthAndHeight` ≠ `sprite.rect`) 경고한다.
- **예제 데이터** — `RoleAnchorTuningDB.asset`에 예제 인물 11건(`Albedo`는 값을 고치고 10건을 더함). 높이는 원본 PNG에서 읽었다
  (지금 임포트된 1647×2048이 아니라 1745×2170 등). `PresentationSample.unity`의 `[StageCanvas]` CanvasScaler 기준을 0.5 → 1(높이).
- **테스트** — `PortraitDimensionsTests`: 크기 기대값. `StageReducerTests`: 폭을 rect 크기로 보고, 부모 높이와 다른 그림(harin 1745×2170) 케이스를 더했다.
- **문서** — `ExportedTuning/schema.md` §4 · §4b · role-anchor, `Core/Documentation~/target-boundary.md`, `docs/command-authoring.md` 본보기 표.

## Unity 확인 목록 (아미야)

1. 컴파일 오류 0. `PortraitTextureImporter.cs.meta`는 커밋에 들어 있다.
2. `Samples/Content/Portraits` 폴더 우클릭 › Reimport — harin · kasmi · sigure가 1745×2170으로 다시 들어온다.
3. `PortraitGeneratedDB`를 다시 만든다.
4. `Ked / U12 / Export Presentation Tuning Dump` → `portrait-dimensions.json`이 원본 크기, `base-resolution.json`의 `matchWidthOrHeight`가 1,
   `role-anchor.json`에 예제 11건, `export-report.json`의 `warnings`가 비어 있다.
5. EditMode 테스트 — `PortraitDimensionsTests` · `StageReducerTests` · `BackgroundTargetTests` · `StageCommandParserTests` 통과.
6. 예제 재생 — 전환 전후 스크린샷에서 인물의 높이와 자리가 같다. 등가성 하네스가 초상 · cast 줄에서 갈림을 신고하지 않는다.

## 하지 않은 것 · 남은 것

- **쓰이지 않는 방식** — `KeepLayout`(0) · `NativeSizeNoReanchor`(2)는 이번 전환과 상관이 없어 두었다.
- **변형별 앵커** — 역할 앵커 키는 캐릭터 하나다. Firenze 변형 a · d의 배율이 다르게 재지면 그때 키에 변형을 더한다.
- **원게임 값** — 이 저장소에 넣지 않는다. 작업공간 전용 씬 · 전용 `RoleAnchorTuningDB`에 넣는다. 남은 측정: Amber 기본 크기 · Cosette ·
  Female tyrant · Firenze 변형 a. `tyrant`(5) · `Amber`(10)는 높이 맞춤 기준으로 맞춘 값이라 옮기지 않는다.
- **리그 문서** — `Stage/Character/Rig/Documentation~/rig-axis-order.md` 95 · 107행이 배경을 "높이 맞춤으로 깐다" · "종횡비"로 적고 있다. 손대지 않았다.
- **분리본 · 복사본** — `ked-stage-runtime` 분리본과 compositor의 코어 사본은 다음 맞춤 때 따라온다.
