# 메신저 — 지나온 줄을 말풍선으로 그리는 대사창 (설계, 2026-11-09)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/subSystem` |
| 왜 | 원본 게임의 메신저 장면을 이 런타임의 대본으로 쓰고 재생한다 |
| 결정 | 아미야(소유자) 2026-11-09: ① 지금 시스템을 그대로 쓴다 — 메신저 줄은 **표시가 말풍선인 일반 대사 한 줄**이다. ② 기록은 뷰가 아니라 데이터로 둔다(지나온 줄의 목록, 미리 굽지 않는다). ③ 구간은 커맨드로 열고 닫는다. ④ 타자기를 쓰지 않고 말풍선을 한 번에 띄운다. ⑤ `•••`는 줄이 아니라 대기 표시다. ⑥ 상대는 왼쪽, 나는 오른쪽. 헤더는 이번에 하지 않는다 |
| 상태 | **구현** (2026-11-09) — Unity 확인 대기(§6.1). 이 환경에는 Unity · C# 컴파일러가 없어 컴파일과 테스트를 돌리지 못했다 |

## 목표 한 문장

> 메신저 구간이 열려 있는 동안, 들어오는 줄은 **메신저 기록**에 한 줄씩 더해지고, 메신저 뷰는 그 기록을 그린다.

화면이 "지금 줄 하나"인 대사창과 달리 메신저의 화면은 "지금까지 온 줄들"이다. 그 "지금까지"를 뷰(UI 객체)에 쌓지 않고 데이터로 둔다 — 그러면 되감기 · 백로그 점프 · 로드 · 빨리 넘김에 메신저만의 길이 하나도 생기지 않는다.

## 1. 원본 (GIF, 1240×680, 26.8초)

- 무대는 계속 돈다. 메신저 패널은 그 위에 뜬다.
- 줄마다 말풍선 하나. 상대 = 왼쪽 흰색, 나 = 오른쪽 파랑. 아바타는 이어 보낸 묶음의 마지막 말풍선에.
- `•••` = 입력 대기 표시. 원본은 다음에 말할 사람 쪽에 띄운다 — 이 런타임은 기다리는 동안 다음 줄을 모르므로 한쪽에 둔다. UI는 재현 비교 밖이다.
- 쌓이면 위로 밀려 잘린다.

## 2. 대본

```yarn
<<messenger_open 코하쿠>>          // 구간을 연다. 인자 = "나"(오른쪽). 나머지 화자는 모두 왼쪽
닉스: 지금 어디야?
닉스: 회의 시작했어
코하쿠: 가는 중!
<<messenger_close>>                // 구간을 닫는다
코하쿠: (큰일이다…)                 // 여기부터 다시 대사창
```

- 구간 안의 줄은 **모두** 말풍선이다. 줄 태그는 쓰지 않는다. 나레이션을 대사창으로 내려면 닫고 쓴다.
- 화자 없는 줄은 왼쪽("내가 아닌 것")에 둔다. 가운데 시스템 글은 §8 나중.
- `messenger_open`을 열린 채 다시 부르면 새 대화다(기록을 비우고 "나"를 다시 정한다). 열리지 않은 `messenger_close`는 아무 일도 없다.
- 두 커맨드는 `surface_layout`과 같은 **즉시 상태 변경**이다(큐에 넣지 않는다) — 화면은 건드리지 않고, 다음 줄의 표시가 그 상태를 읽는다. 닫아도 패널은 다음 줄의 전환이 감출 때까지 그대로 보인다.

## 3. 데이터 — `MessengerLog` (순수 C#)

```csharp
public enum MessengerSide { Left = 0, Right = 1 }

public readonly struct MessengerEntry
{
    public readonly string Speaker;   // 대본의 화자 이름(없으면 빈 문자열) — 아바타 · 묶음의 키
    public readonly string Text;
    public readonly MessengerSide Side;
}

public sealed class MessengerLog
{
    public bool IsOpen { get; }
    public IReadOnlyList<MessengerEntry> Entries { get; }

    public void Open(string me);                     // 기록을 비우고 연다
    public void Close();                             // 닫고 기록을 비운다
    public void RecordLine(string speaker, string text);   // 열려 있을 때만 더한다. 쪽 = 화자가 "나"면 오른쪽
    public void Reset();                             // 장면 경계
}
```

| 언제 | 누가 | 무엇 |
|---|---|---|
| `<<messenger_open>>` · `<<messenger_close>>` | `YarnCommandBridge` (`surface_layout` 옆) | `Open` · `Close` |
| 줄 진입 | `YarnLineBoundary.CommitLineEntered` — 백로그 · 롤백 포인트를 적는 그 자리 | `RecordLine(meta.charName, meta.rawText)` |
| 장면 경계 | `ScenePresentation.BeginScene` · `PrepareReplay` — `_dialogueSurfaceState.Reset()` 옆 | `Reset` |

- 줄 진입은 **시크 통과 줄도 지나간다**. 그래서 되감기 · 로드가 루트부터 다시 달리면, 커맨드가 다시 열고 줄이 다시 적혀서 표적 줄에 닿았을 때 기록이 일반 재생 때와 같다.
- 메신저 기록은 `LineRecordDecision`을 보지 않고 **항상** 적는다. 백로그는 되감기 너머로 살아남아서 다시 적지 않지만, 메신저 기록은 장면 경계에서 비워지고 매번 다시 선다.
- 미리 굽지 않는다. Yarn은 줄을 하나씩 주고, 구간 안에 분기 · 변수가 있으면 뒤를 미리 알 수 없다.

## 4. 표시

### 4.1 뷰 계약

전환 코드가 쓰는 공통 부분(보이기 · 감추기 · 페이드)만 부모로 뺀다. 기존 구현(`KedDialogueBoxView`)은 바뀌지 않는다.

```csharp
public interface IPresentationLineSurfaceView
{
    void SetVisibleImmediate(bool visible);
    void PrepareHidden();
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}

// 지금 것 그대로 — 선언만 부모를 잇는다.
public interface IPresentationDialogueBoxView : IPresentationLineSurfaceView
{
    TMP_Text GetLineText();
    TMP_Text GetNameText();
    void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry);
    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);
}

public interface IPresentationMessengerView : IPresentationLineSurfaceView
{
    // 기록 전체를 그린다. animateLast면 마지막 말풍선만 등장 연출, 나머지는 선 채로.
    // 몇 번을 불러도 같은 기록이면 같은 화면이다.
    void Render(IReadOnlyList<MessengerEntry> entries, bool animateLast);
}
```

메신저 뷰의 입구는 `Render` 하나다. 기록을 뷰가 들고 있지 않으므로 "더하기" · "비우기" · "시크용 조용한 더하기"가 따로 없다.

### 4.2 `DialogueBoxPresentationController`

- `DialogueBoxKind.Messenger`를 더한다. 종류 판정(`ResolveNextBoxKind`)의 맨 앞: **기록이 열려 있으면 `Messenger`**. 그 뒤는 지금 그대로(줄 태그 → 화자 정책 → 기본). 열리지 않았는데 `#messenger` 태그가 오면 경고하고 기본 종류로 간다.
- 생성자에 `MessengerLog`와 `IPresentationMessengerView`(없어도 됨)를 더한다. 뷰가 없는데 구간이 열리면 경고 한 번 + 대사창으로 보여 준다.
- `DialogueBoxCurrentState.Box`의 형을 `IPresentationLineSurfaceView`로 넓힌다 — "지금 보이는 뷰"가 전환에서 **나가는 뷰**다.
- 내용 얹기(`ApplyContent`): 메신저면 `Render(log.Entries, animateLast: !immediate)`, 아니면 지금 그대로.

전환표 (나가는 뷰 `out` = `_boxState.Box`, 들어오는 뷰 `in`):

| 전환 | 뷰가 둘일 때 |
|---|---|
| `Keep` | 종류가 같으니 `out == in`. 메신저 → 메신저면 `Render`가 새 말풍선 하나를 더해 그린다 |
| `Cut` | `out != in`이면 `out`을 즉시 감추고, `in`에 내용 + 보이기 |
| `FadeIn` | 지금 그대로. 되감기 도착 줄이 이 길 — 쌓인 기록과 함께 패널이 뜬다 |
| `FadeOutIn` | `out` 페이드 아웃 → `out` 감춤 → `in`에 내용 → `in` 페이드 인 |
| `Hide` | `out` 페이드 아웃 → `out` 감춤 → `in`에 내용(감춘 채) |

- 메신저에서 나갈 때 뷰는 마지막으로 그린 말풍선을 그대로 든 채 사라진다(기록은 이미 `Close`로 비었지만 화면은 다시 `Render`받을 때까지 그대로) — 순서를 맞추는 장치가 필요 없다.
- `CloseAll()` — 두 뷰를 감추고 상태를 리셋한다. **기록은 건드리지 않는다**(시크 통과가 줄마다 부른다).
- `CleanupStale()` — 커밋된 뷰만 커밋된 대로 보이고 다른 뷰는 감춘다.

### 4.3 `LinePresentationFlow` — 타자기 건너뛰기

고치는 곳은 하나다. `ShowLineAsync` 뒤, 이 줄이 메신저면 타자기 단계(`SetTextView` · `PrepareForContent` · `RunTypewriter`)를 건너뛰고 바로 `DisplayCommitted`로 간다.

```csharp
if (_boxPresentation.UsesTypewriter)   // 메신저 줄이면 false
{
    // 지금의 TypewriterReady → TypewriterCompleted
}
```

- 그 밖의 단계(진입 기록 · 보이스 · BGM · 블러 · `LineDisplayed` · 전진 대기 · `#auto`)는 그대로다.
- **시크 통과(`RunSeekPassThrough`)는 고치지 않는다** — `CloseAll()` 그대로. 기록은 줄 진입에서 이미 적혔다.
- 메신저 줄의 인라인 마크업 이벤트(타자기 도중에 도는 것)는 돌지 않는다. 글은 마크업을 뗀 평문이다.

### 4.4 `•••` — 예제 UI의 대기 표시

다음 표시는 지금도 본체 계약이 아니라 예제 UI의 일이다(`KedUIFlow.Play.HandleLineDisplayed` → `PlayScreen.SetNextMarkerVisible`). 메신저도 같은 자리에 건다.

- `HandleLineDisplayed`가 `KedMessengerView.SetWaiting(true)`도 부른다. 뷰는 **패널이 보일 때만** 열 끝에 `•••` 말풍선을 붙인다.
- `Render`가 `•••`를 뗀다 — 다음 줄의 말풍선이 그 자리에 선다.
- 기록에 `•••`는 없다. 백로그 · 되감기 · `#auto`에 흔적이 남지 않는다.

## 5. 되감기 · 백로그 · 저장/로드 · 빨리 넘김 — 고칠 것 없음

| 기능 | 실제로 도는 길 | 메신저 |
|---|---|---|
| 한 걸음 되감기 | 정지 → `PrepareReplay` → 노드 **루트부터** 다시, 표적 줄까지 시크 통과 | `Reset` → 커맨드가 다시 열고 통과 줄이 다시 적힌다 → 표적 줄의 `Render`가 전부 그린다 |
| 백로그 점프 | 항목 → 롤백 포인트 → 위와 같은 길 | 위와 같다 |
| 로드 · 이어하기 | `BeginScene` → 루트부터 `SeekKind.Load` 시크 | 위와 같다 |
| 저장 | 줄 좌표 + 백로그 문자열 + 진행. 화면 상태는 저장하지 않는다 | 저장할 것 없음 |
| 백로그 기록 | 줄 진입에서 줄마다 | 말풍선 하나 = 항목 하나 |
| 빨리 넘김 · 배속 | 줄을 하나씩 즉시 전환(`Cut`)으로 넘긴다 | 타자기가 없으니 말풍선이 차례로 바로 선다 |
| 줄 중단(stale) | `StopAsync`에서만 오고, 뒤는 반드시 `PrepareReplay`/`BeginScene` | `Reset` + `CloseAll` |

- 메신저 **앞** 줄로 되감으면: 여는 커맨드가 아직 돌지 않았으니 기록은 닫혀 있다.
- 메신저 **뒤** 줄로 되감으면: 통과하며 열렸다 닫힌다 — 표적 줄에서는 닫혀 있다.
- 롤백 도착 줄은 `#auto`를 무시한다(`LineAdvancePolicy`) — 그대로.

## 6. 단계

각 단계는 커밋하고 확인받은 뒤 다음으로 간다.

| 단계 | 무엇 | 확인 |
|---|---|---|
| 1 | `MessengerLog` · 커맨드 둘 · 줄 진입 기록 · 장면 경계 리셋 (화면은 아직 그대로) | EditMode 테스트: 열기 전 줄은 안 적힌다 · 쪽 · 다시 열면 새 대화 · 닫으면 빈다 · `Reset`. 기존 테스트 전부 |
| 2 | `DialogueBoxKind.Messenger` · 뷰 계약 분리 · 컨트롤러의 두 뷰 전환 · 타자기 건너뛰기 | EditMode 테스트(가짜 뷰 둘): ① 열린 구간의 줄은 메신저 뷰에 기록 전체가 `Render`된다 ② 메신저 → 대사창 `FadeOutIn`의 호출 순서 ③ `CloseAll`은 기록을 건드리지 않는다 ④ 메신저 뷰가 없으면 대사창으로 떨어진다. 메신저 뷰 없이 기존 대본 · 되감기 · 빨리 넘김이 같다 |
| 3 | 예제 UI `PlayMessengerPanel` · `KedMessengerView` — 말풍선 · 좌우 · 아바타 묶음 · `•••` · 넘치면 위로 밀기 | 아미야 Unity |
| 4 | 데모 메신저 대본 · README 어휘 표 · `SCOPE-BOUNDARY.md` §4 | §6.1 |

구현하며 달라진 것:

- **단계 3의 패널은 프리팹이 아니라 코드로 선다**(`PlayMessengerPanel` — `PlayScreen`이 대사창 틀 바로 아래에 세운다). 빌더 · 프리팹 · `Refs`를 건드리지 않아 빌더를 다시 돌릴 필요가 없다. 그림은 색 판, 아바타는 화자 이름 첫 글자다 — 모양을 다듬을 때 프리팹으로 옮긴다.
- 패널은 무대를 살짝 누르는 막(검정 0.35)을 함께 띄운다.
- **단계 2의 컨트롤러 테스트(가짜 뷰)는 쓰지 않았다.** `LocalizedLine`을 손으로 만들어야 하는데 컴파일해 볼 수 없는 환경이라, 테스트 어셈블리 전체를 깨뜨릴 위험을 지지 않았다. 들어간 것은 `MessengerLogTests`(순수 C#)뿐이고, 이를 위해 테스트 asmdef에 `Ked.Presentation.Playback` 참조를 더했다.
- README에는 커맨드 어휘 표가 없어(팝업 때도 고치지 않았다) 그대로 두었다. `SCOPE-BOUNDARY.md` §4의 "한 라인의 표시 트랜잭션"은 문구가 그대로 맞아 고치지 않았다.

### 6.1 Unity 확인 목록 (아미야)

1. 컴파일 오류 0, 새 `.meta` 함께 커밋.
2. 데모 대본 일반 재생: 좌우, 아바타 묶음, `•••` → 다음 줄에서 말풍선으로, 닫으면 대사창으로.
3. 메신저 **도중** 줄에서 되감기 · 백로그 점프 → 앞 말풍선이 트윈 없이 서 있고 일반 재생과 같다.
4. 메신저 **앞** 줄로 되감기 → 메신저가 없다.
5. 메신저 도중 저장 → 타이틀 → 이어하기 → 3과 같다.
6. 빨리 넘김 · 배속으로 메신저를 지난다.

## 7. 고치는 파일

| 파일 | 무엇 |
|---|---|
| `Box/MessengerLog.cs` (새) | §3 |
| `Box/IPresentationLineSurfaceView.cs` · `IPresentationMessengerView.cs` (새) | §4.1 |
| `Box/IPresentationDialogueBoxView.cs` | 부모 선언 |
| `Box/DialogueBoxKind.cs` | `Messenger = 6` |
| `Box/DialogueBoxCurrentState.cs` | `Box`의 형 |
| `Box/DialogueBoxPresentationController.cs` | §4.2 |
| `Line/YarnLineBoundary.cs` | `RecordLine` 한 줄 |
| `Line/LinePresentationFlow.cs` | 타자기 건너뛰기 분기 |
| `YarnBridge/CommandBridge*.cs` | 커맨드 둘 |
| `Host/ScenePresentation.cs` | `Reset` 두 곳 |
| `Samples/…/SampleBootstrap.Presentation.cs` · `KedUIHost.cs` · `KedUIFlow.Play.cs` | 조립 · `•••` |
| `Samples/…/PlayMessengerPanel.cs` · `KedMessengerView.cs` · `PlayScreen.cs` (새 둘) | 단계 3 — 프리팹 없이 코드로 |
| `Tests/EditMode/MessengerLogTests.cs` (새) · 테스트 asmdef | 단계 1 |
| `presentation_demo_Demo_Presentation.yarn` | 데모 구간(팝업 뒤) |

## 8. 나중

- 헤더(방 · 상대 이름, 상태 글) — `messenger_open`에 인자를 더하거나 따로 커맨드.
- `•••`를 다음 화자 쪽에 — 필요해지면 줄 태그로 힌트.
- 화자 없는 줄의 가운데 시스템 글.
- 아바타 그림의 출처(화자 → 그림). 단계 3에서 예제 UI 안의 표로 시작한다.
- 구간 안의 선택지.
- 그림 · 이모티콘 말풍선(수집기의 `chat` · `emojis/phone`).
