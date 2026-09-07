using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 이어하기 snapshot 하나와 수동 슬롯 요약을 복구한다. 북마크 본문은 선택 시 받는다.
public sealed class ServerRestore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ILocalSaveStore _localStore;
    private Task<bool> _resumeTask;
    private Task<bool> _indexTask;

    public ServerRestore(ServerApi api, GuestSession session, ILocalSaveStore localStore)
    {
        _api = api; _session = session; _localStore = localStore;
    }

    private bool Begin()
    {
        if (_session.UserId == null) return false;
        RestoreProgress progress = _localStore.LoadRestoreProgress();
        if (progress.Started) return true;
        if (_localStore.ListPlaythroughIds().Count > 0 || !progress.AllowActivation) return false;
        progress.Started = true;
        _localStore.SaveRestoreProgress(progress);
        return true;
    }

    public async Task<bool> RestoreAsync()
    {
        bool resume = await RestoreResumeAsync();
        bool index = await RestoreBookmarkIndexAsync();
        return resume && index;
    }

    public Task<bool> RestoreResumeAsync()
    {
        if (_resumeTask != null && !_resumeTask.IsCompleted) return _resumeTask;
        return _resumeTask = RestoreResumeCoreAsync();
    }

    private async Task<bool> RestoreResumeCoreAsync()
    {
        if (!Begin()) return false;
        if (_localStore.LoadRestoreProgress().ResumeCompleted) return true;
        var result = await _session.CallAsync(token => _api.GetResumeAsync(_session.UserId.Value, token));
        if (!result.Ok) return false;
        if (result.Status != 204)
        {
            ResumeSaveDto data = result.Body;
            if (data?.Playthrough?.ClientPlaythroughId == null || data.Save?.Snapshot == null || data.NextChoiceSeq < 1) return false;
            LocalSaveFile save = data.Save.Snapshot.ToObject<LocalSaveFile>(SaveJson.Serializer);
            if (save == null) return false;
            save.PlaythroughId = data.Playthrough.ClientPlaythroughId;
            _localStore.ImportRestored(save, data.Playthrough.Id, data.Save.Revision, data.NextChoiceSeq);
            _localStore.TryActivateRestored(save.PlaythroughId);
        }
        RestoreProgress progress = _localStore.LoadRestoreProgress();
        progress.ResumeCompleted = true;
        progress.Completed = progress.BookmarksCompleted;
        _localStore.SaveRestoreProgress(progress);
        return true;
    }

    public Task<bool> RestoreBookmarkIndexAsync()
    {
        if (_indexTask != null && !_indexTask.IsCompleted) return _indexTask;
        return _indexTask = RestoreBookmarkIndexCoreAsync();
    }

    private async Task<bool> RestoreBookmarkIndexCoreAsync()
    {
        if (!Begin()) return false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            RestoreProgress progress = _localStore.LoadRestoreProgress();
            if (progress.BookmarksCompleted) return true;
            string cursor = progress.BookmarkCursor;
            if (!seen.Add(cursor ?? "")) throw new InvalidOperationException("서버 bookmark cursor가 반복된다.");
            var result = await _session.CallAsync(token => _api.GetBookmarkPageAsync(_session.UserId.Value, cursor, token));
            if (!result.Ok || result.Body?.Items == null) return false;
            BookmarkFile file = _localStore.LoadBookmarks();
            foreach (BookmarkDetailDto remote in result.Body.Items)
            {
                string id = remote.ClientBookmarkId;
                if (string.IsNullOrEmpty(id) || file.DeletedIds.Contains(id) || file.Bookmarks.Exists(b => b.Id == id)) continue;
                file.Bookmarks.Add(new Bookmark
                {
                    Id = id, Label = remote.Label, Preview = remote.Preview, CreatedAtUtc = remote.CreatedAt,
                    ChapterId = remote.ChapterId, PlaythroughId = remote.PlaythroughClientId, SceneIndex = remote.SceneIndex,
                    LocalVersion = Math.Max(1, remote.ClientVersion), SyncedVersion = remote.ClientVersion, SyncedAtUtc = remote.UpdatedAt,
                });
            }
            _localStore.SaveBookmarks(file);
            progress = _localStore.LoadRestoreProgress();
            progress.BookmarkCursor = result.Body.NextCursor;
            progress.BookmarksCompleted = result.Body.NextCursor == null;
            progress.Completed = progress.ResumeCompleted && progress.BookmarksCompleted;
            _localStore.SaveRestoreProgress(progress);
        }
    }

    public async Task<Bookmark> HydrateBookmarkAsync(string id)
    {
        Bookmark loaded = _localStore.LoadBookmark(id);
        if (loaded != null) return loaded;
        Bookmark before = _localStore.LoadBookmarks().Bookmarks.Find(b => b.Id == id);
        if (before == null || _session.UserId == null) return null;
        var result = await _session.CallAsync(token => _api.GetBookmarkAsync(_session.UserId.Value, id, token));
        if (!result.Ok || result.Body?.Snapshot == null) return null;
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark now = file.Bookmarks.Find(b => b.Id == id);
        if (now == null || file.DeletedIds.Contains(id)) return null;
        if (now.LocalVersion != before.LocalVersion || now.SnapshotKey != null) return _localStore.LoadBookmark(id);
        if (result.Body.ClientVersion < before.LocalVersion) return null;
        Bookmark body = result.Body.Snapshot.ToObject<Bookmark>(SaveJson.Serializer);
        if (body?.Checkpoint == null) return null;
        body.Id = id;
        body.SnapshotKey = null;
        body.LocalVersion = result.Body.ClientVersion;
        body.SyncedVersion = result.Body.ClientVersion;
        body.Label = result.Body.Label;
        body.SyncedAtUtc = result.Body.UpdatedAt;
        body.SyncError = null;
        file.Bookmarks[file.Bookmarks.IndexOf(now)] = body;
        _localStore.SaveBookmarks(file);
        return _localStore.LoadBookmark(id);
    }
}
