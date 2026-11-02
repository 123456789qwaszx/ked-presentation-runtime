public enum VNLinePresentationPhase
{
    None = 0,

    // Yarn has delivered a line, but no VN-side state has been committed yet.
    LineReceived = 10,

    // The current line's entry seek kind, record decision, and seek decision have been resolved
    // before any line state is committed.
    // YarnLineMeta has been built for seek matching, and resolving the seek decision advances
    // the seek target match count. No current-line state, backlog, or rollback point has been written yet.
    LineRuntimeStateResolved = 20,

    // The line has been committed as the current line.
    // Backlog and rollback points have been recorded according to the resolved record decision,
    // and collected commands have been played.
    // Seek pass-through lines may be consumed for alignment and leave the visual flow here.
    LineEnteredCommitted = 30,
    
    // A new LinePresentationRun has started.
    // The previous visual run has been cancelled by the presenter owner.
    VisualRunStarted = 40,

    // The line is being consumed silently as part of an active seek.
    // It should complete processing without showing the dialogue box or typewriter.
    SeekPassThrough = 41,
    
    // The dialogue presentation route for this line has been resolved.
    // A visible dialogue surface may have been prepared, or dialogue presentation may have been suppressed.
    DialogueSurfaceResolved = 60,

    // The typewriter has been bound to the resolved text view and prepared with line content.
    // No visible character reveal has started yet.
    TypewriterReady = 70,
    
    // The typewriter reveal has finished or was cancelled/suppressed through the current line token.
    // The line is not yet committed as fully displayed until the run is validated afterward.
    TypewriterCompleted = 71,

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