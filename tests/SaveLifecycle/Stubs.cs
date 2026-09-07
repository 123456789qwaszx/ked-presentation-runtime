// 이 harness의 대역 범위는 Unity/Yarn 재생과 HTTP 경계뿐이다.
// Save, Coordinator, Restore, Worker, GuestSession, Launcher는 실제 소스를 컴파일한다.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class Time { public static float realtimeSinceStartup; }
    public static class Debug
    {
        public static readonly List<string> Errors = new();
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
        public static void LogError(object value) { Errors.Add(value.ToString()); }
    }
    public sealed class TextAsset { public byte[] bytes = Array.Empty<byte>(); }
}
namespace Yarn.Unity { public sealed class DialogueRunner { public object YarnProject; } }
public struct YarnLineMeta { public string lineId, nodeName, rawText; }
public sealed class RollbackPoint { public int historyIndex; }
public static class ProgressionContentLoader
{
    public static Ked.Progression.ScenarioProgression LoadSingleChapter(UnityEngine.TextAsset asset)
    {
        var chapter = new Ked.Progression.ChapterProgression("chapter", "", "scene1", null,
            new[] { new Ked.Progression.EpisodeNode("scene1", "", "node") });
        return new Ked.Progression.ScenarioProgression("scenario", "", "chapter", new[] { chapter });
    }
}
public static class ProgressionContentPreflight
{
    public static bool CheckAndLog(Ked.Progression.ScenarioProgression scenario, object yarn) => true;
}
public sealed class ProgressionDriver
{
    public bool IsRunning;
    public IReadOnlyList<CommittedChoice> PendingPath = Array.Empty<CommittedChoice>();
    public Func<Task> OnStop;
    public int Starts;
    public Task RequestReplayAsync() => Task.CompletedTask;
    public async Task StopAsync() { if (OnStop != null) await OnStop(); IsRunning = false; }
    public Task RunAsync(object yarn, Ked.Progression.ChapterProgression chapter, Ked.Progression.ProgressionState state,
        YarnVariableSnapshot variables, IReadOnlyList<DialogueLogEntry> backlog, SavedLoadPlan plan)
    { Starts++; IsRunning = true; return Task.CompletedTask; }
}
public sealed class ServerApi
{
    public Func<Task<ApiResult<ResumeSaveDto>>> OnResume;
    public Func<string,Task<ApiResult<BookmarkPageDto>>> OnPage;
    public Func<BookmarkUpsertRequestDto,Task<ApiResult<BookmarkUpsertResponseDto>>> OnPutBookmark;
    public Func<string,Task<ApiResult<object>>> OnDeleteBookmark;
    public Func<string,Task<ApiResult<List<ChapterVersionInfoDto>>>> OnVersions;
    public Task<ApiResult<ResumeSaveDto>> GetResumeAsync(long id,string t) => OnResume();
    public Task<ApiResult<BookmarkPageDto>> GetBookmarkPageAsync(long id,string cursor,string t) => OnPage(cursor);
    public Task<ApiResult<object>> SetResumeAsync(long id,ResumePointerRequestDto r,string t)
        => Task.FromResult(ApiResult<object>.Success(204,null,""));
    public Func<Task<ApiResult<List<PlaythroughSummaryDto>>>> OnList;
    public Func<long,Task<ApiResult<SaveSlotDetailDto>>> OnSave;
    public Func<long,Task<ApiResult<List<ChoiceHistoryItemDto>>>> OnChoices;
    public Func<Task<ApiResult<List<BookmarkDetailDto>>>> OnBookmarks;
    public Func<string,Task<ApiResult<BookmarkDetailDto>>> OnBookmark;
    public Func<SaveUploadRequestDto,Task<ApiResult<SaveUploadResponseDto>>> OnUpload;
    public Task<ApiResult<UserResponseDto>> SignUpAsync(string u,string p) => Task.FromResult(ApiResult<UserResponseDto>.Network("offline"));
    public Task<ApiResult<LoginResponseDto>> LoginAsync(string u,string p) => Task.FromResult(ApiResult<LoginResponseDto>.Network("offline"));
    public Task<ApiResult<PlaythroughCreatedDto>> CreatePlaythroughAsync(long u,PlaythroughCreateRequestDto r,string t)
        => Task.FromResult(ApiResult<PlaythroughCreatedDto>.Success(201,new PlaythroughCreatedDto { PlaythroughId = 10 }, ""));
    public Task<ApiResult<List<ChapterVersionInfoDto>>> GetChapterVersionsAsync(string id)
        => OnVersions == null ? Task.FromResult(ApiResult<List<ChapterVersionInfoDto>>.Network("offline")) : OnVersions(id);
    public Task<ApiResult<SaveUploadResponseDto>> PutSaveAsync(long id,int slot,SaveUploadRequestDto r,string t) => OnUpload(r);
    public Task<ApiResult<List<PlaythroughSummaryDto>>> GetPlaythroughsAsync(long id,string t) => OnList();
    public Task<ApiResult<SaveSlotDetailDto>> GetSaveAsync(long id,int slot,string t) => OnSave(id);
    public Task<ApiResult<List<ChoiceHistoryItemDto>>> GetChoicesAsync(long id,int slot,string t) => OnChoices(id);
    public Task<ApiResult<List<BookmarkDetailDto>>> GetBookmarksAsync(long id,string t) => OnBookmarks();
    public Task<ApiResult<BookmarkDetailDto>> GetBookmarkAsync(long uid,string id,string t) => OnBookmark(id);
    public Task<ApiResult<BookmarkUpsertResponseDto>> PutBookmarkAsync(long uid,string id,BookmarkUpsertRequestDto r,string t)
        => OnPutBookmark == null ? Task.FromResult(ApiResult<BookmarkUpsertResponseDto>.Network("offline")) : OnPutBookmark(r);
    public Task<ApiResult<object>> DeleteBookmarkAsync(long uid,string id,string t)
        => OnDeleteBookmark == null ? Task.FromResult(ApiResult<object>.Success(204,null,"")) : OnDeleteBookmark(id);
}
