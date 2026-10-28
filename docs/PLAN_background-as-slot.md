# 배경 리그 제거 — 배경을 캐스팅하지 않은 슬롯으로

| | |
|---|---|
| 무대(원본) | `ked-stage-runtime` · `refactor/background-as-slot` ← `dev` @ `640ac60` |
| 재생(이 저장소) | `ked-presentation-runtime` · `refactor/background-as-slot` ← `dev` @ `0020943` (무대 반입본 = 위 브랜치, `diff -r` 0) |
| 분석 | 프로젝트 문서 `claude/analysis-2026-10-28-bgrig.md` — 노드 대응표 · 이력 · 발견한 고장 |

**현재 위치(2026-10-29):** 코드·문서·손 덤프 끝. 오프라인 컴파일(Unity 6000.3 참조) 18개 어셈블리 오류 0,
Core 순수 테스트 413/413. **Unity 확인이 남았다**(아래 목록). 두 브랜치는 `dev`에 합치지 않았다.

> 첫 시도는 낡은 기준(`refactor/framework-scope` @ `7ed5c1a5`) 위에서 했다가 버렸다 — 10-27에 무대가
> `ked-stage-runtime`으로 갈라진 것을 늦게 봤다. 설계는 그대로 옮겼고, 독립 검토에서 나온 결함(아래 D8·D11·D12)을
> 다시 하면서 반영했다. 옛 커밋은 이 컨테이너의 `wip/background-as-slot-stale`에만 있다(푸시하지 않았다).

## 목표 한 문장

> 배경은 별도 리그가 아니라 **캐스팅하지 않은 캐릭터 슬롯**이다. 리그 스키마는 `CharacterRig`·`ScreenEffectRig` 둘만 남고,
> `bg_*` 명령은 대본에 그대로 남되 무대의 파서가 캐릭터 리그 Spec으로 펼친다.

## 왜 지우는가

배경 리그는 05-21에 캐릭터 리그를 본떠 만든 쌍둥이다. 둘이 달라야 했던 이유는 06-09의 컷인 창(마스크 안에 배경과 캐릭터를
함께 넣고 창째 움직이는 연출)이었고, 그 기능은 08-18 `3a75d4f6`에서 지워졌다. 남은 배경 리그의 일은 far 레이어에 이미지
한 장을 높이 맞춤으로 깔고·갈아 끼우고·페이드하는 것뿐이다 — 전부 캐릭터 리그로 된다. 복제본은 덜 돌봐져서
`bg_place`가 넉 달째 NRE로 끝나고(`a53c71d4`부터, 컴파일러가 CS0649로 짚고 있었다), `bg_spawn` 뒤의 `bg_fade_in`은 1→1이라
한 번도 보인 적이 없다. "두 곳에 있으면 갈린다"(SCOPE-BOUNDARY §2)의 실제 사례다.

## 확정된 결정

| # | 항목 | 결정 | 근거 |
|---|---|---|---|
| D1 | `bg_*` 이름 | **유지.** 무대의 `StageCommandParser.Background`가 캐릭터 Spec으로 펼친다 | 대본·문서·외부 도구 변경 최소. "이건 배경"이라는 저작 의도가 대본에 남는다. 대가: 코어에 `bg_*` 케이스 |
| D2 | `bg_size` | **제거.** 대본 세 줄(값 1)을 지웠다 | 효과 없는 쓰임뿐. 남기면 `bg_scale`과 같은 노드를 두 명령이 잡는다 |
| D3 | `bg_place` | **제거.** | 넉 달째 깨져 있고 쓰임 0. `move_by`·`rotate_by`가 같은 일을 한다 |
| D4 | `focus_on`·`focus_clear`와 배경 | **배역 있는 슬롯만 훑는다** | 배경이 `SettledDepthScale` 1로 초점 계산에 끼지 않는다. `focus_clear`가 배경의 `screen_blur`를 지우지도 않는다(전과 같음). 플래그가 아니라 구조로 빠진다 |
| D5 | 마스크·흰 바탕 | **버린다.** | 예제 배경 7장이 모두 16:9 근처라 클립 효과가 없다. 21:9에서 양옆만 확인 |
| D6 | 리그 이름 | **`CharacterRig` 유지.** | 노드 이름이 3자 이름 동등성과 튜닝 덤프에 걸려 있다. 하이어라키에 `bg_main_CharacterPortraitSprite_Image`가 보이는 것은 받아들인다 |
| D7 | `bg_spawn` 첫 보임 | **숨은 채 선다**(캐릭터 `slot`과 같다), `bg_fade_in`으로 들인다 | 처음엔 "바로 보임"(옛 동작)으로 정했다가 바꿨다(아미야, 10-29) — 대본 세 곳이 모두 `bg_spawn` 뒤에 `bg_fade_in 24fr`을 쓴다. 대가: `bg_fade_in` 없이 `bg_spawn`만 쓰면 배경이 안 보인다(캐릭터와 같은 규칙) |
| D8 | 키 공간·덮어쓰기 | **하나.** 배경이 **배역 있는 슬롯**을 덮을 때만 배역을 풀고 경고한다 | 처음엔 "같은 키 재등록이면 늘 경고"였다 — 정상적인 재선언·배경 교체마다 울려 경고가 무뎌진다(검토 지적). 배역을 안 풀면 `fade_in harin`이 배경을 페이드하고 D4가 뚫린다 |
| D9 | `bg_sprite` 셋째 인자 | **제거.** | 받고 버리던 인자 |
| D10 | 스프라이트 키 | `Resources/Backgrounds/` 아래 **상대 경로, 확장자 없이** — 런타임이 `Resources.Load`에 넘기는 문자열 그대로 | 덤프도 같은 키를 쓴다. 이름만 쓰면 하위 폴더의 같은 이름이 서로를 덮는다(검토 지적) |
| D11 | 재선언(같은 키로 다시 세우기) | **새 리그.** 노드·α가 스키마로, 배역은 남는다 — 캐릭터 `slot`도 같다 | 런타임은 처음부터 그랬다(`SetupCharRigCommand` + `Register`). 폴드만 "재사용한다 — 좌표도 유지"로 접고 있었다 — 배경 교체가 매번 그 경로를 타서 하네스 불일치가 될 자리였다(검토 지적) |
| D12 | 배경 이미지 클릭 | **받지 않는다**(`raycastTarget` 끔) | 옛 배경 이미지는 꺼져 있었다. 캐릭터 프리팹은 켜져 있어, 그대로면 화면 전체가 레이캐스트를 받는다(검토 지적) |

## 명령 대응표

| 대본 | 펼쳐지는 캐릭터 Spec(파서) | 대상 노드 | 코어 |
|---|---|---|---|
| `bg_spawn k sprite` · `bg_slot0N k sprite [layer=far]` | `SetupCharRig`(stage, layer, `asBackground`) → `SetSprite`(키) | `CharacterPortraitSprite_Image` | `SpawnSlot` + `ClearCast` + 배경 사이징 (α 0 그대로) |
| `bg_sprite k sprite` | `SetSprite`(키, HeightFit, 레이캐스트 끔) | `CharacterPortraitSprite_Image` | 배경 사이징(`background-dimensions.json`의 종횡비) |
| `bg_fade_in k [10fr]` · `bg_fade_out` | `FadeIn/OutCommandSpecCharR` | `CharacterPortraitSprite_Root` | `ApplyFade` |
| `bg_move k x y [10fr]` | `MoveByCommandSpecCharR`(상대) | `CharSlot_Track` — `move_by`와 같은 축 | `ApplyMoveBy` |
| `bg_scale k s [10fr]` | `ScaleToCommandSpecCharR`(절대) | `CharacterPortrait_ActingScale` — 가운데 피벗 | `ApplyPortraitScaleTo` |
| `bg_size` · `bg_place` | — (표에서 뺐다) | — | 케이스 없음 → Unhandled |

> `CharSlot_Scale`은 **발 피벗**이다. 배경 확대를 거기 걸면 화면 아래 가운데를 중심으로 커진다.

## 한 것

### 무대 `ked-stage-runtime` (원본)

| 커밋 | 내용 |
|---|---|
| `a5c6471` S1 코어 | `StageReducer.Background.cs`, `BackgroundDimensionsFileDto` + `StageReducerTuning.BackgroundDimensions`, `SpawnSlot`(재선언 = 새 리그, D11), `StageState.ClearCast`(D8). 기존 테스트 `같은_슬롯_재선언은_부착만_갱신한다` → `…리그를_새로_세운다`. `BackgroundReductionTests` 15개. `reduction-boundary.md` |
| `fee00b0` S2 런타임·에디터·덤프 | 파서(`StageCommandParser.Background.cs`, GUID 유지) · 표에서 `bg_size`·`bg_place` 삭제 · 생성자에서 배경 프리팹 삭제 · `ParseSignedUnit`을 파서 본체로. `SetSpriteCommandSpecCharR`가 Resources 키 + `raycastTarget`, 못 찾으면 이미지를 끈다. `ResourcesSpriteResolver`(GUID 유지). `SetupCharRigCommandSpec.asBackground`. `DepthFocusCommandCharR` 필터. 배경 리그 전부 삭제(Runtime/BackgroundRig · Commands_BackgroundRig 9 · Factory · 프리팹 · Stage 레지스트리 · 블러 루프 · `IRigPrefabSource.BackgroundRigPrefab`). 익스포터: background 분기 삭제, `ExportBackgroundDimensions`(D10). `rig-schemas.json`에서 background 항목을 손으로 걷음, `background-dimensions.json` 손 덤프. 파서 테스트 4개. 문서(rig-axis-order §1.5 · ASSEMBLY-BOUNDARY · BLUR · transform-math · README · schema.md 2·4b) |
| `859281c` D7 | `bg_spawn`이 숨은 채 선다 — 파서는 `FadeIn 0초`를 더 내지 않고, 코어는 α를 1로 접지 않는다 |

### 재생 `ked-presentation-runtime` (이 저장소)

- 무대 반입(`Assets/Ked.Presentation/` Playback 밖 전부 + `ExportedTuning/`), `Documentation~/vendoring.md` 스냅샷 표.
- `Playback/YarnBridge`: `bg_place`·`bg_size` 등록 삭제, `bg_sprite` 2인자, 이름 `BindBackground`·`Enqueue…Background…`.
- `Playback/Diagnostics/StageEquivalenceHarness`: `LoadBackgroundDimensions`(없거나 비면 경고).
- 예제 배선: `PresentationAssembler`(배경 팩토리·빌더·해석기, 파서 인자) · `PresentationSceneRefs` · `SampleBootstrap`(필드·`IRigPrefabSource`).
  씬 `PresentationSample.unity`의 죽은 줄 `backgroundRigPrefab`(지운 프리팹 GUID)을 YAML에서 뺐다.
- 대본: 데모·진행 예제의 `<<bg_size bg_main 1>>` 세 줄 삭제, 데모 머리말에 규칙 한 줄. 이제 세 곳의 `bg_fade_in 24fr`이 보인다.
- `SCOPE-BOUNDARY.md` §2에 사례 한 단락.

## 검증

**오프라인 하네스**(이 컨테이너, dotnet 없음): Ubuntu mono + `mono/roslyn-binaries`(Roslyn 3.9, C# 9).
- Core 순수 테스트 — NUnit 2.6 콘솔 + NUnit 3 문법 shim. **413/413.**
- 전 어셈블리 컴파일 — PC의 Unity 6000.3.16f1 `Managed/UnityEngine/*.dll` · `NetStandard`(ref + compat shims) ·
  `Library/ScriptAssemblies`의 패키지 어셈블리(UGUI·TMP·Yarn Spinner·테스트 러너·UIPF) · Yarn DLL · Newtonsoft · NUnit을 받아
  asmdef를 읽는 스크립트로 Unity 순서대로 컴파일. 기준(`dev`) 18개 오류 0 → 이 브랜치 **18개 오류 0**.
  `SetAnchorCommandBgR._rigRefs` CS0649 경고(= `bg_place` 고장)가 사라졌다.
- Unity 타입을 쓰는 파서 테스트 — 리플렉션 러너로 Unity 밖에서 실행. 새 배경 테스트 4개 통과
  (기존 2개는 `LogAssert`·mono Span 때문에 Unity 밖에서 못 돈다 — 이 변경과 무관).
- PC에 남긴 것: `Library/claude-unityrefs.tgz`(22MB) · `Library/claude-netstd-compat.tgz` · `Library/claude-Newtonsoft.Json.dll`.
  `Library/`는 git이 무시한다 — 지워도 된다.

## Unity 확인 목록 (아미야)

순서가 있다 — **덤프 재생성이 대조 테스트보다 먼저다.**

1. 두 저장소에서 `refactor/background-as-slot`을 받는다. Unity를 닫고 바꾼다(프리팹 삭제·씬 수정).
2. 컴파일 — 새 경고 0, 누락 스크립트·참조 0. 씬을 한 번 저장했을 때 diff가 없어야 한다(죽은 줄은 이미 뺐다).
3. `Ked / U12 / Export Presentation Tuning Dump` → `rig-schemas.json`이 바이트 그대로인지, `background-dimensions.json`이 손 덤프와 같은지, `export-report.json` 경고 0.
   다르면 Unity 덤프가 진실이다 — 무대 저장소에 커밋하고 반입한다.
4. EditMode 테스트 전부(Unity 대조 포함).
5. 연출 데모: S1 배경이 **24fr 페이드로 들어온다**(처음 보는 모습). S4 `screen_blur`가 배경을 흐리고 `focus_on`은 배경을 흐리지 않는다. S6 트랜지션 아래 배경 교체. S7 `bg_fade_out`.
6. 진행 예제: 두 장면 모두 배경이 페이드로 들어온다. 장면 경계·이어하기·되감기에서 배경이 다시 선다.
7. 하이어라키: `bg_main_CharacterRig` 아래 25노드(덤프는 루트 포함 26), 배경 Image의 Raycast Target이 꺼져 있다. 배경 위 UI가 클릭을 받는다.
8. 21:9: 배경 양옆에 무엇이 보이는지(흰 바탕이 사라졌다).
9. 등가성 하네스: 예제 장면에서 불일치 0, `finalUnhandled`에 `bg_*` 없음.
10. (선택) 임시 대본 `slot s1` → `cast s1 harin` → `bg_spawn s1 class_day` → 경고 1건, `fade_in harin`이 배경을 건드리지 않는다.

## 하지 않은 것 · 남은 것

- `Assets/Ked.CommandExecutor/Base/CanvasFadeCommandBase.cs` 주석 "Char/Bg 4종" — 실행기는 또 다른 원본(`ked-command-executor`)이라 손대지 않았다. 다음에 실행기를 만질 때 함께.
- 생성된 ysls(`ProjectSettings/Packages/dev.yarnspinner/*.ysls.json`)의 `bg_size`·`bg_place` 항목 — Yarn Spinner가 Unity 컴파일 때 다시 만든다.
- VnTool: `rig-schemas.json`의 `background` 항목이나 `bg_size`·`bg_place`를 읽고 있다면 그쪽을 고친다.
- `CharacterRig` 이름 바꾸기(D6), 마스크·클립 복원, `bg_*` 어휘 확장 — 하지 않는다.
- 캐릭터 `slot`도 재선언 의미가 바뀌었다(D11) — 런타임 동작은 그대로이고 폴드만 런타임에 맞췄다. 같은 키를 장면 안에서 다시 `slot`하던 대본이 있었다면 하네스가 그동안 거짓 일치를 냈을 수 있다.

## 위험

| 위험 | 대응 |
|---|---|
| 손으로 낸 덤프가 Unity 덤프와 다름 | 확인 3. 스프라이트 rect는 임포터 상한 2048로 계산했다(3840×2160 → 2048×1152) — 다른 임포트 설정이 있으면 어긋난다 |
| `bg_fade_in` 없이 `bg_spawn`만 쓴 외부 대본 | 배경이 안 보인다(D7). 이 저장소 대본에는 없다 |
| 같은 키로 캐릭터를 다시 `slot`하면 배역은 남고 그림은 비는 리그가 선다 | 전부터 그랬다(런타임). 폴드도 이제 같다(D11) |
| 캐릭터 `slot`이 배경 키를 덮는 경우 | 감지하지 않는다 — 배역이 없는 쪽이라 해가 적다. 필요하면 레지스트리가 종류를 기억하게 한다 |
