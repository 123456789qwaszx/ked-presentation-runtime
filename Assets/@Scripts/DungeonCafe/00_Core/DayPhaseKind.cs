/// <summary>
/// 하루의 진행 단계. 세이브/복구 시 어디서부터 재개할지 판단하는 기준이 된다.
/// </summary>
public enum DayPhaseKind
{
    None = 0,

    // 인터넷 게시판에 오늘 방문 희망 몬스터가 올라온다. 종족과 겉모습만 확인된다.
    ReservationBoard = 10,

    // 전화로 예약을 확정한다. 이 시점에 대응 타입 정보가 수첩에 갱신된다.
    ReservationCall = 20,

    // 담당 메이드를 선택한다.
    MaidAssignment = 30,

    // 격리실 접객 진행.
    ServiceSession = 40,

    // 반응 점수 × 붕괴 배율 결산.
    ServiceSettlement = 50,

    // 오늘 3회 접객이 모두 끝났다.
    DayReport = 60,

    // 밤: 회복 또는 관리 붕괴, 숙련 이벤트, 메이드 간 대화.
    Night = 70,

    DayClosed = 900,
}

/// <summary>
/// 하루 단계의 표시 문구. 페이즈가 곧 라벨이므로 플로우가 문자열을 들고 있을 필요가 없다.
/// </summary>
public static class DayPhaseLabels
{
    public static string Of(DayPhaseKind phase)
    {
        return phase switch
        {
            DayPhaseKind.ReservationBoard => "예약 게시판",
            DayPhaseKind.ReservationCall => "예약 확정 통화",
            DayPhaseKind.MaidAssignment => "담당 배정",
            DayPhaseKind.ServiceSession => "접객 진행",
            DayPhaseKind.ServiceSettlement => "결산",
            DayPhaseKind.DayReport => "업무 종료",
            DayPhaseKind.Night => "밤 처리",
            DayPhaseKind.DayClosed => "영업 종료",
            _ => string.Empty,
        };
    }
}

/// <summary>
/// 접객 단계의 표시 문구.
///
/// 페이즈 이름은 '무엇이 끝났는가'로 되어 있고, 라벨은 '지금 무엇을 하는가'로 읽힌다.
/// 표시되지 않는 단계는 빈 문자열을 돌려주어 직전 라벨을 유지시킨다.
/// </summary>
public static class ServiceSessionPhaseLabels
{
    public static string Of(ServiceSessionPhase phase)
    {
        return phase switch
        {
            ServiceSessionPhase.AssignmentCommitted => "입실 준비",
            ServiceSessionPhase.RoomSealed => "격리실 봉인",
            ServiceSessionPhase.BeatSituationPlayed => "접객 진행",
            ServiceSessionPhase.ControlLost => "통제 상실",
            ServiceSessionPhase.AutonomousRunning => "자율 진행",
            ServiceSessionPhase.ScenarioCompleted => "접객 종료",
            ServiceSessionPhase.Settled => "결산",
            _ => string.Empty,
        };
    }
}
