// Captures how a single option set should be resolved, decided once before any visual work begins.
// Mirrors the static-factory decision style used by VNSeekLineDecision so the flow branches on intent,
// not on raw flag combinations.
public sealed class VNOptionSelectionDecision
{
    public bool ShouldReturnNoOption { get; private set; }
    public bool ShouldReplayRecordedSelection { get; private set; }
    public bool ShouldPresentInteractive { get; private set; }

    // No option in the set is currently available; the set resolves to NoOptionSelected.
    public static VNOptionSelectionDecision NoOptionAvailable()
    {
        return new VNOptionSelectionDecision
        {
            ShouldReturnNoOption = true,
        };
    }

    // A seek is active; the selection is resolved from recorded choice history without showing UI.
    public static VNOptionSelectionDecision ReplayDuringSeek()
    {
        return new VNOptionSelectionDecision
        {
            ShouldReplayRecordedSelection = true,
        };
    }

    // Normal play; the option set is presented to the user for an interactive selection.
    public static VNOptionSelectionDecision PresentInteractive()
    {
        return new VNOptionSelectionDecision
        {
            ShouldPresentInteractive = true,
        };
    }
}