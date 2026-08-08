# 리덕션 경계 — "스펙 → 목표 상태" 규약

코드베이스 **57곳**에 흩어져 있던 `ClaimTarget` 관습을 규약으로 승격한 것.
이후 커맨드 이관이 전부 이 문서의 모양을 따른다.

본보기: [`Reduce/CharPlacementReductions.cs`](../Reduce/CharPlacementReductions.cs) (MoveBy · ScaleTo)
↔ 호스트 `MoveByCommandCharR` · `ScaleToCommandCharR`.

## 코어로 가는 것 / 호스트에 남는 것

| | 어디 | 왜 |
|---|---|---|
| 스펙 → 목표 상태 변환 (`XxxReduction.Reduce`) | **코어** | 정지 프레임이 필요한 전부. 순수 산수다 |
| 출력 타입 (`StageNodeClaim`) | **코어** | 게시·트윈 종점·폴드가 같은 값을 봐야 한다 |
| `CommandBase` · 코루틴 · `WaitForCompletion` | 호스트 | MonoBehaviour 호스트를 문다 |
| `ResolveRefs` (slotKey → RectTransform) | 호스트 | 유니티 참조 해석이다 |
| `DOKill` · 트윈 생성/가속 (`OnStepLifetimeFinished`) | 호스트 | 시간의 세계다 |
| Commit의 트랜스폼 쓰기 · Ledger `Clear` | 호스트 | 유니티에 쓰는 일이다 |
| `HasClaimedTarget` 수명 관리 | 호스트 | 실행 수명의 일부다 |

## 리덕션의 형태

```csharp
public static class XxxReduction            // 커맨드당 하나, Reduce/ 폴더
{
    public readonly struct Args { ... }     // 파싱된 스펙 값만. UnityEngine 타입 금지
    public static StageNodeClaim Reduce(    // 순수 함수
        string nodeKey,                     // 논리 노드 키
        in Args args,
        /* 필요한 현재 상태를 명시적 인자로 */ ...);
}
```

- **순수**: 시간·랜덤·IO·전역 상태 없음. 트랜스폼을 **읽지도** 않는다 —
  현재 상태가 필요하면 값으로 받는다. 같은 입력은 언제나 같은 출력.
- **출력은 `StageNodeClaim`.** 복수 노드를 겨누면 배열이다(SetDepth처럼 위치+스케일 두 장).
  스케일 클레임의 z 보존 규약은 `ApplyTo`가 안다.

## 호스트 어댑터의 형태 (본보기에서)

```csharp
private void ClaimTarget()
{
    _rect.DOKill(true);                                    // 호스트: 시간의 세계
    _startPos = _rect.anchoredPosition;                    // 호스트: 현재 상태 읽기

    StageNodeClaim claim = MoveByReduction.Reduce(         // ── 코어 ──
        _rect.name,
        new MoveByReduction.Args(_spec.useAbsolutePosition, _spec.delta.ToCore()),
        _startPos.ToCore());

    _destPos = claim.Value.XY.ToUnity();                   // 호스트: 트윈 종점
    _rigRefs.PlacementTargets.PublishAnchoredPosition(_rect, _destPos); // 호스트: 게시

    HasClaimedTarget = true;
}
```

**동작 불변이 규약이다**: 어댑터는 계산의 **자리만** 바꾸고 값·순서·부작용을 바꾸지 않는다.
그래서 리덕션 테스트의 기대값은 **종전 식**에서 온다 — 새 기대값을 만들면 불변을 주장할 수 없다.

호스트에서 노드의 정체는 `RectTransform` 참조이고, 클레임의 `NodeKey`는 진단용 이름이다.
장부(`CharacterPlacementTargetLedger`)가 참조 ↔ 키 대응을 안다.

유니티 ↔ 코어 벡터 변환은 `PresentationCoreConversions`의 `ToCore()` / `ToUnity()` 한 곳에 둔다.

## 클레임이 흐르는 세 갈래

```
              StageNodeClaim
                    │
   ┌────────────────┼────────────────┐
   ↓                ↓                ↓
① 장부 게시      ② 트윈 종점      ③ 상태 폴드
Ledger.Publish   DOTween 종점     claim.ApplyTo(tree)
(정착 예약)      (재생)           (정지 프레임)
```

**셋이 같은 값을 보므로 "재생 결과 = 정착 예약 = 정지 프레임"이 한 곳에서 갈라진다.**
하나라도 빠뜨리면 나중에 원인 모를 불일치가 난다.
(`ReductionTests.세_갈래가_같은_값을_본다`가 이 성질을 고정한다.)

## 표본 정독 보고 — 편차 축

착수 전 표본 8곳 정독(placement / setup / acting / color / overlay). 결과:

### 축 ① — dest 출처

| 유형 | 예 | 이관 |
|---|---|---|
| 순수 (스펙 + 현재 값) | MoveBy · ScaleTo · OverlaySize · ColorTo | 그대로 가능 |
| 튜닝 값 필요 | SetDepth (depth 프리셋) | 파싱된 값을 `Args`에 담아 받는다 |
| **현재 정착 상태 의존** | SetDepth(focus 보존) · PlaceCharacterFocus | solver 순수화가 선행 |
| **절차적 (시간·랜덤)** | Tremble · Sway · Breath · Walk · Hop · Bounce | **불가** (아래) |

### 축 ② — 상태 종류

트랜스폼(anchoredPosition / localScale / localEuler) · alpha(Fade) · color(ColorTo) ·
sizeDelta(Overlay) · 마스크(StageMaskMotion).
`StageNodeClaim`은 트랜스폼 셋으로 시작하고, 필요한 종류를 그때 더한다.

### 축 ③ — 게시 여부

57곳 중 **4곳만** 장부에 게시한다: MoveBy · ScaleTo · PlaceCharacterFocus · SetDepth.
나머지는 트윈 종점으로만 쓴다. 이건 편차가 아니라 **의미 차이**다 —
"이 노드가 정착 후 어디 있을지 남이 알아야 하는가"의 문제다.

## 두 가지 주의 (표본 정독에서 드러난 것)

**1. 이관 단위는 `ClaimTarget` 메서드가 아니라 "dest를 계산하는 자리"다.**
`ScaleToCommandCharR.ClaimTarget`은 `DOKill`만 하고, 계산은 `CaptureTweenEndpoints` →
`ResolveTargetScale`에, 게시는 `PublishSettledTarget`에 따로 있다.
메서드 이름으로 찾으면 놓친다.

**2. 절차적 커맨드는 이관 대상이 아니다.**
`Tremble`은 `_basePos`와 `Random.Range` 시드만 잡고 **목표값 자체가 없다** —
시간 함수로 계속 흔들 뿐이다. 정지 프레임에서 이런 커맨드는 목표가 정의되지 않으므로,
리듀서에서 `Unhandled`로 남는 것이 맞다. 이관 개수 추정에서 뺀다.

## 사장 코드

`PlaceToCommandCharR`은 게시·클리어가 통째로 주석 처리돼 있다. 이관 대상이 아니다.
