// 진행 층이 바깥(저장)에 알리는 것. 단위는 장면 — 선택 하나가 아니다.
public interface IProgressionReporter
{
    // 장면에 들어섰다. 진입 스냅샷 — 장면 기록의 앞부분.
    void ReportSceneEntered(SceneEntryReport report);

    // 장면이 끝나 fold됐다. 선택·시청·확정 상태·[3] 덤프·백로그가 한 번에 온다 — 장면 기록의 뒷부분.
    void ReportSceneCommitted(SceneCommitReport report);
}
