public enum VNLinePresentationPhase
{
    None = 0,

    // Yarn이 라인을 넘긴 직후. 아직 VN 상태 변경 없음.
    LineReceived = 1,

    // YarnLineMeta 생성 및 CurrentLine 커밋 완료.
    // Backlog / RollbackPoint 기록 완료 (조건부).
    LineEnteredCommitted = 2,

    // Seek 상태 판정 완료.
    // pass-through / target-pending / normal 결정됨.
    SeekResolved = 3,

    // LinePresentationRun 생성 완료. 이전 visual run 취소됨.
    VisualRunStarted = 4,

    // Box 전환 중 (FadeIn / FadeOutIn / Cut 등).
    BoxTransitioning = 5,

    // Box가 준비됨. TMP_Text 확보됨.
    BoxReady = 6,

    // Typewriter 실행 중.
    TypewriterRunning = 7,

    // Typewriter 완료. DisplayCompleted 커밋됨.
    DisplayCommitted = 8,

    // Yarn NextContentToken 대기 중.
    WaitingForAdvance = 9,

    // 라인 트랜잭션 정상 종료.
    Completed = 10,

    // Run이 stale이 되어 정상 커밋 없이 종료.
    Stale = 11,

    // seek pass-through로 처리된 라인.
    SeekPassThrough = 12,
}