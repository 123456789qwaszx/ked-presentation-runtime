using System.Threading.Tasks;

// 네트워크 경계. 테스트는 응답을 원하는 시점에 완료시켜 전환·재시도를 검증한다.
public interface ISaveSyncTransport
{
    Task<long?> CreatePlaythroughAsync(LocalSaveFile save);
    Task<int?> ResolveChapterVersionAsync(string chapterId);
    Task<ApiResult<SaveUploadResponseDto>> UploadAsync(long serverId, SyncWork work);
}

public sealed class SaveSyncTransport : ISaveSyncTransport
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ChapterVersionResolver _versions;
    private readonly string _deviceKey;

    public SaveSyncTransport(ServerApi api, GuestSession session, ChapterVersionResolver versions, string deviceKey)
    {
        _api = api; _session = session; _versions = versions; _deviceKey = deviceKey;
    }

    public async Task<long?> CreatePlaythroughAsync(LocalSaveFile save)
    {
        var request = new PlaythroughCreateRequestDto
        {
            ClientPlaythroughId = save.PlaythroughId,
            ForkedFrom = save.ForkedFrom == null ? null : new ForkOriginDto
            {
                ClientPlaythroughId = save.ForkedFrom.PlaythroughId,
                SceneIndex = save.ForkedFrom.SceneIndex,
            },
        };
        var result = await _session.CallAsync(token => _api.CreatePlaythroughAsync(_session.UserId.Value, request, token));
        return result.Ok ? result.Body.PlaythroughId : (long?)null;
    }

    public Task<int?> ResolveChapterVersionAsync(string chapterId) => _versions.ResolveAsync(chapterId);

    public Task<ApiResult<SaveUploadResponseDto>> UploadAsync(long serverId, SyncWork work)
    {
        LocalSaveFile save = work.Snapshot;
        var request = new SaveUploadRequestDto
        {
            ChapterId = save.ChapterId,
            ChapterVersion = work.ChapterVersion.Value,
            CurrentEpisodeId = save.CurrentEpisodeId,
            Snapshot = save,
            PlaySeconds = save.PlaySeconds,
            DeviceKey = _deviceKey,
            BaseRevision = work.BaseRevision,
            Choices = work.Choices,
            Events = work.Events,
            InheritedPlaySeconds = save.InheritedPlaySeconds,
            OwnPlaySeconds = save.OwnPlaySeconds,
            ChapterCompleted = save.ChapterCompleted,
        };
        return _session.CallAsync(token => _api.PutSaveAsync(serverId, ServerSaveContract.PrimarySlotNo, request, token));
    }
}
