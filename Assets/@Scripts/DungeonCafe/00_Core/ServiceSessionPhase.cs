/// <summary>
/// 접객 1회(입실 ~ 결산)의 진행 단계.
/// VNLinePresentationPhase 와 같은 목적으로, 어느 지점에서 중단/무효화되었는지 로그로 남기기 위해 존재한다.
/// </summary>
public enum ServiceSessionPhase
{
    None = 0,

    // 예약이 확정되고 담당 메이드가 배정되었다. 아직 입실 전이다.
    AssignmentCommitted = 10,

    // 입실 브리핑(위임 프로토콜 고지)이 재생되었다.
    BriefingPlayed = 20,

    // 격리실이 봉쇄되고 세션 상태가 생성되었다.
    RoomSealed = 30,

    // 현재 비트의 상황 노드가 재생되었다.
    BeatSituationPlayed = 40,

    // 메이드의 대응력과 성향으로 제안 후보가 추려져 승인 대기 중이다.
    OptionsOffered = 50,

    // 관리자가 행동 하나를 승인했다.
    OptionApproved = 60,

    // 승인된 행동이 재생되고 부담/반응이 세션에 반영되었다.
    OptionResolved = 70,

    // 붕괴 한계를 넘어 관리자 통제 신호가 거부되었다.
    ControlLost = 80,

    // 통제 상실 이후의 자동 사건이 진행 중이다. 플레이어 입력은 무시된다.
    AutonomousRunning = 81,

    // 시나리오 템플릿이 끝까지 소화되었다.
    ScenarioCompleted = 90,

    // 반응 점수 x 붕괴 배율 결산이 커밋되었다.
    Settled = 100,

    // 세션 트랜잭션이 정상 종료되었다.
    Completed = 900,

    // 세션이 유효하지 않게 되었다. 이 단계 이후 공유 상태를 커밋하면 안 된다.
    Aborted = 901,
}
