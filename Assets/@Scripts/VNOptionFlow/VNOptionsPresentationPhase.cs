public enum VNOptionsPresentationPhase
{
    None = 0,

    // Yarn has delivered an option set, but no VN-side option state has been committed yet.
    OptionsReceived = 10,

    // The option set has been assigned its choice index within the current node,
    // and the choice cursor has advanced. This index is stable across replay.
    OptionSetCommitted = 20,

    // The selection policy has been resolved before any visual work begins.
    // This decides whether the option set is replayed from recorded history (during seek),
    // presented interactively, or short-circuited as a no-option result.
    SelectionPolicyResolved = 30,

    // The option set is being resolved silently from recorded choice history as part of an active seek.
    // It completes without showing any option UI.
    ReplayResolved = 31,

    // The interactive view models have been built from the available options.
    // Lines that produce no available option leave the flow here as a no-option result.
    ViewModelsBuilt = 40,

    // The option box is transitioning into the required state.
    // This may include fade-in, anchored placement near a character, or immediate transition.
    BoxTransitioning = 50,

    // The option box is ready and its item container has been resolved.
    BoxReady = 60,

    // The option items have been bound to the pool and revealed.
    ItemsPrepared = 70,

    // The presenter is waiting for the user to submit a selection.
    WaitingForSelection = 80,

    // A selection has been made and committed to choice history.
    SelectionCommitted = 90,

    // The option transaction completed normally.
    Completed = 900,

    // The option transaction was aborted before a normal selection commit.
    // This covers external cancellation (next-content requests), seek interruption, and box failure.
    // Shared option/domain state must not be committed after entering this phase.
    Aborted = 901,
}