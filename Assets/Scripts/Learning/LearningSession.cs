// 학습 서버 연결 상태만 보관한다.
// 기존 PlaythroughFile.Sync의 서버 ID/revision/ACK와 섞지 않는다.
public static class LearningSession
{
    public static string ChapterKey { get; private set; }
    public static string ClientPlaythroughId { get; private set; }
    public static long? ServerPlaythroughId { get; private set; }
    public static LocalSaveFile LatestSnapshot { get; private set; }
    public static ILocalSaveStore LocalStore { get; private set; }

    public static void BindLocalStore(ILocalSaveStore localStore)
    {
        LocalStore = localStore;
    }

    public static void Capture(LocalSaveFile snapshot)
    {
        if (snapshot == null)
            return;

        bool changedPlaythrough =
            ClientPlaythroughId != snapshot.PlaythroughId;

        ChapterKey = snapshot.ChapterId;
        ClientPlaythroughId = snapshot.PlaythroughId;
        LatestSnapshot = snapshot;

        if (changedPlaythrough)
            ServerPlaythroughId = null;
    }

    public static void BindServer(
        string clientPlaythroughId,
        long serverPlaythroughId)
    {
        if (ClientPlaythroughId != clientPlaythroughId)
            return;

        ServerPlaythroughId = serverPlaythroughId;
    }
}
