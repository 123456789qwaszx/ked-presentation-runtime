using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 서버 복구:
// - 이어하기 snapshot 하나를 복구한다.
// - 수동 세이브 슬롯은 우선 목록(요약)만 복구한다.
// - 수동 세이브의 실제 snapshot은 사용자가 선택할 때 Hydrate한다.
//
// 복구는 로컬에 기존 회차가 없는 새 환경에서만 시작한다.
// 한번 시작된 복구는 RestoreProgress를 기준으로 중단된 지점부터 이어간다.
public sealed class ServerRestore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ILocalSaveStore _localStore;

    // 같은 복구 작업이 동시에 여러 번 실행되는 것을 막는다.
    private Task<bool> _resumeTask;
    private Task<bool> _bookmarkIndexTask;

    public ServerRestore(
        ServerApi api,
        GuestSession session,
        ILocalSaveStore localStore)
    {
        _api = api;
        _session = session;
        _localStore = localStore;
    }


    // ---------------------------------------------------------------------
    // Restore entry
    // ---------------------------------------------------------------------

    public async Task<bool> RestoreAsync()
    {
        bool resumeRestored = await RestoreResumeAsync();
        bool bookmarkIndexRestored = await RestoreBookmarkIndexAsync();

        return resumeRestored && bookmarkIndexRestored;
    }

    // 복구를 처음 시작할 수 있는지 확인한다.
    //
    // 이미 Started 상태라면, 중간에 로컬 회차가 생겼더라도
    // 이전에 시작한 복구를 계속할 수 있도록 허용한다.
    private bool TryBeginRestore()
    {
        if (_session.UserId == null)
            return false;

        RestoreProgress progress = _localStore.LoadRestoreProgress();

        if (progress.Started)
            return true;

        bool hasLocalPlaythrough =
            _localStore.ListPlaythroughIds().Count > 0;

        if (hasLocalPlaythrough)
            return false;

        if (!progress.AllowActivation)
            return false;

        progress.Started = true;
        _localStore.SaveRestoreProgress(progress);

        return true;
    }


    // ---------------------------------------------------------------------
    // Resume restore
    // ---------------------------------------------------------------------

    public Task<bool> RestoreResumeAsync()
    {
        if (_resumeTask != null && !_resumeTask.IsCompleted)
            return _resumeTask;

        _resumeTask = RestoreResumeCoreAsync();
        return _resumeTask;
    }

    private async Task<bool> RestoreResumeCoreAsync()
    {
        if (!TryBeginRestore())
            return false;

        RestoreProgress progress = _localStore.LoadRestoreProgress();

        if (progress.ResumeCompleted)
            return true;

        var result = await _session.CallAsync(
            token => _api.GetResumeAsync(
                _session.UserId.Value,
                token));

        if (!result.Ok)
            return false;

        // 204 = 서버에 이어하기 데이터가 없음.
        // 이것도 정상적으로 복구 단계를 완료한 것으로 본다.
        if (result.Status == 204)
        {
            MarkResumeCompleted();
            return true;
        }

        ResumeSaveDto remote = result.Body;

        if (!IsValidResume(remote))
            return false;

        if (!ImportResume(remote))
            return false;

        MarkResumeCompleted();

        return true;
    }

    private static bool IsValidResume(ResumeSaveDto remote)
    {
        if (remote == null)
            return false;

        if (remote.Playthrough?.ClientPlaythroughId == null)
            return false;

        if (remote.Save?.Snapshot == null)
            return false;

        if (remote.NextChoiceSeq < 1)
            return false;

        return true;
    }

    private bool ImportResume(ResumeSaveDto remote)
    {
        LocalSaveFile save =
            remote.Save.Snapshot.ToObject<LocalSaveFile>(
                SaveJson.Serializer);

        if (save == null)
            return false;

        save.PlaythroughId =
            remote.Playthrough.ClientPlaythroughId;

        _localStore.ImportRestored(
            save,
            remote.Playthrough.Id,
            remote.Save.Revision,
            remote.NextChoiceSeq);

        _localStore.TryActivateRestored(save.PlaythroughId);

        return true;
    }

    private void MarkResumeCompleted()
    {
        RestoreProgress progress =
            _localStore.LoadRestoreProgress();

        progress.ResumeCompleted = true;
        progress.Completed = progress.BookmarksCompleted;

        _localStore.SaveRestoreProgress(progress);
    }


    // ---------------------------------------------------------------------
    // Bookmark index restore
    // ---------------------------------------------------------------------

    public Task<bool> RestoreBookmarkIndexAsync()
    {
        if (_bookmarkIndexTask != null &&
            !_bookmarkIndexTask.IsCompleted)
        {
            return _bookmarkIndexTask;
        }

        _bookmarkIndexTask = RestoreBookmarkIndexCoreAsync();
        return _bookmarkIndexTask;
    }

    private async Task<bool> RestoreBookmarkIndexCoreAsync()
    {
        if (!TryBeginRestore())
            return false;

        var seenCursors =
            new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            RestoreProgress progress =
                _localStore.LoadRestoreProgress();

            if (progress.BookmarksCompleted)
                return true;

            string cursor = progress.BookmarkCursor;

            EnsureCursorDoesNotRepeat(seenCursors, cursor);

            var result = await _session.CallAsync(
                token => _api.GetBookmarkPageAsync(
                    _session.UserId.Value,
                    cursor,
                    token));

            if (!result.Ok)
                return false;

            if (result.Body?.Items == null)
                return false;

            BookmarkFile file =
                _localStore.LoadBookmarks();

            MergeBookmarkIndex(
                file,
                result.Body.Items);

            _localStore.SaveBookmarks(file);

            AdvanceBookmarkRestore(
                result.Body.NextCursor);
        }
    }

    private static void EnsureCursorDoesNotRepeat(
        HashSet<string> seenCursors,
        string cursor)
    {
        string key = cursor ?? string.Empty;

        if (seenCursors.Add(key))
            return;

        throw new InvalidOperationException(
            "서버 bookmark cursor가 반복된다.");
    }

    private static void MergeBookmarkIndex(
        BookmarkFile file,
        IEnumerable<BookmarkDetailDto> remoteBookmarks)
    {
        foreach (BookmarkDetailDto remote in remoteBookmarks)
        {
            if (!CanImportBookmarkSummary(file, remote))
                continue;

            file.Bookmarks.Add(
                CreateBookmarkSummary(remote));
        }
    }

    private static bool CanImportBookmarkSummary(
        BookmarkFile file,
        BookmarkDetailDto remote)
    {
        string id = remote.ClientBookmarkId;

        if (string.IsNullOrEmpty(id))
            return false;

        // 로컬에서 이미 삭제한 슬롯을 서버 복구로 되살리지 않는다.
        if (file.DeletedIds.Contains(id))
            return false;

        // 이미 가지고 있는 슬롯은 덮어쓰지 않는다.
        if (file.Bookmarks.Exists(bookmark => bookmark.Id == id))
            return false;

        return true;
    }

    private static Bookmark CreateBookmarkSummary(
        BookmarkDetailDto remote)
    {
        return new Bookmark
        {
            Id = remote.ClientBookmarkId,

            Label = remote.Label,
            Preview = remote.Preview,
            CreatedAtUtc = remote.CreatedAt,

            ChapterId = remote.ChapterId,
            PlaythroughId = remote.PlaythroughClientId,
            SceneIndex = remote.SceneIndex,

            LocalVersion =
                Math.Max(1, remote.ClientVersion),

            SyncedVersion =
                remote.ClientVersion,

            SyncedAtUtc =
                remote.UpdatedAt,
        };
    }

    private void AdvanceBookmarkRestore(string nextCursor)
    {
        RestoreProgress progress =
            _localStore.LoadRestoreProgress();

        progress.BookmarkCursor = nextCursor;
        progress.BookmarksCompleted = nextCursor == null;

        progress.Completed =
            progress.ResumeCompleted &&
            progress.BookmarksCompleted;

        _localStore.SaveRestoreProgress(progress);
    }


    // ---------------------------------------------------------------------
    // Bookmark hydration
    // ---------------------------------------------------------------------

    // 목록만 복구된 bookmark의 실제 snapshot을 필요할 때 서버에서 받는다.
    //
    // 서버 요청을 기다리는 동안 bookmark가 수정되거나 삭제될 수 있으므로,
    // await 전 상태와 await 후 상태를 비교한 뒤에만 서버 응답을 적용한다.
    public async Task<Bookmark> HydrateBookmarkAsync(string id)
    {
        Bookmark loaded = _localStore.LoadBookmark(id);

        if (loaded != null)
            return loaded;

        Bookmark beforeRequest =
            FindBookmarkSummary(id);

        if (beforeRequest == null)
            return null;

        if (_session.UserId == null)
            return null;

        var result = await _session.CallAsync(
            token => _api.GetBookmarkAsync(
                _session.UserId.Value,
                id,
                token));

        if (!result.Ok)
            return null;

        if (result.Body?.Snapshot == null)
            return null;


        // await 동안 로컬 상태가 바뀌었을 수 있으므로 다시 읽는다.
        BookmarkFile currentFile =
            _localStore.LoadBookmarks();

        Bookmark afterRequest =
            FindBookmark(currentFile, id);

        if (afterRequest == null)
            return null;

        // 요청 중 사용자가 삭제했다면 서버 응답으로 되살리지 않는다.
        if (currentFile.DeletedIds.Contains(id))
            return null;


        // 요청을 기다리는 사이 로컬 bookmark가 변경되었다.
        // 지금 받은 서버 응답으로 그 변경을 덮어쓰지 않는다.
        if (afterRequest.LocalVersion != beforeRequest.LocalVersion)
            return _localStore.LoadBookmark(id);

        // 다른 작업이 먼저 hydration을 완료했다면 그것을 사용한다.
        if (afterRequest.SnapshotKey != null)
            return _localStore.LoadBookmark(id);


        // 서버가 가진 데이터가 요청 당시 로컬보다 오래됐다면 적용하지 않는다.
        if (result.Body.ClientVersion < beforeRequest.LocalVersion)
            return null;


        Bookmark hydrated =
            result.Body.Snapshot.ToObject<Bookmark>(
                SaveJson.Serializer);

        if (hydrated?.Checkpoint == null)
            return null;


        ApplyHydratedBookmark(
            currentFile,
            afterRequest,
            hydrated,
            id,
            result.Body.ClientVersion,
            result.Body.Label,
            result.Body.UpdatedAt);

        return _localStore.LoadBookmark(id);
    }

    private Bookmark FindBookmarkSummary(string id)
    {
        BookmarkFile file =
            _localStore.LoadBookmarks();

        return FindBookmark(file, id);
    }

    private static Bookmark FindBookmark(
        BookmarkFile file,
        string id)
    {
        return file.Bookmarks.Find(
            bookmark => bookmark.Id == id);
    }

    private void ApplyHydratedBookmark(
        BookmarkFile file,
        Bookmark current,
        Bookmark hydrated,
        string id,
        int clientVersion,
        string label,
        string updatedAt)
    {
        hydrated.Id = id;

        hydrated.SnapshotKey = null;

        hydrated.LocalVersion = clientVersion;
        hydrated.SyncedVersion = clientVersion;

        hydrated.Label = label;
        hydrated.SyncedAtUtc = updatedAt;
        hydrated.SyncError = null;

        int index = file.Bookmarks.IndexOf(current);

        if (index < 0)
            return;

        file.Bookmarks[index] = hydrated;

        _localStore.SaveBookmarks(file);
    }
}