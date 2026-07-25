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
