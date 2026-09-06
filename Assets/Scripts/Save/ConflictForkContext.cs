internal sealed class ConflictForkContext
{
    public LocalSaveFile Save { get; }
    public SyncBatch Pending { get; }

    public int SceneIndex { get; }

    // Save.PlaythroughId는 중간에 ForkPlaythroughId로 바뀐다.
    // 따라서 fork 이전 id는 별도 snapshot으로 보존한다.
    public string SourcePlaythroughId { get; }
    public string ForkPlaythroughId { get; }

    public ForkOrigin Origin { get; }

    public ConflictForkPhase Phase { get; internal set; } = ConflictForkPhase.None;

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