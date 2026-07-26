# Guesthouse Vertical Slice — 시스템 데모

기존 VN 프레젠테이션 레이어(`VNLinePresentationFlow`, `DialogueBoxPresentationController`, `UIManager`, `VnScreenBindings`) **위에 얹는** 게임 시스템 레이어입니다.
연출/대사 재생은 기존 레이어가 그대로 담당하고, 이 코드는 **노드 경계**와 **수치 규칙**만 다룹니다.

---

## 1. 폴더 구조

| 폴더 | 역할 | Unity 의존 |
|---|---|---|
| `00_Core` | 축/종족/반응/단계 enum, `AxisTriple` | 직렬화 속성만 |
| `10_Definition` | 불변 런타임 레코드 (프로필, 시나리오 그래프, 종족 규약, 튜닝) | 없음 |
| `20_Data` | ScriptableObject 저작 + `GuesthouseContentDB` 조회 | 있음 |
| `30_State` | 캠페인/하루/세션/메이드 가변 상태 | 없음 |
| `40_Rules` | 순수 계산 (부담 누적, 통제 판정, 결산, 밤 전환, 엔딩) | 없음 |
| `50_Flow` | 진행 흐름 + 표현 계층 포트 인터페이스 | `YarnTask`만 |
| `60_Integration` | Yarn/UI 어댑터, 합성 루트, 내장 콘텐츠, 헤드리스 구동 | 있음 |
| `70_UI` | 패널 4종 + `VnScreenBindings` 파셜 | 있음 |

`40_Rules` 이하는 `UnityEngine`에 의존하지 않으므로 EditMode 테스트에서 그대로 호출할 수 있습니다.

---

## 2. 기획 → 코드 대응

| 기획 항목 | 구현 |
|---|---|
| 육체/정신/감응 3축 | `BurdenAxis`, `AxisTriple` |
| 대응력 3종 / 누적 부담 3종(상처·스트레스·충동) | `MaidProfile.Aptitude` / `MaidBurdenState` |
| 누적 부담 = 붕괴도 | 두 개념을 한 축으로 통합 (아래 §5 참고) |
| 업무 숙련 3트랙, 자동 레벨업 없음 | `MaidMasteryTrack.IsEventReady` → 밤에 `CommitLevelUp` |
| 메이드가 능력치대로 3개 제안 | `ServiceOptionSelector` (무작위 없음) |
| 검을 꺼내면 또 선택지 | `ServiceActionOption.NextBeatKey` → `ServiceBeat` 분기 |
| 반응 3단계 (0 / 1 / 3점) | `MonsterReactionGrade` (enum 값 = 점수) |
| 결산: 반응 합 × 붕괴 배율 | `ServiceSettlementCalculator` + `CollapseMultiplierTable` |
| 배율 0~24 ×1.0 / 25~49 ×1.5 / 50~74 ×2.0 / 75+ ×3.0 | `ProgressionTuning.CreateDefault()` |
| 한계 초과 = 통제권 상실 | `ControlAuthorityRule` → `ServiceSessionFlow_Autonomous` |
| 배드엔딩은 종족 단위 | `SpeciesProtocol` |
| 밤: 회복 / 관리 붕괴 | `NightConversionRule.RunCare` / `RunManagedRelease` |
| 관리 붕괴 92 → 46 | 한계까지 끌어올린 뒤 진입 시점 수치의 50%로 회수 |
| 3일 × 3접객 → 엔딩 | `CampaignFlow` + `EndingResolver` |

---

## 3. 구동 흐름

```
CampaignFlow.RunAsync
└ DayCycleFlow.RunDayAsync                       (×3일)
  ├ 게시판 → 예약 확정 통화 (종족만 공개 → 대응 타입 공개)
  ├ 슬롯 루프 (×3)
  │ ├ 메이드 배정
  │ ├ ServiceSessionFlow.RunAsync
  │ │ ├ 브리핑(위임 프로토콜 고지)
  │ │ └ 비트 루프
  │ │   ├ 상황 노드 재생
  │ │   ├ ServiceOptionSelector.Select     ← 능력치/성향
  │ │   ├ 승인 대기                         ← 플레이어
  │ │   ├ 승인 노드 재생 + 부담/반응 반영
  │ │   └ ControlAuthorityRule 재판정
  │ │       └ Lost → RunAutonomousCollapseAsync (플레이어 개입 불가)
  │ └ ServiceSettlementCalculator.Settle
  ├ 하루 리포트
  └ NightPhaseFlow.RunNightAsync
    ├ 회복 또는 관리 붕괴 1건
    ├ 준비된 숙련 이벤트 소화 (레벨업 확정)
    └ 메이드 간 대화
```

---

## 4. 씬 배치

1. 빈 GameObject에 `GuesthouseRuntime` 부착
2. `Content Bundle` 슬롯에 `GuesthouseContentBundleSO` 연결 — **비워두면 `GuesthouseDemoContent`(코드 내장)로 자동 폴백**
3. `Dialogue Runner` 슬롯에 기존 메인 러너 연결
4. `VnScreenBindings` 생성 직후 한 번 주입:

```csharp
_guesthouseRuntime.ConfigureScreens(_vnScreenBindings);
```

5. 패널 프리팹 4종을 `UIManager` 패널 레이어 하위에 배치
   (`MaidActionApprovalPanel`, `MaidAssignmentPanel`, `ServiceSettlementPanel`, `NightProgramPanel`)
   — 각 `Refs` enum 이름과 자식 GameObject 이름을 맞추면 됩니다. 옵션 리스트는 기존 `ChoiceBoxView` 프리팹을 그대로 재사용합니다.

**UI 없이 검증**하려면 `ConfigureScreens`를 호출하지 않으면 됩니다.
`HeadlessGuesthouseScreens`가 자동 응답으로 3일치 루프를 끝까지 굴리고 로그를 남깁니다. 무작위성이 없으므로 밸런스 회귀 테스트에 그대로 쓸 수 있습니다.

---

## 5. 설계 판단 (기획서에서 명시되지 않아 확정한 부분)

- **누적 부담과 붕괴도를 하나로 통합**했습니다. 기획서에 상처/스트레스/충동과 "관련 붕괴도"가 따로 등장하는데, 결산 배율이 "몬스터가 원하는 유형의 붕괴도"를 참조하므로 두 개를 별도 수치로 두면 항상 같이 움직이는 중복 상태가 됩니다. 축당 0~100 한 벌로 통일했고, 표기 이름만 상처/스트레스/충동으로 분리했습니다. 분리가 필요해지면 `MaidBurdenState`에 두 번째 배열만 추가하면 됩니다.
- **경험치는 완화 전 원본 부하 기준**입니다. "몬스터가 가하는 부담만큼" 얻는다는 서술을 따랐습니다. 대응력이 높은 메이드는 안전하지만 성장이 느립니다.
- **완화는 부하를 0으로 만들지 않습니다** (`MinimumAppliedLoad`). 이상적인 플레이일수록 한계에 가까워진다는 낮 설계의 전제를 지키기 위한 안전장치입니다.
- **제안 후보 선정에 난수를 쓰지 않습니다.** 롤백/세이브 복원 시 제안이 바뀌면 안 되기 때문입니다. 변주가 필요하면 `ServiceOptionSelector`에 세션 토큰 기반 시드를 주입하는 형태로 확장하세요.
- **통제 상실 이후 자동 사건은 개체 시나리오를 더 이상 참조하지 않습니다.** 종족 규약(`SpeciesProtocol`)만 봅니다. 종족 단위로 결말을 묶는다는 기획을 코드 구조로 강제한 것입니다.
- **`AllowsWithdrawAfterControlLoss = false`인 종족에서 한계를 넘기면 그 메이드는 캠페인에서 이탈**합니다(`MaidRuntimeState.IsLost`). 이후 배정 후보에서 제외되고 엔딩 판정 1순위가 됩니다.

---

## 6. 확장 지점

| 하고 싶은 것 | 손댈 곳 |
|---|---|
| 예약 구성 로직 교체 (평판/난이도 곡선) | `IBookingPlanner` 새 구현 |
| 엔딩 조건 추가 | `EndingResolver.Resolve`의 평가 순서 |
| 밤 처리 방식 추가 | `NightProgramKind` + `NightConversionRule` |
| 승인 UI 교체 | `IGuesthouseScreenBindings`만 구현 |
| 밸런스 조정 | `ProgressionTuningSO` (코드 수정 불필요) |
| 업무 수첩 화면 | `GuesthouseContentDB` + `MonsterProfile.CodexNotes` / `SpeciesProtocol.RiskNotes` 이미 준비됨 |
| 세이브/로드 | `CampaignState`가 루트. 참조가 아닌 ID(`MaidId`/`MonsterId`/`ScenarioKey`)로만 정의를 가리키므로 DTO 매핑이 단순합니다 |

---

## 7. 확인이 필요한 연결부

- `ScenarioNodeRunner`는 `DialogueRunner.StartDialogue` / `IsDialogueRunning` / `Stop()` / `Dialogue.NodeExists`만 사용합니다. Yarn Spinner 버전에 따라 완료 감지를 `onDialogueComplete` 기반으로 바꾸는 편이 나을 수 있습니다 — 그 경우 `PlayNodeAsync` 하나만 고치면 됩니다.
- 접객 세션이 기존 사이드 러너(`VNSideRunnerSyncHub`)와 동시에 도는 구성이라면, `ServiceSessionFlow.Invalidate()`를 라인 러너 정지 시점에 함께 호출해 주세요.
- 미작성 Yarn 노드는 경고 로그만 남기고 통과합니다. 노드 이름 규칙은 `GuesthouseNodeNaming`과 `GuesthouseDemoContent`에 모두 명시되어 있습니다.

---

## 8. 미구현 (의도적 범위 밖)

- 업무 수첩 화면 UI (데이터 접근은 준비됨)
- 세이브/로드 DTO
- 숙련 이벤트 / 야간 이벤트의 **본문 텍스트** — 전부 Yarn 노드 키로만 참조합니다. 시스템 코드에는 대사가 들어 있지 않으므로, 노드 작성은 기존 Yarn 파이프라인에서 그대로 진행하시면 됩니다.
