# 배경 사이징 — 높이 맞춤을 걷고 원본 픽셀 크기로 (2026-11-09)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/subSystem` ← `dev` @ `774ee89b` |
| 왜 | 영상 재현 — 다른 게임의 플레이 영상을 이 런타임의 어휘로 옮겨, Unity로 재생했을 때 원본과 거의 일치하게 한다 |
| 결정 | 아미야(소유자) 2026-11-09: "기본 방식(높이 맞춤)을 치운다. 예제 배경은 신경 쓰지 않는다. 배경을 화면보다 크게 만드는 게 맞다" |

## 목표 한 문장

> 배경은 **원본 픽셀 크기 그대로** 화면 가운데에 놓인다. 화면보다 큰 배경은 화면 밖까지 걸치고, 카메라(샷)는 그 안을 본다.

## 근거 — 원본 게임 실영상

플레이 영상(1920×886) 프레임을 위키에서 받은 배경 원본(297장, 2540×1800)과 특징점으로 맞댔다.

| 프레임 | 배경 | 배경 px ÷ 영상 px | 영상 (0,0)이 닿는 배경 자리 |
|---|---|---|---|
| 1편 20 · 60 · 120 · 250초 | `BG_uniseed_office` | 1.2165 | (101, 362) |
| 1편 550초 | 같음 | 1.1058 | (208, 365) — 줌인 |
| 4편 200초 | `BG_uniseed_corridor` | 1.0150 | (295, 450) — 줌인 |

- 기본 상태에서 보이는 것은 배경의 **가운데 2336×1078** — 화면 높이에 배경 1080px, 원본 크기 그대로 가운데. 위아래 약 360px, 좌우 약 100px가 화면 밖이다.
- 줌은 그 여백 안에서 가운데를 기준으로 일어난다.

## 전과 후 (2540×1800 배경, 1920×1080 화면)

| | 전 — 높이 맞춤 | 후 — 원본 픽셀 |
|---|---|---|
| 그려지는 크기 | 1524×1080 (0.6배) | 2540×1800 |
| 화면 | 배경 전체가 줄어 들어옴, 좌우 198px씩 빔 | 가운데 1920×1080이 보이고 사방이 화면 밖으로 걸침 |
| 노드 값 | `sizeDelta = (1524, 0)` — rect 3444×1080, 보이는 크기는 `preserveAspect`가 정했다 | `sizeDelta = (620, 720)` — rect = 그림 크기 |
| 카메라 줌아웃 | 위아래 여백 0 — 곧바로 가장자리가 드러난다 | 0.6배(위아래) · 0.76배(좌우)까지 가장자리가 안 보인다 |

`bg_scale`의 기준도 원본 크기가 된다(`bg_scale 1.2` = 원본의 1.2배). 화면보다 작은 예제 배경(1026×580 등)은 작게 선다 — 받아들인다(결정).

## 한 것

- **코어** — `NodeTargets.BackgroundSizing`(새 파일 `NodeTargets.Background.cs`): `sizeDelta = 그림 크기 − 부모 크기 × (anchorMax − anchorMin)`.
  `StageReducer.Background`의 사이징이 이것을 쓴다. `BackgroundDimensionsFileDto.TryGetAspect` → `TryGetSize`(배경만 쓰던 것).
- **런타임** — `CharRigImageSizingMode.SourcePixels`(=3)와 `CharRigImageSizingPolicy.ApplySourcePixels`: 같은 식. 앵커 · 피벗 · 위치는 건드리지 않는다.
  파서 `bg_spawn` · `bg_sprite`와 `SetSpriteCommandSpecCharR`의 기본값이 이 모드다. 높이 맞춤은 배경 길에서 걷었다(초상은 그대로).
- **임포트** — `Editor/BackgroundTextureImporter.cs`: `Resources/Backgrounds/` 아래 텍스처의 `maxTextureSize`를 4096으로 올린다(내리지 않는다).
- **익스포터** — 배경 텍스처가 원본보다 작게 임포트됐으면(`GetSourceTextureWidthAndHeight` ≠ `sprite.rect`) 경고한다.
- **테스트** — `BackgroundTargetTests`: 기대값을 sizeDelta · rect 크기로 바꾸고, 화면보다 큰 배경(2540×1800 → (620, 720)) 케이스를 더했다. `StageCommandParserTests`: 모드 기대값.
- **문서** — `ExportedTuning/schema.md` §4b, `Core/Documentation~/target-boundary.md`.

## Unity 확인 목록 (아미야)

1. 컴파일 오류 0. 새 파일 둘의 `.meta`가 생긴다 — 함께 커밋한다.
2. `Resources/Backgrounds` 폴더 우클릭 › Reimport — 상한 4096이 걸린다(3840×2160 예제 배경은 원본 크기로 다시 들어온다).
3. `Ked / U12 / Export Presentation Tuning Dump` → `background-dimensions.json`이 원본 크기로 바뀌고 `export-report.json`의 `warnings`가 비어 있다.
4. EditMode 테스트 — `BackgroundTargetTests` · `StageCommandParserTests` 통과.
5. 연출 데모 재생 — 배경이 원본 크기 그대로 가운데에 선다. 등가성 하네스가 배경 줄에서 갈림을 신고하지 않는다.

## 하지 않은 것 · 남은 것

- **화면 비율** — 원본 영상은 1920×886(약 2.17:1)이다. Game 뷰를 그 크기로, CanvasScaler를 높이 기준(match 1)으로 두면 캔버스가 2340×1080이 되어
  원본이 보여 주는 2336×1078과 0.2% 안에서 맞는다. 무대 보정 단계에서 런타임 설정으로 한다.
- **깊이 층과 카메라** — 배경 슬롯(far 층)이 샷에 반응하는 배율을 원본과 아직 맞대지 않았다(원본은 배경이 카메라 줌과 같은 배율로 커졌다).
- **분리본 · 복사본** — `ked-stage-runtime` 분리본과 compositor의 코어 사본은 다음 맞춤 때 따라온다.
