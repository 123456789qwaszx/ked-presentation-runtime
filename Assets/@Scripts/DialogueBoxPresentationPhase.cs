public enum DialogueBoxPresentationPhase
{
    None = 0,

    // A line has been received by the dialogue box presentation controller.
    // No box selection, text priming, or visual state change has been committed yet.
    LineReceived = 10,

    // The next dialogue box, previous box, and transition kind have been resolved.
    // This is only a transition plan; no visual state has been committed yet.
    PlanBuilt = 30,

    // The target dialogue box has been filled with the line text and speaker name.
    // The text is prepared for display, but the box transition has not been applied yet.
    TextPrimed = 40,

    // The initial visual state for the selected transition has been prepared.
    // For example, the next box may be hidden, shown, or isolated from other boxes.
    TransitionPrepared = 50,

    // The transition is currently being applied.
    // This may be immediate, fade-in, fade-out-in, cut, keep, or hide.
    TransitionApplying = 60,

    // The selected box has been committed as the current dialogue box state.
    // After this point, the controller's shared box state points to the new box.
    Committed = 70,

    // The dialogue box presentation completed normally.
    Completed = 900,

    // The current LinePresentationRun became stale before the normal commit/completion path.
    // Shared dialogue box state must not be committed after entering this phase.
    Stale = 901,
}