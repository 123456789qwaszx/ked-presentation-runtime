public enum DialogueBoxPresentationPhase
{
    None = 0,

    LineReceived = 10,
    
    PlanBuilt = 30,
    
    TextPrimed = 40,
    
    TransitionPrepared = 50,
    
    TransitionApplying = 60,

    Committed = 70,

    Completed = 900,
    Stale = 901,
}