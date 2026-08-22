public enum VNOptionsPresentationPhase
{
    None = 0,
    
    // Must happen before condition checks - even hidden groups must consume a sequence number
    // so rollback/replay assigns the same number at the same position.
    ChoiceSequenceReserved = 10,
    
    // During rollback/replay, resolve the choice from the recorded selection.
    // Do not show the options UI
    ReplayResolved = 20,

    // 표시할 옵션이 정해졌다(ViewModel). 아직 화면에는 없다.
    ViewModelsBuilt = 30,

    // 옵션 박스가 페이드인을 마쳤다. 아직 항목은 붙지 않았고 입력도 닫혀 있다.
    OptionsBoxShown = 40,

    // 항목이 생성·바인딩됐고 입력이 열렸다. 첫 항목이 선택돼 있다.
    InteractiveReady = 50,

    // 플레이어의 선택을 기다리는 중 — 이 층에서 유일하게 멈추는 지점이다.
    // 깨우는 것은 셋: 항목 제출 / 다음 라인 요청 / 세션 Dispose.
    AwaitingSelection = 60,

    // 선택이 기록에 커밋됐다. 이 시점부터 롤백 리플레이가 이 선택을 재현할 수 있다.
    SelectionCommitted = 70,

    // 고를 수 있는 옵션이 하나도 없어 끝났다 — 이것만이 NoOption이다.
    // 시크 중 기록을 복원하지 못한 경우는 여기가 아니라 Stale이다.
    NoOption = 901,

    // 선택 전에 중단됐다(다음 라인 요청 / 대화 정지).
    // 박스 페이드인 도중 끊긴 경우도 여기다 — 실패가 아니라 취소다.
    Aborted = 902,

    // 이 선택지 트랜잭션의 전제가 깨져 더 이어갈 수 없다 —
    // 시크 중 기록된 선택을 지금 선택지 목록에서 복원하지 못했거나,
    // 옵션 박스에 항목을 붙일 자리가 없다(배선 문제).
    // VNLinePresentationPhase.Stale과 같은 자리다: 공유 상태를 커밋하지 않고 빠져나간다.
    // 옵션 UI는 아직 뜨지 않았으므로 정리할 화면도, 기다릴 Advance도 없다.
    // 리플레이 복원 실패로 여기 온 경우, 시크를 끄고 일반 재생으로 복귀한 뒤 빠져나간다.
    Stale = 903,
}