# 샷의 겹 — 카메라 몸(트랙)과 렌즈(팬 · 틸트 · 줌)를 가른다 — 계획 (2026-11-16)

기준: `refactor/actor-view` @ `8b60c733`. 상태: **대체됨 — [PLAN_camera-rig.md](PLAN_camera-rig.md)가 새 계획이다(2026-11-17).** §2–§8은 트랙만 넣는 첫 안이고 버렸다. 코드는 고치지 않았다.

이 문서에서 살아 있는 것은 §1(지금의 구조 · 한계 · 쓰임)과 §9.2 · §9.3(계층 순서의 까닭 · 조사한 사실)이다 — 새 계획이 그것을 근거로 든다.

## 0. 한 문장

> 지금 샷은 값 한 벌(줌 · 팬 · 초점)이다. 실제 촬영처럼 **카메라가 서 있는 자리(트랙)**와 **그 자리에서 렌즈가 하는 일(팬 · 틸트 · 줌)**을 다른 겹으로 가른다.

## 1. 지금

### 1.1 구조

```
Stage_Root
  Mask_Root
    Camera_Pan      샷의 팬. anchoredPosition = pan
      Camera_Zoom   샷의 줌. localScale = 1 + 0.05 × zoom
        Layer_{d}   깊이 층. 샷에 d만큼 반응한다
```

- 의도는 `ShotIntentState` 하나다 — `Zoom` · `PanInRigSpace` · `FocusPointInRigSpace`.
- 전환도 하나다 — `PresentationShotResponseSystem`의 `IntentTransition` 하나가 그 한 벌을 통째로 옮긴다.
- 화면에 보이는 자리(`ShotLayoutMath.ProjectToRigSpace`):

```
화면 = pan + 층 오프셋 + cameraScale × 층 배율 × p
층 오프셋 = 초점 산개(줌에 비례) + pan × panResponse(d)
층 배율   = cameraScale ^ zoomScaleResponse(d)
```

- 명령은 다섯이고 전부 같은 한 벌을 고쳐 쓴다.

| 명령 | 하는 일 |
|---|---|
| `shot_to zoom x y` | 줌과 팬을 절대값으로 |
| `shot_zoom zoom` | 줌만 |
| `shot_track x y` | **지금 팬에 더한다** — 이름은 트랙이지만 팬이다 |
| `shot_focus_to 대상 …` | 대상의 초점이 화면 지점에 오는 줌 · 팬을 푼다 |
| `shot_reset` | 상수 기본값(줌 0 · 팬 0 · 초점 0)으로 |

### 1.2 촬영 용어로 보면

| 촬영 | 뜻 | 화면에서 | 지금 |
|---|---|---|---|
| 트랙(트럭 · 페데스탈) | 카메라 몸이 좌우 · 위아래로 **움직인다** | 가까운 것이 많이, 먼 것이 적게 밀린다(시차) | **없다** |
| 팬 · 틸트 | 제자리에서 렌즈를 좌우 · 위아래로 **돌린다** | 화면 전체가 같이 밀린다 | `pan`(벡터 하나) — 다만 층마다 약한 시차가 섞여 있다(`panResponse`: far −0.04 … close +0.2, x축만) |
| 줌 | 렌즈 배율 | 화면 가운데 기준으로 전체가 커진다 | `zoom` — 다만 층마다 배율 · 산개가 다르다(달리에 가까운 성질이 섞여 있다) |
| 달리 | 카메라 몸이 앞뒤로 움직인다 | 가까운 것이 더 크게 변한다 | 줌에 섞여 있다 |
| 흔들림 | 구도 위에 잠깐 얹는 움직임 | 끝나면 제자리 | **없다** |

지금의 `pan`은 팬과 트랙이 한 값에 섞인 것이고, 둘을 따로 움직일 수 없다.

### 1.3 한계

| # | 한계 | 까닭 |
|---|---|---|
| L1 | **카메라가 서는 자리를 잡을 수 없다.** 장면의 기본 구도를 두고 그 위에서 렌즈만 움직일 수 없다 | 겹이 하나 |
| L2 | `shot_reset`이 장면 구도가 아니라 상수 기본값으로 간다. 구도로 돌아가려면 숫자를 다시 적는다 | 돌아갈 "기준"이 없다 |
| L3 | 두 움직임이 같이 못 돈다. 느린 이동 중에 줌을 넣으면 새 의도가 앞 전환을 대체하고, 이동도 줌의 길이로 끝난다 | 전환이 하나 |
| L4 | 시차가 약하고 고를 수 없다. 팬에 딸린 값(x축 ±0.04~0.2)뿐이다 | 트랙 축이 없다 |
| L5 | 구도 위에 얹는 흔들림 · 표류를 쓸 수 없다 | 연기 겹이 없다 |

### 1.4 쓰이는 곳

| 어디 | 무엇 |
|---|---|
| 예제 대본 | `shot_focus_to` 1 · `shot_reset` 5 · `shot_to` 1 |
| ked-vn-workspace 재현 대본 | `shot_to` **512** · `shot_reset` 23. 전부 절대값이고 `tools/stage/script.py`가 낸다. 인물 자리를 `(화면 − 팬) ÷ 카메라 배율`로 푼다 — 지금 식에 맞춰 잰 값이다 |
| `shot_track` | **쓰는 대본이 없다** |
| `shot_zoom` | 쓰는 대본이 없다 |

재현 작업의 기록에 같은 한계가 보인다.
- "배경 track, 면접관은 무대에 선 채"(`ch05_01` 99 · 108번 줄) — 배경만 움직이는 것을 `shot_to`로 적었다.
- 화면 흔들림 R8 — 어휘가 없어 카드를 두지 않았다(`vocabulary-gaps.md`).
- 카메라 카드 K01(다가가기) · K02(물러나기) · K03(옆으로 훑기) — 다가갔다 돌아오는 짝이 많은데, 돌아올 값을 대본 생성기가 기억해서 적는다.

## 2. 제안 — 두 겹

캐릭터 리그의 규율을 옮긴다: **한 노드 = 한 축, 자리가 그 노드의 뜻이다**(`CharacterRigSchema`).

```
Stage_Root
  Mask_Root
    Camera_Pan        렌즈 — 팬 · 틸트. 화면 전체를 민다           (지금 그대로)
      Camera_Zoom     렌즈 — 줌. 화면 가운데 기준 배율              (지금 그대로)
        Camera_Track  몸 — 카메라가 서는 자리. 무대를 그 반대로 민다  (새 노드)
          Layer_{d}   깊이 층. 렌즈와 몸에 d만큼 반응한다
```

| 겹 | 축 | 값 | 화면에서 | 중립 |
|---|---|---|---|---|
| **몸** | 트랙 | `Track`(x · y, 무대 단위) | 무대가 반대로 밀리고, 층마다 시차가 난다 | (0, 0) |
| **렌즈** | 팬 · 틸트 | `Pan`(x · y) — 지금 값 | 화면 전체가 민다 | (0, 0) |
| **렌즈** | 줌 | `Zoom` · `FocusPoint` — 지금 값 | 화면 가운데 기준 배율 | 0 |

트랙이 줌 **아래**에 있다. 그래서 카메라가 옮겨 선 자리를 가운데로 줌이 걸린다 — 실제 카메라와 같은 순서다.

### 2.1 식

```
화면 = pan + 층 오프셋 + cameraScale × ( 층 배율 × p  −  track × (1 + trackResponse(d)) )
```

- `trackResponse(d)`는 새 프로필 값이다. mid = 0(무대가 트랙만큼 그대로 밀린다), 먼 층은 음수(덜 밀린다), 가까운 층은 양수(더 밀린다). 축별이다.
- `Camera_Track.anchoredPosition = −track`. 층마다의 차이(`−track × trackResponse(d)`)는 층 노드에 쓴다 — 줌 아래 공간의 값이라 배율로 나눌 필요가 없다.
- 트랙이 0이면 지금 식과 글자 그대로 같다. **지금 대본의 화면은 바뀌지 않는다.**
- 식은 지금처럼 Core `ShotLayoutMath` 한 곳에 둔다. 중립은 여전히 `Solve(Default)`다.

### 2.2 전환도 겹마다

`IntentTransition`을 둘 둔다 — 몸(트랙)과 렌즈(줌 · 팬 · 초점). 적용은 둘의 "지금 보이는 값"을 합쳐 한 번에 쓴다.
느린 트랙 도중에 줌을 넣어도 트랙은 제 길이로 간다(L3).

### 2.3 명령

| 명령 | 겹 | 뜻 | 지금과 |
|---|---|---|---|
| `shot_track x y [길이] [이징]` | 몸 | 카메라가 서는 자리를 절대값으로 | **뜻이 바뀐다** — 지금은 "팬에 더하기"다. 쓰는 대본이 없다 |
| `shot_pan x y [길이] [이징]` | 렌즈 | 팬 · 틸트만 절대값으로 | 새 명령 — 지금은 팬만 바꾸려면 `shot_to`에 줌도 적어야 한다 |
| `shot_zoom zoom …` | 렌즈 | 줌만 | 그대로 |
| `shot_to zoom x y …` | 렌즈 | 줌 · 팬 | 그대로 |
| `shot_focus_to 대상 …` | 렌즈 | 대상이 화면 지점에 오는 줌 · 팬 | 식에 트랙이 들어간다(§2.4). 트랙 0이면 그대로 |
| `shot_reset …` | 렌즈 | 줌 0 · 팬 0 · 초점 0 | **트랙은 건드리지 않는다**(→ Q4). 트랙을 쓰지 않는 대본에서는 그대로 |

장면의 기본 구도는 `shot_track`으로 잡는다. 그 위에서 `shot_zoom` · `shot_pan` · `shot_focus_to`로 연기하고, `shot_reset`으로 그 자리의 중립 렌즈로 돌아온다(L1 · L2).

```yarn
<<shot_track 3u 0u 0fr>>          // 이 장면은 카메라가 오른쪽에 선다
하린: …
<<shot_zoom 3 0.4s>>              // 그 자리에서 다가간다
하린: !!
<<shot_reset 0.4s>>               // 그 자리의 기본 렌즈로 — 카메라는 그대로 오른쪽
```

### 2.4 `shot_focus_to`

지금은 "무대 위 자리와 층만으로" 팬을 푼다. 트랙이 생기면 **지금 카메라가 선 자리가 입력**이 된다 — 같은 대상이라도 카메라가 옮겨 서 있으면 팬이 달라진다.
`ShotLayoutMath.SolvePanToPlace`가 트랙을 받아 "팬 0일 때 보이는 자리"에 넣는다. 보이는 자리는 여전히 팬에 대해 선형이라 닫힌 꼴로 풀린다.

### 2.5 폴드 · 중립 · 하네스

- `ShotIntentState`에 `Track`이 든다. `StageReducer.Shot`이 `shot_track` · `shot_pan`을 접고, `StageStateComparer` · `StageViewSnapshot`이 견준다.
- `Camera_Track`도 샷 소유 노드다(`StageRigRefs.IsShotOwned`). 정착 사슬이 지날 때 중립을 쓴다 — 인물의 자리 계산은 카메라가 어디 서 있든 같다.
- 프로필 파일(`layer-profile.json` — `ExportedTuning`)에 `trackResponse`가 는다. 없으면 0으로 읽는다(옛 파일 호환).
- Core가 바뀌므로 사본들이 따라간다.

## 3. 하지 않는 것 (이번에)

| 무엇 | 까닭 |
|---|---|
| 달리(앞뒤 트랙)를 줌에서 떼기 | 지금 줌의 층 반응(배율 · 산개)이 그 구실을 한다. 떼면 재현 대본 512줄의 화면이 바뀐다 |
| 팬의 약한 시차(`panResponse`)를 걷기 | 구조가 아니라 **튜닝값**이다. 프로필 파일에서 0으로 두면 순수한 팬이 된다. 기본값은 그대로 둔다 — 지금 화면이 바뀌지 않게 |
| 롤(화면 기울이기) | 수요가 아직 없다. 노드 하나(`Camera_Roll`)로 나중에 더할 수 있다 |
| 흔들림 · 표류(연기 겹) | 수요는 있다(R8). 이 계획의 겹이 서면 그 위에 더하는 일이다 — 따로 계획한다 |

## 4. 영향

| 어디 | 무엇 |
|---|---|
| Core | `ShotIntentState` · `ShotLayoutMath` · `ShotIntents` · `StageReducer.Shot` · `StageStateComparer` · `StageViewSnapshot` · `LayerProfile`(+DTO) |
| Stage | `StageRigSchema`(노드 하나) · `StageRigBuilder` · `StageRigRefs` · `PresentationShotResponseSystem`(전환 둘) · 샷 명령 · 파서 표 · 브리지 등록 |
| 문서 | `stage-rig.md`(§2 계층 · §4 식 · §7 focus_to) · `target-boundary.md` · README |
| 지금 대본 | 화면 변화 없음(트랙 0). `shot_track`을 쓰는 대본은 없다 |
| 사본 | Core를 반입한 곳 전부(ked-stage-runtime · compositor · characterRig · workspace) |
| ked-vn-workspace | 바로 바꿀 것은 없다. "배경만 track" 같은 자리를 트랙으로 다시 잴지는 그쪽 결정이다 |

## 5. 단계

| # | 일 | 동작 변화 |
|---|---|---|
| T1 | Core: `ShotIntentState.Track` · `LayerProfile.TrackResponse` · `ShotLayoutMath`(식 · `SolvePanToPlace`) + EditMode 테스트(트랙 0이면 종전 값과 같다) | 없음 |
| T2 | 무대: `Camera_Track` 노드 · 샷 소유 · 중립 | 없음(항등) |
| T3 | 샷 시스템: 전환 둘(몸 · 렌즈) · 적용 합성 | 없음(트랙 0) |
| T4 | 명령: `shot_track`(새 뜻) · `shot_pan` · `shot_reset`(렌즈만) · `shot_focus_to`(트랙 입력) + 파서 · 브리지 · 폴드 | 트랙 생김 |
| T5 | 프로필 기본값(`trackResponse`) 잡기 — Unity에서 눈으로 | 시차 |
| T6 | 데모 대본에 트랙 한 대목 · 문서 | — |

## 6. 지켜야 할 것

- **트랙 0이면 지금과 같다.** T1의 테스트가 이것을 본다 — 재현 대본 512줄의 근거다.
- **종점은 하나다.** 압축(`OnSkip`) · 자연 종료 · 폴드가 같은 의도에 닿는다(R3).
- **식은 Core 한 곳.** 런타임은 출력을 노드에 쓰기만 한다(`stage-rig.md` D5).
- **중립 = `Solve(Default)`.** 따로 적은 정의를 두지 않는다(D6).

## 7. 열어 둔 것

- **스텝 경계의 가속.** 샷 전환은 다음 줄이 열리면 ×20으로 끝난다(R6). 그래서 여러 줄에 걸친 느린 트랙은 지금 규칙으로는 안 된다.
  트랙을 화면 효과(vignette · noise)처럼 "경계에서 관리하지 않는" 부류로 둘 수 있다 — 그러면 줄을 넘겨도 제 길이로 간다(→ Q7).

## 8. 정할 것

| # | 물음 | 제안 |
|---|---|---|
| Q1 | 겹을 어떻게 나누나 | 노드를 더한다(`Camera_Track`). 값만 둘로 나눠 지금 노드에 합쳐 쓰는 방법도 되지만, "한 노드 = 한 축"이 캐릭터 리그와 같고 하이어라키에서 읽힌다 |
| Q2 | 트랙 값의 방향 | **카메라의 자리**다 — `shot_track 3u 0u`면 카메라가 오른쪽으로 가고 화면은 왼쪽으로 흐른다. 팬은 지금처럼 "화면이 가는 쪽"이다(512줄이 그렇게 쓴다). 두 방향이 다르다는 것을 문서에 적는다 |
| Q3 | `shot_track`의 지금 뜻(팬에 더하기) | 지운다. 쓰는 대본이 없다. 이름을 새 트랙이 쓴다 |
| Q4 | `shot_reset`의 범위 | 렌즈만. 카메라 자리는 `shot_track 0u 0u`로 돌린다. "전부 되돌리기"가 필요하면 인자를 하나 더한다 |
| Q5 | `shot_pan`을 더하나 | 더한다. 팬 · 틸트만 바꾸는 명령이 지금 없다 |
| Q6 | 트랙의 축 | x · y 둘. 앞뒤(달리)는 이번에 넣지 않는다(§3) |
| Q7 | 트랙이 줄을 넘겨도 이어 가나 | 이번에는 지금 규칙(다음 줄에서 가속해 끝냄) 그대로. 느린 트랙이 필요하면 바로 이어서 푼다 |

## 9. 이어받기 (2026-11-16)

### 9.1 정해진 것 (소유자)

- **겹을 나눈다.** 노드로 나눈다.
- **카메라 리그를 무대 리그와 따로 세운다** — `StageRigSchema`에 노드를 끼우지 않고 `CameraRig`(스키마 · 빌더 · 참조)를 둔다.
- **달리 · 롤 · 흔들림도 넣는다.** 쓸 일이 많다. 트랙만 넣는 §2–§8은 버리고, 이 셋까지 넣은 계획을 새로 쓴다.
- §8의 Q2–Q7은 아직 답하지 않았다. 새 계획에서 다시 묻는다.

### 9.2 계층 순서 — "트랙이 부모, 팬이 자식" 아닌가

카메라 자신의 계층으로는 맞다: **몸(트랙 · 달리) → 헤드(팬 · 틸트 · 롤) → 렌즈(줌)**.
그런데 이 런타임에는 카메라가 없고 **무대를 반대로 움직여** 카메라처럼 보이게 한다. 무대에 거는 변환은 카메라 변환의 역이라 순서가 뒤집힌다.

```
카메라      C = 트랙 · 헤드            화면 = 렌즈 · 헤드⁻¹ · 트랙⁻¹ · p
무대 노드   바깥 → 안 :  렌즈(줌) → 헤드(팬 · 롤) → 몸(트랙) → 층
```

그래서 무대 쪽에서는 **트랙이 가장 안쪽**이다 — 카메라에서 부모라서다. 첫 안(§2)의 트랙 자리는 맞다.

지금 `Camera_Pan`은 줌 **바깥**에 있다. 진짜 팬(렌즈를 돌림)이라면 줌 안쪽이어야 한다 — 당겨 잡을수록 같은 각도에 화면이 더 민다.
지금 것은 줌과 무관하게 화면 px로 미는 값, 곧 **화면 밀기(프레임 시프트)**다. 재현 대본 512줄이 이 뜻으로 쟀다(`인물 자리 = (화면 − 팬) ÷ 카메라 배율`).
새 계획에서 정할 것: 지금 팬을 "화면 밀기"로 그대로 두고 진짜 팬을 줌 안쪽에 따로 둘지, 하나로 둘지.

새 계획의 출발 순서(바깥 → 안, 무대를 움직이는 쪽):

| 노드 | 겹 | 뜻 | 비고 |
|---|---|---|---|
| Shake | 연기 | 흔들림. 화면 px로 얹고 끝나면 0 | 끝값이 없다 — 폴드는 접지 않는다(모션의 절차적 축과 같은 부류) |
| Shift | 렌즈 | 화면 밀기 = 지금의 `pan` | 512줄 호환 |
| Zoom | 렌즈 | 화면 가운데 기준 배율 | 지금 그대로 |
| Roll | 헤드 | 화면 가운데 기준 회전 | 줌과 중심이 같아 서로 순서를 바꿔도 같다 |
| (Pan/Tilt) | 헤드 | 진짜 팬 — 둘지 미정 | 줌 안쪽 |
| Track | 몸 | 카메라가 서는 자리(x · y). 층마다 시차 | 가장 안쪽 |
| Dolly | 몸 | 앞뒤. **노드가 아니다** — 층마다 배율 · 산개가 d에 따라 다르게 변한다 | 지금 줌의 층 반응(`zoomScaleResponse` · `focusSpreadPerZoom`)이 이 성질이다. 줌에서 떼면 512줄 화면이 바뀐다 → 줌의 것은 두고 달리 축을 따로 더하는 쪽 |

### 9.3 조사한 사실 (다시 읽지 않아도 되게)

**구조** — 경로는 `Assets/Ked.Presentation/` 아래.
- 무대 계층: `Stage/StageRig/StageRigSchema.cs`. `Stage_Root → Mask_Root → Camera_Pan → Camera_Zoom → Layer_{d} → …_Content`. 모든 노드가 stretch · 피벗 중앙 · 항등으로 선다. 빌더 `StageRigBuilder.cs`, 참조 `StageRigRefs.cs`(`IsShotOwned` — 샷 소유 노드 목록).
- 무대는 여럿이다(`stage00` 본 무대 · `stage01/02` 덮는 무대). 카메라는 무대마다 있고 **샷은 본 무대만 움직인다**(`StageRoles`, 결정 D2 · D11). 카메라 리그를 따로 세울 때 "무대마다 하나"인지 "본 무대에만"인지 정해야 한다.
- 마스크(`Mask_Root`)가 카메라 위에 있다 — 카메라가 밀어낸 내용을 자른다. 카메라 리그는 마스크 아래에 들어가야 한다.
- 샷 시스템: `Stage/Shot/PresentationShotResponseSystem.cs`. `IntentTransition` **하나**가 의도 한 벌을 옮긴다. `Follow`(전환) · `Snap`(압축) · `AccelerateTransition`(스텝 경계) · `Clear`(장면 경계). 중립 조회 `TryGetNeutral`.
- 의도: 런타임 `Stage/Shot/PresentationIntentState.cs`, Core `Core/State/ShotIntentState.cs` — `Zoom` · `PanInRigSpace` · `FocusPointInRigSpace`. 다리 `PresentationIntentStateCoreBridge.cs`.
- 식: `Core/Transforms/ShotLayoutMath.cs` — `SolveCamera` · `SolveLayer` · `ProjectToRigSpace` · `SolvePanToPlace` · `LayerProfileCurve`(d → 계수, 기본 표본 다섯: far −1 · back −0.5 · mid 0 · front 0.5 · close 1). 카메라 배율 `1 + 0.05 × zoom`.
- 목표 계산: `Core/Reduce/ShotIntents.cs`(`Zoom` · `Track` · `To` · `Reset` · `FocusTo`). 폴드 `Core/Reduce/StageReducer.Shot.cs`. 비교 `Core/State/StageStateComparer.cs` · `StageViewSnapshot.cs`.
- 명령: `Stage/Shot/Commands/`(`ShotIntentCommandBase` — 의도에서 출발해 다음 의도를 넘긴다, 스텝 경계 가속 ×20). 파서 표 `Stage/Grammar/StageCommandParser.Table.cs` 39–43행. 브리지 등록 `Playback/YarnBridge/CommandBridge.cs`의 `BindShotStaging`.
- 문서: `Stage/StageRig/Documentation~/stage-rig.md`(계층 §2 · 식 §4 · 중립 §5 · `shot_focus_to` §7 · 결정 §9). 캐릭터 리그의 규율은 `CharacterRigSchema.cs` 머리 주석과 `rig-axis-order.md`.
- 튜닝 파일: `ExportedTuning/`의 `layer-profile.json`(DTO `Core/Tuning/LayerProfileDto.cs`).

**쓰임**
- 예제 대본: `shot_focus_to` 1 · `shot_reset` 5 · `shot_to` 1.
- ked-vn-workspace 재현 대본: `shot_to` 512 · `shot_reset` 23, 전부 절대값. `tools/stage/script.py`가 낸다(112행이 `shot_to`를 쓴다 — `zoom = (배율 − 1) / 0.05`).
- `shot_track` · `shot_zoom`: 쓰는 대본이 없다. `shot_track`은 지금 "팬에 더하기"다.
- 배경은 `bg_spawn`의 기본 층이 `far`다.

**지켜야 할 것** — §6 그대로. 무엇보다 **새 축이 모두 0이면 지금 식과 같아야 한다**(512줄).

**수요의 근거**(ked-vn-workspace): `docs/vocabulary-gaps.md` R8(화면 흔들림 — 어휘 없음) · S12(집중선), `catalog/참고집.md` K01–K03(카메라 카드), `ch05_01.yarn` 99 · 108번 줄(배경만 track).

### 9.4 다음 세션이 할 일

> 2026-11-17 — 했다. [PLAN_camera-rig.md](PLAN_camera-rig.md). 3번의 물음은 그 §10에 제안과 함께 있다.

1. 이 절(§9)만 읽는다.
2. 달리 · 롤 · 흔들림 · 트랙을 넣은 카메라 리그 계획을 새로 쓴다 — 계층(§9.2에서 출발) · 식 · 겹마다의 전환 · 명령 어휘 · 폴드 · 단계.
3. 그때 물을 것: 진짜 팬을 따로 둘지(§9.2) · 카메라 리그가 무대마다인지 · 값의 방향(Q2) · `shot_reset`의 범위(Q4) · 줄을 넘는 느린 움직임(Q7) · 흔들림을 폴드에서 어떻게 볼지.
