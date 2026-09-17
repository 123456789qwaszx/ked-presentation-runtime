// 장면 기록을 받아 저장으로 확정하는 자리. 단위는 장면 — 선택 하나가 아니다.
//
// 진행 층이 이것을 직접 부르지 않는다. 진행 층은 Ked.Progression.IScenePersistence까지만 알고,
// ProgressionSaveBridge가 진행 결과에 Yarn 변수·Yarn 선택·백로그를 얹어 여기로 넘긴다.
public interface ISceneRecordReporter
{
    // 장면에 들어섰다. 진입 스냅샷 — 장면 기록의 앞부분.
    void ReportSceneEntered(SceneEntryReport report);

    // 장면이 끝나 fold됐다. 선택·시청·확정 상태·[3] 덤프·백로그가 한 번에 온다 — 장면 기록의 뒷부분.
    void ReportSceneCommitted(SceneCommitReport report);
}
