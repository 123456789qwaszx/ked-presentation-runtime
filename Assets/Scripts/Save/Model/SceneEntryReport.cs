using Ked.Progression;

// 장면 진입 스냅샷.
public sealed class SceneEntryReport
{
    public string ChapterId { get; }
    public ChapterState State { get; }
    public int BacklogSerialStart { get; }

    public SceneEntryReport(
        string chapterId, 
        ChapterState state,
        int backlogSerialStart)
    {
        ChapterId = chapterId;
        State = state;
        BacklogSerialStart = backlogSerialStart;
    }
}