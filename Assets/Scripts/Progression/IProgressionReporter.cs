// 진행 층이 바깥(저장)에 알리는 것. 단위는 장면 — 선택 하나가 아니다.
public interface IProgressionReporter
{
    // 장면이 끝나 fold됐다. 선택·시청·확정 상태·[3] 덤프가 한 번에 온다.
    void ReportSceneCommitted(SceneCommitReport report);
}
