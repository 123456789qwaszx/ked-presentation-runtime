public enum ConflictForkPhase
{
    None = 0,

    // 충돌 대상과 fork에 필요한 공유 데이터가 준비됐다.
    // Save와 Queue의 영속 상태는 아직 변경되지 않았다.
    ContextPrepared = 10,

    // LocalSaveFile이 새 회차 기준으로 갱신됐다.
    // 변경 내용은 아직 디스크에 저장되지 않았다.
    SavePrepared = 20,

    // 새 회차 파일이 저장되고 active 회차가 새 fork로 전환됐다.
    // 기존 source 회차 파일은 그대로 유지된다.
    SavePersisted = 30,

    // source 회차 Queue에서 이번 fork가 가져갈 Pending이 제거됐다.
    // 제거된 Pending은 Context가 계속 보관한다.
    SourceQueueReleased = 40,

    // 공유 SyncQueue가 새 회차의 Queue 파일을 가리키도록 전환됐다.
    // 새 Queue에는 아직 Pending이 적재되지 않았다.
    ForkQueueSelected = 50,

    // 이전 회차의 Pending이 새 회차 Queue에 다시 적재됐다.
    // Choice seq는 새 회차 기준으로 1부터 다시 부여된다.
    PendingRequeued = 60,

    // Runtime 회차 상태가 새 fork에 맞게 정리됐다.
    // Startup Sync 중이라 Runtime이 없는 경우에는 의도적으로 변경하지 않는다.
    RuntimeStateResolved = 70,

    // Save, Queue, Runtime 전환이 끝난 상태가 외부에 공개됐다.
    // 이 시점부터 ConflictForked 수신자는 새 회차가 active라고 가정할 수 있다.
    ConflictPublished = 80,

    // 새 회차의 서버 동기화가 다시 요청됐다.
    // 동기화 완료 여부는 이 Conflict 처리 흐름의 책임에 포함되지 않는다.
    ResyncRequested = 90,

    // Conflict fork 처리가 정상적으로 완료됐다.
    Completed = 900,
}