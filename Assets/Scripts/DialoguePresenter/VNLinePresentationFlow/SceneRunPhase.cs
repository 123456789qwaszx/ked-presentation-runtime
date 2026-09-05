public enum SceneRunPhase
{
    None = 0,

    // 장면 루트와 presentation checkpoint를 준비하는 중.
    SceneEntering = 10,

    // EpisodePlayer의 장면 진입 처리가 끝났다.
    SceneEntered = 20,

    // 장면 진입 상태가 저장 계층에 보고됐다.
    EntryReported = 30,

    // 이어하기 LoadPlan을 적용했거나, 적용할 계획이 없음을 확인했다.
    LoadPlanApplied = 40,

    // 현재 Episode의 Yarn 노드를 실행 중이다.
    EpisodePlaying = 50,

    // 현재 Episode가 정상적으로 끝났고 시청/로드 진행 상태까지 반영됐다.
    EpisodeCompleted = 60,

    // recorded / auto / user 중 다음 진행 선택을 결정하는 중이다.
    ChoiceResolving = 70,

    // 다음 진행 선택이 확정됐다.
    ChoiceResolved = 80,

    // 선택에 딸린 Via 노드를 실행 중이다.
    ViaPlaying = 90,

    // 선택의 TargetEpisodeId로 현재 위치를 이동했다.
    TargetMoved = 100,

    // rollback 요청을 받아 장면 루트 재실행을 준비하는 중이다.
    Replaying = 200,

    // pending을 확정 상태로 접고 저장 보고를 만드는 중이다.
    SceneCommitting = 900,

    // 장면 또는 챕터가 정상적으로 확정됐다.
    Completed = 910,

    // 외부 CancellationToken에 의해 실행이 중단됐다.
    Cancelled = 920,

    // 예상하지 못한 예외로 장면 실행이 종료됐다.
    Faulted = 930,
}