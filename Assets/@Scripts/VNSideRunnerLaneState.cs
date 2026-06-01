using Yarn.Unity;

public sealed class VNSideRunnerLaneState
{
    public readonly string LaneKey;
    public readonly DialogueRunner Runner;

    public int PendingAdvanceCount;
    public bool IsReadyForAdvance;

    public VNSideRunnerLaneState(string laneKey, DialogueRunner runner)
    {
        LaneKey = laneKey;
        Runner = runner;
    }

    public void Clear()
    {
        PendingAdvanceCount = 0;
        IsReadyForAdvance = false;
    }

    public void ResetForRestart()
    {
        Clear();
    }
}