# 어셈블리 경계 (U13-a, 2026-10-26 Playback 분리)

이 폴더(`Assets/Scripts/Ked.Presentation.Runtime/`)는 **하나의 어셈블리**다. 왜 이렇게 생겼는지를 여기 남긴다.
경계는 코드 어디에도 안 보이고 `.asmdef` 파일 안에만 있어서, 근거가 없으면 다음 사람이
"왜 폴더가 이렇게 묶여 있지"를 알 수 없다.

**목적**: `Ked.Presentation.Core`(순수 C# 상태 계산 층)를 뽑아 외부 도구(VnTool)가
참조할 수 있게 하는 것. U13-a에서 경계를 긋고, **U13-b에서 실제로 뽑았다** —
지금은 VnTool에 `src/Ked.Presentation.Core`로 복사 반입돼 있다.

---

## 1. 어셈블리

| 어셈블리 | 위치 | 내용 |
|---|---|---|
| `Ked.Presentation.Core` | `Assets/Scripts/Ked.Presentation.Core/` | 순수 상태 계산 — 정지 프레임 리듀스·이징·튜닝 (**엔진 의존 0**, 참조 0) |
| `Ked.Presentation.Runtime` | `Assets/Scripts/Ked.Presentation.Runtime/` | 연출 실행 계층 — PresentationCore · Commands · CharacterRig · ShotResponse · BackgroundRig · ScreenEffect · StageMask · Audio (참조: Core · TextMeshPro · DOTween.Modules) |
| `Ked.Presentation.Playback` | `Assets/Scripts/Ked.Presentation.Playback/` | 대사 재생 루프와 연출 기억 — 줄 진행·선택지(DialoguePresenter) · 재생 제어·롤백 기록·백로그(FeatureController) · Yarn 커맨드 번역(YarnBridge) · 재생 호스트 계약·재생 단위(Host) · 연출 기억 스냅샷(Memory) (참조: Core · Runtime · YarnSpinner.Unity · TextMeshPro · UnityEngine.UI · DOTween.Modules) |
| `Ked.Progression` · `Ked.Progression.Save` | `Assets/Scripts/Ked.Progression/` | 진행 판정 코어와 진행 저장 (**엔진 의존 0**. 원본은 형제 저장소 `ked-progression-runtime`, [vendoring.md](../Ked.Progression/Documentation~/vendoring.md)) |
| `Assembly-CSharp` (기본) | `Assets/Scripts/` 나머지 | **예제 층** — Game(부트스트랩·조립·저장 흐름) · UI · ProgressionHost(진행 런타임을 꽂는 어댑터) |
| `Assembly-CSharp-Editor` (기본) | `Assets/Editor/` | 에디터 도구 — 초상 DB 빌더 · 튜닝 덤프 익스포터 · 폰트 교체 |
| `Ked.Presentation.Core.Tests` | `Assets/Scripts/Ked.Presentation.Core/Tests/EditMode/` | EditMode 테스트 (참조: Core · Runtime) |

> **에디터 도구는 런타임 폴더 안에 두지 않는다.** 한때 런타임 트리 안에 에디터 전용
> 어셈블리가 하나 더 있었으나, 그러면 폴더를 읽는 사람이 매번 "이건 빌드에 들어가나"를
> 확인해야 한다. `Assets/Editor/`로 모으면 **위치가 곧 답이다.**

**의존 방향은 한쪽뿐이다.**

```
Assembly-CSharp (예제) ──> Ked.Presentation.Playback ──> Ked.Presentation.Runtime ──> Ked.Presentation.Core
        └────────────────> Ked.Progression.Save ──> Ked.Progression
        ✗ 역방향 불가 (컴파일러가 막는다)
```

연출 쪽 셋과 진행 쪽 둘은 서로 모른다. 둘을 잇는 것은 예제 층의 `ProgressionHost/`뿐이다.
순수 코어(`Ked.Presentation.Core` · `Ked.Progression` · `Ked.Progression.Save`)는 유니티도 모른다.

---

## 2. 왜 일곱 폴더가 한 어셈블리인가

두 사실이 겹친 결과다.

1. **`.asmdef`는 자기 폴더 트리를 덮지, 형제 폴더를 묶지 못한다.**
2. **코어 폴더들은 서로 순환한다** — `Commands ↔ CharacterRig`, `CharacterRig ↔ ShotResponse`,
   `ScreenEffect ↔ CharacterRig`, `ScreenEffect ↔ BackgroundRig` …
   어셈블리 그래프는 비순환이어야 하므로 이들은 **한 어셈블리일 수밖에 없다.**

그래서 `Runtime/` 부모 폴더를 만들어 모았다. 폴더를 나누고 싶으면 먼저 폴더 간 순환을 없애야 한다.

## 3. 대사 재생 루프는 왜 이름 있는 어셈블리가 됐나 (2026-10-26)

처음에는 대사 재생 루프(줄 진행·롤백·백로그·시크·Yarn 브리지)를 "글루"로 보고 기본 어셈블리에 두었다.
기본 어셈블리는 모든 asmdef를 자동 참조하므로 "글루 → 코어" 단방향이 공짜로 성립하고, asmdef를 씌워 얻는 것이 없었다.

롤백·백로그·시크·선택 기록이 **이 저장소의 상품**이라고 정하면서(`docs/PLAN_playback-unit.md` E1·E5) 이유가 생겼다.
예제(UI·Game·ProgressionHost)와 같은 어셈블리에 있으면 "연출이 스스로 기억하는 것"의 경계가 코드에 보이지 않고,
그 안쪽이 예제 타입을 집어도 컴파일러가 막지 않는다. 그래서 `Ked.Presentation.Playback`으로 묶었다.

기본 어셈블리에 남은 것은 **예제뿐**이다. 예제에 asmdef가 없는 이유는 처음과 같다 — "예제 → 상품" 단방향이 공짜로 선다.

옮길 때 걸린 것은 셋이었다.

- **대사창·선택지창 뷰 베이스**(`PresentationDialogueBoxViewBase`, `PresentationOptionsBoxViewBase`, `VNDefaultOptionsPanel`)는
  UI 프레임(`UIBase`)을 상속한다. `UI/DialogueBox/`에 남겼고, Playback은 그 계약(`IPresentationDialogueBoxView` · `IPresentationOptionsBoxView`)만 안다.
- **예제 조립부**(`PresentationAssembler`, `PresentationSceneRefs`)는 UI 매니저·패치 서비스를 세운다. `Game/`으로 옮겼다.
- **연출 기억 코덱**(`PresentationMemory`, 예전 `ProgressionHost/PresentationSaveState`)은 진행 저장의 JSON 설정을 빌려 쓰고 있었다.
  자기 설정을 갖게 해 진행 참조를 끊었다. 형식(camelCase)은 그대로다.

## 4. `YarnBridge`는 Runtime이 아니라 Playback에 있다

`YarnCommandBridge`는 Yarn 텍스트를 `CommandSpec`으로 옮기는 **번역층**이지 커맨드 실행이 아니다.
실행은 `Runtime/Commands/Commands/`와 `Runtime/Commands/CommandFactory/`가 한다.
브리지를 Runtime 밖으로 보내면서 Runtime의 `Yarn.Unity` 참조가 **0**이 됐다 — 코어가 Yarn을 모르는 상태가
U13-b(순수 코어 추출)의 전제다. Yarn을 아는 것은 Playback부터다.

## 5. ⚠ `partial class`는 어셈블리를 넘을 수 없다

이 작업에서 **세 번 걸린 함정**이다. 참조는 인터페이스로 끊을 수 있지만,
같은 타입의 조각이 두 어셈블리에 있으면 방법이 없다.

이 저장소는 "provider 계약과 그 구현을 한 파일에 둔다"는 관습이 있었다. 읽기엔 좋지만
경계에서는 정확히 벽이 된다 — 계약은 코어가, 구현은 UI가 가져야 하는데 한 파일이라 못 가른다.

**그래서 생긴 것이 `Assets/Scripts/UI/Providers/`다.**
UI 타입이 기능 폴더의 계약을 구현하는 partial을 전부 여기 모았다.

| UI/Providers/ | 구현하는 계약 | 계약의 위치 |
|---|---|---|
| `PresentationUIRoot.ShotResponseStage.cs` | `IShotResponseStageProvider` | `Runtime/ShotResponse/` |
| `PresentationUIRoot.StageDepthContentSlot.cs` | `IStageDepthContentSlotProvider` | `Runtime/PresentationCore/` |
| `PresentationUIRoot.StageMask.cs` | `IStageMaskProvider` | `Runtime/StageMask/` |
| `PresentationUIRoot.StageCounts.cs` | (계약 아님 — enum을 세는 private const) | enum은 `Runtime/PresentationCore/` |

> **새 provider를 만들 때**: 계약(interface)은 그것을 필요로 하는 기능 폴더에,
> 구현 partial은 `UI/Providers/`에. **한 파일에 같이 두지 말 것.**

## 6. 코어가 바깥을 필요로 할 때 — 포트

코어가 호스트에게 무언가 요구해야 하면 `Runtime/PresentationCore/Execution/CommandRuntimePorts.cs`에
인터페이스를 두고, 구현은 바깥 어셈블리(Playback 또는 예제)가 갖는다. 기존 예:

- `IInputSource` · `ITimeSource` · `ISignalBus` · `ISignalLatch`
- `IUIThemePatchPort` — `ui_patch` 커맨드용. 구현은 `UI/Core/Patcher/UIThemePatchAdapter.cs`
  ("현재 화면"은 `UIManager`만 알고 "패치 방법"은 `UIPatchService`만 알아서, 둘을 아는 어댑터가 필요했다)
- `ISeekStateQuery` (`PresentationCore/ISeekStateQuery.cs`) — 코어가 `VNLinePresentationState`(Playback)에서
  실제로 쓰는 것은 `IsSeekingActive` 하나뿐이라 그것만 잘라 냈다

Playback이 예제에게 요구할 때도 같다. 계약은 Playback에, 구현은 예제에 —
대사창 뷰(`IPresentationDialogueBoxView`)와 화면 전환(`IPresentationView`)이 그렇다.

## 7. ⚠ DOTween 의존

`DOAnchorPos`(RectTransform) · `DOFade`(CanvasGroup) 같은 확장은 **DOTween 코어 DLL이 아니라
`Assets/Plugins/Demigiant/DOTween/Modules/`의 소스 파일**에 있다. asmdef 없이 두면 그 소스가
`Assembly-CSharp`에 컴파일되고, 코어는 기본 어셈블리를 참조할 수 없어 확장이 안 보인다.

그래서 `Modules/DOTween.Modules.asmdef`를 만들고 코어가 참조한다.

> **DOTween을 업데이트하면 이 asmdef가 지워질 수 있다.**
> 그때는 DOTween 유틸리티 패널의 "Create ASMDEF"로 다시 만들면 된다(같은 이름을 쓴다).

---

## 8. 경계 오류를 만났을 때

컴파일러가 "type or namespace not found"를 내면 대개 **경계를 넘으려 한 것**이다. 순서대로 물어보라.

1. **안쪽 어셈블리의 코드가 바깥 타입을 쓰려는가?** (Core ← Runtime ← Playback ← 예제)
   → 그 코드가 정말 안쪽 것인지 다시 본다. 아니면 바깥으로 옮긴다(`YarnBridge`, 뷰 베이스가 그랬다).
   → 안쪽이 맞다면 **포트 인터페이스**를 만든다(§6).
2. **같은 타입의 `partial`이 양쪽에 있는가?**
   → 파일을 가른다(§5). 인터페이스로는 못 푼다.
3. **확장 메서드가 안 보이는가?**
   → 그 확장이 어느 어셈블리에 있는지 본다. `Assembly-CSharp`이면 asmdef가 필요하다(§7).

## 9. `Ked.Presentation.Runtime`은 "순수 코어"가 아니다

이 어셈블리는 **의도적으로 `UnityEngine`·DOTween·`MonoBehaviour`에 의존한다** — 실행 계층이다.
순수 상태 계산은 U13-b에서 `Ked.Presentation.Core`로 뽑혀 나갔고(§1),
이 어셈블리는 그 계산을 실제 무대에 재생하는 쪽을 맡는다.

참고 수치(U13-a 착수 시점, `Assets/Scripts` 572파일 기준):

- `Commands/` 116파일 중 `MonoBehaviour`는 **1개**, `PresentationCore/` 54파일 중 **2개**
  → 엉킴은 `MonoBehaviour`가 아니라 **`RectTransform`을 상태 담지체로 쓰는 것**(402회)이다
- 커맨드 51개가 이미 "목표값 계산(`ClaimTarget`)"과 "트윈"을 나눠 두었다
  → 정지 프레임(2b)에 필요한 것은 앞쪽뿐이고, DOTween 의존은 뒤쪽에 몰려 있다
- `ShotResponse/CharacterPlacementTargetLedger.cs`가 "트윈이 끝났다면 어디 있을 것인가"를
  이미 별도 딕셔너리로 들고 있다 → `StageState`의 원형이다
