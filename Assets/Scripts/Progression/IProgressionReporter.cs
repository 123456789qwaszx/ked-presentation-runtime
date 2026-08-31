// 드라이버가 진행 중 확정된 사실을 알리는 상대 (M7). 저장 층이 구현한다.
//
// 이벤트가 아니라 직접 호출인 이유: 루프 안에서 "무엇이 언제 기록되는가"가 코드 순서
// 그대로 읽혀야 한다. 반환값이 없고 드라이버는 결과를 기다리지 않는다.
public interface IProgressionReporter
{
    // EventKey가 달린 에피소드의 대사가 끝까지 재생됐다.
    void ReportEpisodeWatched(EpisodeWatchReport report);

    // 선택이 커밋됐다 — 스탯 반영과 이동이 끝난 뒤다.
    void ReportChoiceCommitted(ChoiceCommitReport report);
}
