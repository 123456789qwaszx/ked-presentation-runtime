public enum VNLinePresentationPhase
{
    None = 0,

    // Yarn has delivered a line, but no VN-side state has been committed yet.
    LineReceived = 10,

    // The line has been converted into YarnLineMeta and committed as the current line.
    // Backlog and rollback points may also have been recorded, depending on the current state.
    LineEnteredCommitted = 20,
    
    // The current line's runtime state has been resolved before any visual work begins.
    // This includes line meta propagation, runtime state update, backlog/rollback handling,
    // collected command registration, and seek decision.
    // Seek pass-through lines may be consumed for alignment and leave the visual flow here.
    LineRuntimeStateResolved = 30,
    
    // The line is being consumed silently as part of an active seek.
    // It should complete processing without showing the dialogue box or typewriter.
    SeekPassThrough = 31,
    
    // The pending seek target line has been reached, and the target resume policy has been resolved.
    // This phase decides whether the target line resumes with immediate rules or normal visual flow.
    // It does not mean that the visual presentation itself has started.
    ResumePolicyResolved = 32,

    // A new LinePresentationRun has started.
    // The previous visual run has been cancelled by the presenter owner.
    VisualRunStarted = 40,

    // The dialogue box is transitioning into the required state.
    // This may include fade-in, fade-out-in, cut, or immediate transition.
    BoxTransitioning = 50,

    // The dialogue box is ready and the target TMP_Text has been resolved.
    BoxReady = 60,

    // The typewriter is actively revealing the line text.
    TypewriterRunning = 70,

    // The line's display processing has been committed as complete.
    // After this point, the line is considered fully shown/processed by the VN presentation layer.
    DisplayCommitted = 80,

    // The presenter is waiting for the external advance gate before ending this line transaction.
    // At this point, the line has already been visually processed.
    WaitingForAdvance = 90,

    // The line transaction completed normally.
    Completed = 900,

    // The current LinePresentationRun became stale before the normal final commit.
    // Shared visual/domain state must not be committed after entering this phase.
    Stale = 901,
}