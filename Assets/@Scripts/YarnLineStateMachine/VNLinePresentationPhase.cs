public enum VNLinePresentationPhase
{
    None = 0,

    // Yarn has delivered a line, but no VN-side state has been committed yet.
    LineReceived = 10,

    // The line has been converted into YarnLineMeta and committed as the current line.
    // Backlog and rollback points may also have been recorded, depending on the current state.
    LineEnteredCommitted = 20,

    // Seek state has been evaluated for this line.
    // The line is now classified as normal, pass-through, or pending seek target.
    SeekResolved = 30,
    
    // The line is being skipped visually as part of an active seek.
    // It should complete processing without showing the dialogue box or typewriter.
    SeekPassThrough = 31,
    
    // The pending seek target line has been accepted and consumed.
    // From this point, normal visual presentation resumes for the target line.
    SeekTargetConsumed = 32,

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

    // The line has been committed as processing-complete.
    // This applies both to normal display completion and seek pass-through completion.
    DisplayCommitted = 80,

    // The presenter is waiting for Yarn's NextContentToken before ending this line transaction.
    WaitingForAdvance = 90,

    // The line transaction completed normally.
    Completed = 900,

    // The current LinePresentationRun became stale before the normal final commit.
    // Shared visual/domain state must not be committed after entering this phase.
    Stale = 901,

}