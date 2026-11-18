# 프레임은 30fps로 센다 (2026-11-18)

> 상태: **런타임 끝(Unity 확인 전)**. 소비처는 아직 — §4 표를 보고 반영한다. 반영을 마치면 §4 표에 적는다.
> 소유자 결정(2026-11-18): 대본의 `Nfr`과 이모지의 프레임을 **30fps**로 센다. 원본 영상과 작업공간의 도구(`compare.py` · `measure_advance.py` · `catalog/clips.py`의 `FPS = 30`)가 이미 30이다.

## 1. 무엇이 24였나

| 곳 | 무엇 |
|---|---|
| `Core/Tokens/DurationToken.cs` `FramesPerSecond` | 시간 토큰 `Nfr` — 대본의 모든 길이 인자(`<<fade_in s1 12fr>>` · `<<pause 24fr>>` · `#auto=54fr`)와 파서의 생략값 |
| `Core/Emoji/EmojiDefinition.cs` `DefaultFps` | `emojis.json`에 `fps`가 없을 때. 클립의 키는 프레임이다 |

- **L3에서 걷은 것은 명령 `<<Nfr>>`**(`<<12fr>>` = `pause 12fr`의 별칭)이다. 인자의 `Nfr` 단위는 그대로 있다 — `<<pause 30fr>>` · `0fr`. 시험의 `Knows("24fr")` · `[TestCase("24fr")]`(별칭) 같은 줄은 별칭이 걷혔음을 지키는 시험이라 숫자와 상관없다.
- 초로 적는 것은 상관없다 — 모션(`motions.json`의 `duration` · 키 시각), 프리셋(팝업 · 마스크 모션 · 덮개), 명령의 `…s` 생략값(`move_by` 0.4s · `stage_dim` 0.3s · 샷 0.45s).
- 재생은 모두 초로 돈다(이모지도 "지난 초 × fps"). 24 · 30은 저작 단위이지 그리는 프레임 수가 아니다.

## 2. 바꾼 것 — 길이(초)는 지킨다

| 곳 | 전 → 뒤 |
|---|---|
| `DurationToken.FramesPerSecond` | 24 → **30** |
| `EmojiDefinition.DefaultFps` | 24 → **30** — `fps`를 적은 파일은 그대로 |
| 파서의 프레임 생략값(`StageCommandParser.Table` · `.CharRigFocus` · `.CharRigPlacement`, 24곳) | 같은 초가 되게 옮겼다 — 아래 표 |
| 데모 · 진행 예제 대본(`Assets/Dialogue/ChapterProgression` 4개, 0 아닌 `Nfr` 74곳) · `README.md`의 예 | 같은 표 |
| 시험 | `DurationTokenTests`(30fr = 1초) · `StageCommandParserTests`(30fr = 1초, 15fr = 0.5초, `bg_fade_out` 생략값 13fr) · `AutoAdvanceTagTests`(`#auto=54fr` = 1.8초) · `EmojiPlaybackTests`(파일 fps 30, 더함: fps가 없으면 30이고 대본의 프레임과 같다) |
| `docs/emoji-format.md` · `EmojisFileDto` 주석 | `fps` 생략값 30 |

옮김 — 24fps 프레임 × 30/24를 가장 가까운 30fps 프레임으로(0.5는 올림). 반올림한 것도 차이는 1/60초 이하다.

| 24fps | 6 | 8 | 10 | 12 | 14 | 16 | 17 | 18 | 20 | 24 | 30 | 40 | 48 | 60 | 72 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 30fps | 8 | 10 | 13 | 15 | 18 | 20 | 21 | 23 | 25 | 30 | 38 | 50 | 60 | 75 | 90 |
| 초 차이 | +0.017 | 0 | +0.017 | 0 | +0.017 | 0 | −0.008 | +0.017 | 0 | 0 | +0.017 | 0 | 0 | 0 | 0 |

파서 생략값 — `face_swap` · `slide_in` · `slide_out` · `char_move_to` · `char_scale_to` · `char_rotate_to` · `char_visual_focus` · `bg_fade_in` · `bg_fade_out` · `bg_move` · `bg_scale` · 초점 깊이 10fr → 13fr, `fade_in` · `fade_out` · `fade` · 등장(show) 14fr → 18fr, `char_visual` · `char_visual_dim` · `_silhouette` · `_inner_rim` · `_outer_rim` · `_clear` 6fr → 8fr, `char_visual_defocus` 17fr → 21fr, 살짝 옮기기(nudge) 8fr → 10fr.

## 3. 그대로 둔 것

- 견본 이모지 `Assets/Dialogue/emojis.json`의 `"fps": 24` — 24로 저작한 키(`star` 등장 5 · 머묾 16 · 퇴장 6 …)라 30으로 옮기면 키와 길이가 프레임 사이에 놓인다. 적힌 값이 이기므로 재생은 전과 같다. 30으로 다시 저작할지는 툴에서 정한다(§4).
- 지난 계획서(`PLAN_*`)와 `target-boundary.md`의 `24fr` · `1fr`~`48fr`은 그때의 기록이다.

## 4. 소비처 — 반영할 것

| 저장소 | 걸리는 것 | 할 일 |
|---|---|---|
| `ked-vn-workspace`(`dev`) | 포크 `runtime/`이 24다 — `DurationToken` · `DefaultFps` · 파서 생략값 24곳 · 데모 대본 74곳. 작업공간의 대본은 `0fr`과 초(`script.py`의 `0.40s`)만 쓴다 — 뜻이 바뀌지 않는다. 파서 생략값에 기대는 줄은 같은 초로 옮겼으니 그대로다. 도구는 이미 30fps | 이 변경을 포크에 들이고 `runtime/FORK.md` 1절에 적는다 |
| `ked-staging-extractor`(`dev`) | `ked_staging/tokens.py` `FPS = 24` — 모든 길이를 `Nfr`로 낸다(`duration_token`). `vocab.py`의 `fade_in` 생략값 사본 `"14fr"` · `14`(163 · 187 · 189). 시험 · 시험 대본 · 문서의 `24fps` | **런타임과 같이** `FPS = 30`, 생략값 사본 `18fr`. 바꾸지 않으면 낸 대본이 30fps 런타임에서 20% 짧아진다. 오라클(compositor의 `Vn.StageOracle` — 코어 사본)도 따라야 한다 |
| `ked-motion-tool-characterRig`(`dev`) | 코어 사본의 `DurationToken` · `EmojiDefinition`(사본은 옛 판 — `vendoring.md`). `EmojiAsset.Fps`가 코어의 `DefaultFps`를 따른다 — 저작본은 초라 내보낼 때 30으로 바뀌지만, 1/24초 격자에 놓은 키는 30fps 프레임 사이에 놓인다(`EmojiValidator` 경고). `TimeSnap.DefaultStep`(1/24) · 프레임 눈금 · 재생 시계(`PlaybackClock`) · 보기(타임 슬라이더 · 그래프 · 도프시트) · 새 이모지 길이(24프레임 = 1초) · 설정의 "24fps 표시". `examples/emoji/emojis.json`(`"fps": 24` — 런타임 견본에서 지은 것) | 코어 사본을 정본에서 들이고 툴의 24를 30으로. 이미 저작한 이모지 · 견본을 30으로 옮길지 정한다. 문서(`USER-GUIDE` · `AUTHORING-FILES` · `guide`) · 시험 |
| `ked-story-director`(`dev`) | 코드에 없다(지난 기록에 `24fps`가 있을 뿐) | 없음 |
| 이 세션에서 닿지 않는 곳 | `ked-stage-runtime`(분리본 — `Documentation~/vendoring.md`), `ked-story-compositor`(오라클의 코어 사본), 형제 모션 툴(`ked-motion-tool-extend` · `ked-motion-tool-screenTransition`)의 코어 사본 | 정본에서 들일 때 함께 |

## 5. 확인

- 유니티 밖: Core 테스트(코어만 쓰는 EditMode 시험 26개 파일, 470) 통과. 파서 · 이모지 재생 시험(`StageCommandParserTests` · `EmojiPlaybackTests`)은 Unity가 있어야 돈다 — 바꾼 것은 토큰 · 기대값과 시험 하나(코어 부분은 유니티 밖에서 같은 내용으로 돌려 봤다).
- **Unity에서 소유자가 볼 것**: 컴파일, EditMode 시험, 데모의 연출 길이가 전과 같은가(같은 초로 옮겼다), 이모지 견본이 전과 같은가(`fps` 24를 적어 두었다).
