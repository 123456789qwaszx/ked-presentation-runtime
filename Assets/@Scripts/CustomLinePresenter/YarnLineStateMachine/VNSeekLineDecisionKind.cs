public enum VNSeekLineDecisionKind
{
    NotSeeking = 0,

    // Active seek 중 target이 아닌 라인.
    // DialogueBox/Typewriter/VisualRun을 생략하고 seek next를 dispatch한다.
    SkipVisualAndDispatchSeekNext = 10,

    // Seek target line에 도착했다.
    // Silent pass-through를 멈추고, 이 라인을 visual presentation 대상으로 진행한다.
    PrepareTargetForVisualResume = 20,
    
    // Rollback seek target line에 도착했다.
    // Silent pass-through를 멈추고, 이 라인을 visual presentation 대상으로 진행한다.
    // Rollback 복원 흐름이므로 transition/presentation은 즉시 적용한다.
    TargetLineVisualResumeImmediate = 21,

    // Load seek target line에 도착했다.
    // Silent pass-through를 멈추고, 이 라인을 visual presentation 대상으로 진행한다.
    // Load 완료 후 실제 감상 대상 라인이므로 transition/presentation은 정상 재생한다.
    TargetLineVisualResumeNormal = 22,
}