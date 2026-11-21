# 카메라 리그 — 샷의 축마다 노드 하나, 전환 하나

샷이 무엇을 움직이는지, 그 값이 무대에 어떻게 쓰이는지, 대본에서 어떻게 적는지를 남긴다.
2026-11-17의 기록이다. 결정의 까닭과 단계는 [PLAN_camera-rig.md](../../../../../docs/PLAN_camera-rig.md)에, 무대(마스크 · 층)는 [stage-rig.md](../../StageRig/Documentation~/stage-rig.md)에 있다.

## 0. 세 문장

> **샷은 카메라의 축 값 한 벌이고, 무대의 트랜스폼은 전부 그 값의 함수다.** 그 함수는 코어 `ShotLayoutMath` 한 곳에 있다.
>
> **한 노드 = 한 축, 한 축 = 한 전환.** 명령은 제가 적은 축만 다시 조준하고, 적지 않은 축은 하던 대로 간다.
>
> **흔들림은 축이 아니다.** 구도 위에 얹혔다가 0으로 돌아오는 모션 클립이고, 상태에 남기는 것이 없다.

---

## 1. 축

| 겹 | 축 | 값 | 단위 · 방향 | 화면에서 | 중립 |
|---|---|---|---|---|---|
| **몸** | 트랙 | `Track`(x · y) | 무대 단위. **카메라의 자리** — mid 면의 그 무대 점이 화면 가운데에 온다 | 무대가 반대로 밀리고, 층마다 다르게 밀린다(시차) | (0, 0) |
| **몸** | 달리 | `Dolly` | 줌과 같은 눈금(1당 mid 면 ×0.05). +면 다가간다 | 가까운 층이 더, 먼 층이 덜 커진다 | 0 |
| **헤드** | 롤 | `Roll` | 도. +면 그림이 반시계로 돈다 | 화면 가운데를 중심으로 전체가 돈다 | 0 |
| **렌즈** | 줌 | `Zoom` · `FocusPoint` | 1당 ×0.05 | 화면 가운데 기준 배율 | 0 |
| **렌즈** | 팬 | `Pan`(x · y) | 화면 px. **화면이 가는 쪽** | 화면 전체가 밀린다 | (0, 0) |

- **트랙과 팬은 방향이 반대다.** 트랙은 카메라가 가는 쪽이고(오른쪽으로 가면 화면은 왼쪽으로 흐른다), 팬은 화면이 가는 쪽이다.
- **달리 n과 줌 n은 mid 면에서 같은 크기다.** 다른 것은 다른 층이다 — 줌은 층을 거의 같이 키우고, 달리는 d에 따라 크게 다르게 키운다. 둘을 반대로 걸면 인물은 그대로이고 배경만 변한다.
- **팬은 화면 밀기다.** 줌 바깥에서 화면 px로 민다 — 렌즈를 돌리는 "진짜 팬"(당겨 잡을수록 같은 각도에 더 미는 것)이 아니다. 멈춘 구도에서는 둘이 구별되지 않는다(`화면 밀기 = 배율 × 진짜 팬`).
- 초점(`FocusPoint`)은 줌에 실린다. 층 산개의 기준이라 줌과 한 몸이다.

---

## 2. 계층

이 런타임에 카메라는 없다. 카메라 변환의 역을 무대에 걸어 카메라처럼 보이게 한다 — 그래서 카메라에서 부모인 몸이 무대 쪽에서는 가장 안쪽이다.

```
{stage}_Mask_Root                    (무대 리그) 마스크 — 카메라 위에서 자른다
  {stage}_Camera_Root                안 움직인다. 카메라 리그의 경계
    {stage}_Camera_Shake             연기   흔들림. 위치 · 회전 · 배율 — 모션 클립만 쓴다. 끝나면 항등
      {stage}_Camera_Pan             렌즈   팬. anchoredPosition = pan
        {stage}_Camera_Zoom          렌즈   줌. localScale = 1 + 0.05 × zoom
          {stage}_Camera_Roll        헤드   롤. localEulerAngles.z = roll
            {stage}_Camera_Dolly     몸     달리의 공통 몫. localScale = 1 + 0.05 × dolly
              {stage}_Camera_Track   몸     트랙의 공통 몫. anchoredPosition = −track
                {stage}_Layer_{d}    (무대 리그) 깊이 층 — 카메라에 d만큼 다르게 반응한다
```

- 스키마 `CameraRigSchema`, 빌더 `CameraRigBuilder`, 참조 `CameraRigRefs`(`Stage/CameraRig/`). 무대 리그와 따로 선다 — 무대 리그가 카메라 리그를 마스크 아래에 세우고 층을 그 `Mount`(가장 안쪽 노드)에 건다. 카메라 리그는 무대를 모른다.
- **공통 몫은 카메라 노드에, d에 따른 차이는 층 노드에** 쓴다. 그래서 mid 층(반응 항등)은 어떤 카메라에서도 항등으로 남고, 하이어라키에서 카메라 노드만 읽으면 카메라가 어디서 무엇을 하는지 보인다.
- 줌 · 롤 · 달리는 중심이 같다(모두 피벗 중앙 · 위치 0). 순서를 바꿔도 값이 같다 — 순서는 겹(렌즈 → 헤드 → 몸)을 읽히게 하려는 것이다.
- 모든 노드는 stretch · 피벗 중앙 · 항등으로 선다. **카메라 노드의 중립은 전부 항등이다.**
- 카메라 리그는 무대마다 하나씩 선다. 샷은 본 무대의 것만 움직이고, 덮는 무대의 것은 항등에 고정이다(`StageRoles`).
- `rig-schemas.json`에 `rigKind: "camera"`로 덤프된다(루트 `__root` = `Camera_Root`). 흔들림 클립이 이 스키마로 검증된다.

---

## 3. 식 — `Core/Transforms/ShotLayoutMath.cs`

p는 깊이 d인 층 안의 점, `s = 1 + 0.05 × zoom`, `g = 1 + 0.05 × dolly`.

```
화면 = pan + Rot(roll) · [ 층 오프셋 + M(d) × ( p − track × (1 + trackResponse(d)) ) ]

M(d)      = s ^ (1 + zoomScaleResponse(d))  ×  g ^ (1 + dollyScaleResponse(d))     층의 전체 배율
층 오프셋 = 초점 산개(줌에 비례) + pan × panResponse(d)
```

- **트랙 · 달리 · 롤이 0이면 종전 식(`pan + 층 오프셋 + s × 층 배율 × p`)과 비트 단위로 같다.** 그 식으로 잰 대본이 화면을 그대로 갖는 근거다. `ShotCameraAxesTests`가 종전 식을 오라클로 적어 두고 허용 오차 없이 견준다.
- **트랙**: 카메라의 축이 층 d를 뚫는 자리가 `track × (1 + trackResponse(d))`다. mid는 0, 먼 층은 음수(−1이면 무한히 멀어 안 밀린다), 가까운 층은 양수.
- **달리**: 층 배율이 `g ^ (1 + dollyScaleResponse(d))`다. 배율은 카메라의 축을 중심으로 걸린다 — 트랙이 배율 안쪽에 있어서, 카메라가 옮겨 선 자리의 무대 점은 다가가도 화면 가운데에 남는다. 축에서 벗어난 점의 시차는 층 배율의 차이에서 저절로 난다.
- **롤**: 팬 안쪽의 것 전부를 강체로 돌린다. 중심은 팬으로 민 화면의 가운데다.
  롤은 모서리를 스스로 메우지 않는다. 16:9에서 모서리를 덮는 배율은 `cos θ + (16/9) × sin θ`다(3°에 ×1.09, 5°에 ×1.15, 10°에 ×1.29) — 배경에 그만큼 여유가 없으면 줌 · 달리를 같이 건다.
- 줌의 층 반응(`zoomScaleResponse` · `focusSpreadPerZoom`)과 팬의 약한 시차(`panResponse`)는 종전 그대로다. 달리 · 트랙에 가까운 성질이 섞여 있지만 떼면 종전 대본의 화면이 바뀐다 — 프로필에서 0으로 두면 순수한 줌 · 팬이 된다.

### 3.1 노드에 쓰는 값

코어가 **노드 로컬 값**을 그대로 낸다(`CameraPose` · `LayerResponse`). 런타임은 받아 쓰기만 하고 좌표 변환을 하지 않는다.

```
Camera_Pan.pos     = pan
Camera_Zoom.scale  = s
Camera_Roll.rotZ   = roll
Camera_Dolly.scale = g
Camera_Track.pos   = −track
Layer_{d}.scale    = r(d) = s ^ zoomScaleResponse(d) × g ^ dollyScaleResponse(d)
Layer_{d}.pos      = 층 오프셋 ÷ (s × g)  +  track × ( 1 − r(d) × (1 + trackResponse(d)) )
```

이 값을 계층대로 합성하면 위의 식이 된다(`ShotCameraAxesTests` — 노드 값을 계층대로 합성하면 보이는 자리가 된다).

### 3.2 프로필

`LayerProfile`의 다섯: 줌 · 팬의 셋(`focusSpreadPerZoom` · `panResponse` · `zoomScaleResponse`)과 몸의 둘(`trackResponse` · `dollyScaleResponse`).

| d | far −1 | back −0.5 | mid 0 | front 0.5 | close 1 |
|---|---|---|---|---|---|
| `trackResponse` (x = y) | −0.5 | −0.25 | 0 | 0.25 | 0.6 |
| `dollyScaleResponse` | −0.6 | −0.3 | 0 | 0.3 | 0.8 |

- 몸의 둘은 **출발값**이다 — 화면을 보고 잡는다. 바꾸려면 코드의 `LayerProfileCurve.Default`를 바꾸고 다시 덤프한다(`layer-profile.json`).
- `layer-profile.json`에 몸의 둘이 없으면 0으로 읽는다(옛 파일 — 트랙 · 달리에 모든 층이 mid처럼 반응한다).
- `panResponse`는 축마다 (−1, 1) 안이어야 한다. −1 이하면 그 층의 점을 팬으로 화면 지점에 놓을 수 없고, 1 이상이면 기운 화면에서 팬을 푸는 2×2가 안 풀릴 수 있다.

### 3.3 초점을 화면 지점에 오게 하는 줌 · 팬 (보조 계산)

> 2026-11-21: `place` 계열 · `shot_focus_to`를 걷었다(`docs/PLAN_command-prune.md`) — 이 계산은 커맨드가 아니라 코어 함수(`ShotIntents.FocusTo` · `ShotLayoutMath.SolvePanToPlace`)로 남는다.
> 도구의 보조 버튼이 이것으로 줌 · 팬을 풀어 `shot_zoom` · `shot_pan`으로 적는다.

"대상의 초점이 화면 지점에 오는 줌 · 팬"을 푼다. **지금 카메라의 몸과 롤이 입력이다** — 같은 대상이라도 카메라가 옮겨 서 있거나 기울어 있으면 팬이 달라진다. 렌즈는 입력이 아니다(지금 줌 · 팬과 무관하게 풀린다).

```
보이는 자리 = pan + Rot(roll) · (pan × panResponse) + [팬 0일 때 보이는 자리]
```

팬에 대해 선형이다. 롤이 0이면 축별 나눗셈(`pan × (1 + panResponse)`)이고, 아니면 2×2다(`ShotLayoutMath.SolvePanToPlace`).

---

## 4. 전환 — 축마다 하나

`PresentationShotResponseSystem`이 `IntentTransition`을 축마다 하나씩 든다(`ShotAxes` — 줌 · 팬 · 롤 · 달리 · 트랙).

- 명령은 **제가 적은 축만** 다시 조준한다. 적지 않은 축은 하던 전환을 제 길이로 마친다 — 느린 달리 도중에 줌을 넣어도 달리는 제 길이로 간다.
- 의도(다음 명령의 출발점 · 폴드가 접는 값)와 화면은 다섯 축을 합친 한 벌이다. 어느 축이 움직이든 한 벌을 통째로 다시 푼다 — 전환은 트랜스폼이 아니라 의도를 보간한다.
- 종점은 하나다. 압축(스킵 · 시크) · 자연 종료 · 폴드가 축마다 같은 값에 닿는다.

**줄을 넘는지는 명령이 아니라 축이 정한다.**

| 축 | 다음 줄이 열리면 | 까닭 |
|---|---|---|
| 줌 · 팬 · 롤 | ×20으로 서둘러 끝낸다 | 그 줄의 연기다 |
| 트랙 · 달리 | 서두르지 않는다 — 제 길이로 간다 | 장면의 구도다. 여러 줄에 걸친 느린 이동이 된다(화면 효과 vignette · noise와 같은 부류) |

---

## 5. 흔들림

절차적 흔들림(시간 · 랜덤으로 계속 흔드는 것)은 끝값이 없어 정지 프레임이 못 접는다. 그 부류를 되살리지 않고, 있는 모션 체계를 쓴다.

**흔들림은 카메라 리그에 건 `restore` 모션 클립이다.**

- 클립: `motions.json`의 `rig: "camera"`. 키를 줄 수 있는 노드는 `Camera_Shake` 하나이고 `end`는 `restore`여야 한다(`MotionRules`) — 나머지 카메라 노드는 샷 식의 해라 키를 주면 샷이 쓰는 값과 싸우고, `commit`이면 폴드가 모르는 값이 남는다.
- 재생: `CameraShakePlayer`(샷 시스템이 든다). 본 무대의 `Camera_Shake`에 튼다. 한 번에 하나만 돌고, 새 흔들림이 오면 앞의 것을 0으로 끊고 시작한다. 샘플링은 코어 `MotionMath` 그대로라 랜덤이 없다 — 툴 프리뷰와 런타임이 같은 흔들림을 낸다.
- 세기: 키 값이 중립에서 벗어난 만큼에 곱한다(`MotionMath.SampleScaled`).
- 수명: 압축(스킵 · 시크) 중에는 틀지 않는다. 스텝 경계와 장면 경계에서 바로 0으로 끊는다.
- 폴드: `shot_shake`는 **무변**이다 — 못 접는 것이 아니라 접을 것이 없다. 틀 수 없는 줄(모르는 이름 · 카메라 리그가 아닌 클립)만 `Unhandled`로 남는다.
- 초점 측정은 `Camera_Shake`를 항등으로 본다 — 흔들리는 중에 재도 값이 같다.
- 하네스: 정착한 프레임에서 `Camera_Shake`가 항등이 아니면 불일치다(`stage:{무대}.cameraShake`).
- 흔들림의 폭만큼 화면 가장자리가 드러난다. 배경에 그만큼 여유가 있어야 한다.

견본 클립(`Assets/Dialogue/motions.json`): `shake_hit`(0.3초 · 14px 충격) · `shake_rumble`(0.8초 · 5px + 0.25° 떨림).

---

## 6. 명령

| 겹 | 명령 | 인자 (생략값) | 조준하는 축 | 뜻 |
|---|---|---|---|---|
| 렌즈 | `shot_zoom` | `zoom [0.45s] [이징]` | 줌 | 줌만 |
| 렌즈 | `shot_pan` | `x y [0.45s] [이징]` | 팬 | 팬만 |
| 몸 | `shot_track` | `x y [0.45s] [이징]` | 트랙 | 카메라가 서는 자리 |
| 몸 | `shot_dolly` | `n [0.45s] [이징]` | 달리 | 앞뒤 자리. +면 다가간다 |
| 헤드 | `shot_roll` | `도 [0.45s] [이징]` | 롤 | 기울기 |
| 연기 | `shot_shake` | `이름 [세기 1]` | (흔들림) | 흔들림 클립을 한 번 튼다 |
| — | `shot_reset` | `[0.3s] [이징]` | 줌 · 팬 (초점) | 렌즈를 중립으로 — 몸과 롤은 그대로다 |
| — | `shot_reset_all` | `[0.3s] [이징]` | 전부 | 카메라를 통째로 중립으로 |

- **`shot_<축>` = 그 축 하나를 절대값으로.** 같은 명령을 두 번 적어도 더해지지 않는다. 상대값 명령은 없다.
- **묶음은 없다.** 줌과 팬, 트랙과 달리를 함께 놓으려면 한 줄에 나란히 적는다 — 같은 프레임에 시작하고, 전환이 축마다 따로라 서로 덮지 않는다.
- **적지 않은 축은 건드리지 않는다.** 한 줄에 여러 축을 다른 길이로 적어도 축마다 제 길이로 간다.
- **값은 꼭 적는다.** 생략할 수 있는 것은 길이 · 이징 · 세기뿐이다. 값이 빠졌거나 못 읽으면 런타임은 오류를 내고 그 줄을 실행하지 않고, 폴드는 `Unhandled`로 남긴다.

장면의 기본 구도는 몸으로 잡고, 그 위에서 렌즈로 연기한다. `shot_reset`은 그 자리의 중립 렌즈로 돌아온다.

```yarn
<<shot_track 3u 0u 0fr>>            // 이 장면은 카메라가 오른쪽에 선다
하린: …
<<shot_dolly 4 2.5s>>               // 천천히 다가간다 — 배경이 인물보다 덜 커진다. 다음 줄로 넘어가도 제 길이로 간다
하린: …
<<shot_zoom 3 0.4s>>                // 다가가는 중에 렌즈로 확 당긴다. 달리는 하던 대로 간다
<<shot_shake shake_hit>>
하린: !!
<<shot_reset 0.4s>>                 // 렌즈만 되돌린다 — 카메라는 그 자리에 다가선 채다
하린: …
<<shot_reset_all 0fr>>              // 장면이 바뀐다 — 통째로 처음으로
```

---

## 7. 어디에 있나

| 자리 | 하는 일 |
|---|---|
| `Core/State/ShotIntentState` | 축 값 한 벌 |
| `Core/Transforms/ShotLayoutMath` | 식 — `SolveCamera`(`CameraPose`) · `SolveLayer`(`LayerResponse`) · `ProjectToRigSpace` · `SolvePanToPlace` |
| `Core/Reduce/ShotIntents` | 명령이 끝나면 의도가 어떤 값이어야 하는가 — 전부 `from`을 받아 제 축만 바꾼다 |
| `Core/Reduce/StageReducer.Shot` | 폴드 |
| `Core/Motion/MotionRules` | 카메라 리그의 모션 규칙(`Camera_Shake` · `restore`) |
| `Stage/CameraRig/` | 스키마 · 빌더 · 참조 |
| `Stage/Shot/PresentationShotResponseSystem` | 축마다 전환 · 노드에 쓰기 · 중립 |
| `Stage/Shot/CameraShakePlayer` | 흔들림 재생 |
| `Stage/Shot/Commands/` | 명령 — `ShotIntentCommandBase`(제 축 · 스텝 경계) |
| `Stage/Grammar/StageCommandParser.Table` · `.ShotResponse` | 이름 · 인자 자리 · 생략값 |
| `Playback/YarnBridge/CommandBridge` | Yarn 등록(이름과 인자 수) |
