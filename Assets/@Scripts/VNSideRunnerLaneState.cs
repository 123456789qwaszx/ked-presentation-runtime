using Yarn.Unity;

public sealed class VNSideRunnerLaneState
{
    public readonly string LaneKey;
    public readonly DialogueRunner Runner;

    public int PendingAdvanceCount;
    public bool IsReadyForAdvance;
    public int Generation;

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
        Generation++;
        Clear();
    }

    public string Snapshot()
    {
        string runnerName = Runner != null ? Runner.name : "null";

        return
            $"lane={LaneKey}, " +
            $"runner={runnerName}, " +
            $"generation={Generation}, " +
            $"pending={PendingAdvanceCount}, " +
            $"ready={IsReadyForAdvance}, " +
            $"running={(Runner != null && Runner.IsDialogueRunning)}";
    }
}