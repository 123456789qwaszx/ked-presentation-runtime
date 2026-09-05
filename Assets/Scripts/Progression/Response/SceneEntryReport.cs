using Ked.Progression;

// 장면 진입 스냅샷.
public sealed class SceneEntryReport
{
    public string ChapterId { get; }
    public ProgressionState State { get; }
    public YarnVariableSnapshot Variables { get; }
    public int BacklogSerialStart { get; }

    public SceneEntryReport(
        string chapterId, 
        ProgressionState state,
        YarnVariableSnapshot variables,
        int backlogSerialStart)
    {
        ChapterId = chapterId;
        State = state;
        Variables = variables;
        BacklogSerialStart = backlogSerialStart;
    }
}