internal sealed class ConflictForkContext
{
    public LocalSaveFile Save { get; }
    public SyncBatch Pending { get; }

    public int SceneIndex { get; }

    public string SourcePlaythroughId { get; }
    public string ForkPlaythroughId { get; }

    public ForkOrigin Origin { get; }

    public ConflictForkContext(
        LocalSaveFile save,
        SyncBatch pending,
        int sceneIndex,
        string sourcePlaythroughId,
        string forkPlaythroughId,
        ForkOrigin origin)
    {
        Save = save;
        Pending = pending;
        SceneIndex = sceneIndex;
        SourcePlaythroughId = sourcePlaythroughId;
        ForkPlaythroughId = forkPlaythroughId;
        Origin = origin;
    }
}