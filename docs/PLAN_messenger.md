# 메신저 — 말풍선을 쌓는 대사창 (설계, 2026-11-09)

| | |
|---|---|
| 브랜치 | `ked-presentation-runtime` · `feat/subSystem` |
| 왜 | 원본 게임의 메신저 장면을 이 런타임의 대본으로 쓰고 재생한다 |
| 결정 | 아미야(소유자) 2026-11-09: 지금 시스템을 그대로 쓴다. 메신저는 **대사창 종류 하나**다 — 대사창이 받은 줄로 글 칸을 채우듯, 메신저는 받은 줄로 말풍선을 만든다. 상대는 왼쪽, 나는 오른쪽. 헤더(상대 이름 · 상태 글)는 이번에 하지 않는다 |
| 상태 | **설계** — 아래 §7의 결정 셋을 받으면 단계 1부터 구현 |

## 목표 한 문장

> 메신저는 `DialogueBoxKind.Messenger`의 뷰다. 줄이 오면 말풍선 하나를 채우고, 줄 표시가 끝나면 `•••` 말풍선을 붙여 두었다가, 다음 줄이 그 말풍선의 내용을 교체한다.

줄 흐름(`LinePresentationFlow`) · Presenter · 타자기 · 전진 대기 · 백로그 · 롤백 기록 · 저장은 **그대로**다. 고치는 곳은 대사창 컨트롤러와 시크 통과 한 줄이다.

## 1. 원본 (GIF, 1240×680, 26.8초)

- 무대는 계속 돈다. 메신저 패널은 그 위에 뜬다.
- 줄마다 말풍선 하나. 상대 = 왼쪽 흰색, 나 = 오른쪽 파랑. 아바타는 이어 보낸 묶음의 마지막 말풍선에.
- `•••` = **입력 대기 표시**(대사창의 Space 표시와 같다). 원본은 다음에 말할 사람 쪽에 띄운다 — 이 런타임은 기다리는 동안 다음 줄을 모르므로 한쪽에 두고 다음 줄에서 교체한다. UI는 재현 비교 밖이다.
- 쌓이면 위로 밀려 잘린다.
- 헤더는 사람 이름이 아니라 **방 이름**일 수 있다(첨부 화면: 말풍선 아이콘 + "일은 끝이 없다" + 상태 글). §7 결정 1의 근거.

## 2. 불변식 하나

> **말풍선 열은 "끊기지 않은 메신저 줄의 구간" 동안만 산다.**
> 메신저가 아닌 줄(보이든, 시크로 지나가든)과 장면 경계(`CloseAll`)가 열을 비운다.

이 한 줄이 일반 재생 · 되감기 · 백로그 점프 · 로드 · stale을 모두 같게 만든다. 메신저는 따로 저장하거나 되감을 것이 없다 — **루트부터 다시 지나가면 같은 열이 다시 선다.**

## 3. 지금 흐름에 얹는 자리

| 지금 흐름 | 대사창 | 메신저 |
|---|---|---|
| `ShowLineAsync` → 내용 얹기 | `ApplySurfaceLayout` → `PrimeText(글, 이름)` — 글 칸을 바꾼다 | `PrimeBubble(글, 이름, 쪽)` — `•••` 말풍선이 있으면 그것을, 없으면 새 말풍선을 채운다 |
| `LineTextTarget` (`GetLineText()`) | 대사창 글 칸 | 그 말풍선의 글 칸 — 타자기가 여기에 쓴다 |
| `LineDisplayed` → 다음 표시 (예제 UI) | `SetNextMarkerVisible(true)` | `•••` 말풍선을 붙인다 |
| 다음 줄이 다른 종류 | — | 기존 전환(`FadeOutIn` · `Hide`)으로 닫고, 닫힌 뒤 열을 비운다 |
| 시크 통과 줄 | `CloseAll()` | 감춘 채 말풍선만 더한다(§5) |
| 장면 경계 `CloseAll()` | 감춘다 | 감추고 열을 비운다 |

- 종류는 줄 태그 `#messenger`(`DialogueBoxKindParser`가 `Enum.TryParse`로 그대로 받는다 — 파서 수정 없음) 또는 화자 정책의 `useBoxKindOverride`.
- 타자기는 `maxVisibleCharacters`만 건드린다. 말풍선은 글 전체로 크기가 잡힌 채 뜨고 글자만 차례로 열린다.

## 4. 코드 — 무엇을 어떻게 고치나

### 4.1 뷰 계약 — 공통 부분을 위로 뺀다

지금 `IPresentationDialogueBoxView`의 절반(레이아웃 프리셋 · 이름 칸 · `PrimeText`)은 메신저에 뜻이 없고, 메신저에는 "쪽"이 필요하다. 전환 코드가 쓰는 공통 부분만 부모 인터페이스로 뺀다. 기존 구현(`KedDialogueBoxView`)은 한 글자도 바뀌지 않는다.

```csharp
// 줄을 올리는 표면의 공통 — 전환(보이기 · 감추기 · 페이드)과 타자기 글 칸.
public interface IPresentationLineSurfaceView
{
    TMP_Text GetLineText();
    void SetVisibleImmediate(bool visible);
    void PrepareHidden();
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}

// 지금 것 그대로 — 선언만 부모를 잇는다.
public interface IPresentationDialogueBoxView : IPresentationLineSurfaceView
{
    TMP_Text GetNameText();
    void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry);
    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);
}

public enum MessengerSide { Left = 0, Right = 1 }

public interface IPresentationMessengerView : IPresentationLineSurfaceView
{
    // 지금 줄. ••• 말풍선이 있으면 그 내용을 교체, 없으면 새로 만든다. 글자는 감춘 채 둔다(타자기가 연다).
    // 앞 말풍선의 글자는 여기서 다 보이게 굳힌다 — 타자기가 hard-cancel되면 글자를 열다 만 채 떠난다.
    void PrimeBubble(string text, string speakerName, MessengerSide side);

    // 시크 통과 줄. 다 보인 채로 더한다. 애니메이션 · ••• 없음.
    void AppendSettledBubble(string text, string speakerName, MessengerSide side);

    // 열을 비운다(••• 포함).
    void ClearBubbles();
}
```

### 4.2 `DialogueBoxPresentationController` — 뷰 둘, 전환은 나가는 뷰와 들어오는 뷰

- 생성자에 `IPresentationMessengerView messenger`(없어도 됨)를 더한다. 없는데 `#messenger` 줄이 오면 경고 한 번 + 기본 대사창으로 보여 준다.
- `DialogueBoxCurrentState.Box`의 형을 `IPresentationLineSurfaceView`로 넓힌다 — "지금 보이는 뷰"가 **나가는 뷰**다.
- `ViewFor(kind)` — `Messenger`면 메신저 뷰, 나머지는 대사창.
- `ApplyContent` — 들어오는 뷰가 메신저면 `PrimeBubble`, 아니면 지금 그대로(레이아웃 → `PrimeText`) + **메신저 열 비우기**(§2 불변식).
- `LineTextTarget` — 마지막 `ShowLineAsync`가 고른 뷰의 `GetLineText()`.

전환표 (나가는 뷰 `out` = `_boxState.Box`, 들어오는 뷰 `in` = `ViewFor(next)`):

| 전환 | 지금 | 뷰가 둘일 때 |
|---|---|---|
| `Keep` | 내용 교체 | 종류가 같으니 `out == in` — 그대로. 메신저 → 메신저면 `•••`가 다음 말풍선이 된다 |
| `Cut` | 내용 교체 + 보이기 | `out != in`이면 `out`을 즉시 감춘 뒤 같다 |
| `FadeIn` | 내용 → 감춤 → 페이드 인 | 그대로(`out` 없음). 되감기 도착 줄이 이 길 — 쌓인 열과 함께 패널이 뜬다 |
| `FadeOutIn` | `box` 아웃 → 내용 → `box` 인 | `out` 아웃 → `out` 감춤 → 내용(여기서 열이 비워진다) → `in` 인 |
| `Hide` | 아웃 → 내용 | `out` 아웃 → `out` 감춤 → `in`에 내용(감춘 채) |

- `CloseAll()` — 두 뷰를 감추고, 메신저 열을 비우고, 상태를 리셋한다.
- `CleanupStale()` — 커밋된 뷰만 커밋된 대로 보이고 다른 뷰는 감춘다. 중단된 줄이 남긴 말풍선은 지우지 않는다 — 줄 중단은 `ScenePresentation.StopAsync`에서만 오고 그 뒤는 반드시 `PrepareReplay`/`BeginScene`의 `CloseAll`이다.

### 4.3 쪽 정하기 — 대본이 정하는 지속 상태

`surface_layout`과 같은 길이다: 커맨드가 `DialogueSurfaceState`에 값을 적고, `ShowLineAsync`(와 시크 통과)가 읽는다. `ScenePresentation`의 `BeginScene` · `PrepareReplay`가 이미 `_dialogueSurfaceState.Reset()`을 부르므로 리셋은 공짜다. 되감기 때는 Yarn이 커맨드를 다시 돌리므로 값도 다시 선다.

- 비교는 **대본의 화자 이름**(`ctx.CharacterName`)으로 한다 — 화자 정책의 `fallbackDisplayName`으로 바꾸기 전 값. 말풍선에 넘기는 이름은 표시 이름.
- 화자가 없는 줄(나레이션)이 `#messenger`로 오면: 첫 판은 오른쪽이 아니라 **왼쪽**(= "내가 아닌 것")에 둔다. 가운데 시스템 글은 §8 나중.
- 어느 쪽을 지정하는가는 §7 결정 1.

### 4.4 `•••` — 예제 UI의 일

다음 표시는 지금도 본체 계약이 아니라 예제 UI의 일이다(`KedUIFlow.Play.HandleLineDisplayed` → `PlayScreen.SetNextMarkerVisible`). 메신저도 같은 자리에 건다.

- `HandleLineDisplayed`가 `KedMessengerView.ShowWaitingBubble()`도 부른다. 뷰는 **지금 줄의 말풍선을 들고 있을 때만** 붙인다(`PrimeBubble`이 세우고 `ClearBubbles`가 내리는 플래그) — 대사창 줄에서는 아무 일도 없다.
- 다음 줄이 메신저면 `PrimeBubble`이 그 말풍선을 교체한다. 아니면 전환이 열을 비울 때 함께 사라진다.
- 본체 계약에 `•••`는 없다.

## 5. 되감기 · 백로그 · 저장/로드 — 한 길로 모인다

| 기능 | 실제로 도는 길 |
|---|---|
| 한 걸음 되감기 | `RequestRollbackTo` → 정지 → `PrepareReplay`(`CloseAll`) → 노드 **루트부터** 다시, 표적 줄까지 시크 통과 |
| 백로그 점프 | 항목 → 롤백 포인트 → 위와 **같은** `RequestRollbackTo` |
| 로드 · 이어하기 | `PlaybackMemory.Restore` → 단위를 열 때 `BeginScene`(`CloseAll`) → 루트부터 `SeekKind.Load` 시크 |
| 저장 | 줄 좌표(`LineTarget`) + 백로그 문자열 + 진행. **화면 상태는 저장하지 않는다** |
| 백로그 기록 | `YarnLineBoundary.CommitLineEntered` — 표면을 정하기 **전**, 줄마다 |

고칠 곳은 하나 — `LinePresentationFlow.RunSeekPassThrough`. 지금은 줄마다 `CloseAll()`만 불러서 표적 줄 앞의 말풍선이 비게 된다.

```csharp
// LinePresentationFlow
_boxPresentation.PassLine(ctx.Line);      // CloseAll() 자리

// DialogueBoxPresentationController
public void PassLine(LocalizedLine line)
{
    // ShowLineAsync와 같은 ResolveNextBoxKind — 태그 → 화자 정책 → 기본.
    if (kind == Messenger && _messenger != null)
    {
        HideViews(); _boxState.Reset();              // 화면은 지금처럼 닫힌 채
        _messenger.AppendSettledBubble(글, 표시 이름, 쪽);
    }
    else
        CloseAll();                                   // 지금과 같다 — 열도 비운다
}
```

- 표적 줄에서는 평소처럼 `ShowLineAsync` → 상태가 리셋돼 있으니 `FadeIn` — 패널이 쌓인 열과 함께 뜨고 그 줄의 말풍선이 더해진다.
- 메신저 **앞** 줄로 되감으면: 통과 줄에 메신저가 없으니 열은 비어 있다.
- 메신저 **뒤** 줄로 되감으면: 메신저 통과 줄이 열을 쌓지만 감춰져 있고, 그 뒤의 비메신저 줄(통과든 표적이든)이 비운다.

나머지는 이미 맞다.
- 백로그: 메신저 줄도 다른 줄과 같이 적힌다(`rawText`).
- 롤백 도착 줄은 `#auto`를 무시한다(`LineAdvancePolicy`).
- 빨리 넘김 · 배속: `ShouldUseImmediateTransition` → `Cut` — 말풍선이 바로 선다.

## 6. 단계

각 단계는 커밋하고 확인받은 뒤 다음으로 간다.

| 단계 | 무엇 | 확인 |
|---|---|---|
| 1 | `DialogueBoxKind.Messenger` · 뷰 계약 분리(§4.1) · 컨트롤러의 뷰 고르기와 두 뷰 사이 전환(§4.2) | 메신저 뷰 없이 기존 대본 · 되감기 · 빨리 넘김이 같다. EditMode 테스트 전부 |
| 2 | 쪽 상태와 커맨드(§4.3) · `PassLine`(§5) | 새 EditMode 테스트(가짜 뷰 둘): ① 일반 재생과 되감기 도착의 말풍선 열이 같다 ② 메신저 앞으로 되감으면 비어 있다 ③ 메신저 → 대사창 `FadeOutIn`에서 열이 페이드 아웃 **뒤에** 비워진다 ④ 메신저 뷰가 없으면 대사창으로 떨어진다 |
| 3 | 예제 UI `KedMessengerView` · 프리팹 — 말풍선 · 좌우 · 아바타 묶음 · `•••` · 넘치면 위로 밀기 | 아미야 Unity |
| 4 | 데모 메신저 대본 · README 어휘 표 · `SCOPE-BOUNDARY.md` §4 | §6.1 |

컨트롤러 테스트는 지금 없다(`Ked.Presentation/Tests/EditMode`에 대사창 테스트 0개). 컨트롤러는 순수 C#이고 뷰가 인터페이스라 가짜 뷰로 바로 쓸 수 있다 — 화자 정책 DB만 `ScriptableObject.CreateInstance`로 만든다.

### 6.1 Unity 확인 목록 (아미야)

1. 컴파일 오류 0, 새 `.meta` 함께 커밋.
2. 데모 대본 일반 재생: 좌우, 아바타 묶음, `•••` → 다음 줄에서 교체, 메신저가 끝나면 대사창으로.
3. 메신저 **도중** 줄에서 되감기 · 백로그 점프 → 앞 말풍선이 트윈 없이 서 있고 일반 재생과 같다.
4. 메신저 **앞** 줄로 되감기 → 메신저가 없다.
5. 메신저 도중 저장 → 타이틀 → 이어하기 → 3과 같다.
6. 빨리 넘김 · 배속으로 메신저를 지난다.

## 7. 정할 것

| # | 물음 | 제안 |
|---|---|---|
| 1 | 대본이 지정하는 쪽은 **상대**(`messenger_contact 닉스` — 상대만 왼쪽)인가 **나**(`messenger_me 코하쿠` — 나만 오른쪽)인가 | **나**. 단체방에서는 왼쪽이 여럿이고, 첨부 화면의 헤더는 사람이 아니라 방 이름이다. "나" 하나만 정하면 1:1과 단체방이 같은 규칙이다. 헤더는 나중에 따로 커맨드(이름 + 상태 글)를 받는다 |
| 2 | 메신저 구간 사이에 나레이션(대사창 줄)이 끼면 열을 비우는가 | **비운다**(§2). "대사창 종류 하나"라는 결정과 맞고 되감기 규칙이 한 줄로 남는다. 원본이 패널을 띄운 채 아래 대사창으로 나레이션을 낸다면 그때는 메신저를 종류가 아니라 지속 표면으로 다시 봐야 한다 — GIF에 그런 줄이 있는지 알려 주시면 된다 |
| 3 | 말풍선 글자를 타자기로 여는가, 한 번에 띄우는가 | 첫 판은 **타자기 그대로**(흐름 수정 0). 한 번에 띄우려면 메신저 줄에서 타자기를 즉시 완료시키는 분기가 `LinePresentationFlow`에 하나 생긴다 — Unity에서 보고 정한다 |

## 8. 나중

- 헤더(방 · 상대 이름, 상태 글).
- `•••`를 다음 화자 쪽에.
- 화자 없는 줄의 가운데 시스템 글.
- 아바타 그림의 출처(화자 → 그림). 단계 3에서 예제 UI 안의 표로 시작한다.
- 그림 · 이모티콘 말풍선(수집기의 `chat` · `emojis/phone`).
