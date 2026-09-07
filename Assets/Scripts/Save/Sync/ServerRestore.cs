using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// 새 기기 복구. 완료 표시는 회차·북마크가 모두 성공했을 때만 저장한다.
// 매 응답 후 최신 로컬을 다시 확인하며, 이미 생긴 로컬 데이터는 덮지 않는다.
public sealed class ServerRestore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ILocalSaveStore _localStore;

    public ServerRestore(ServerApi api, GuestSession session, ILocalSaveStore localStore)
    {
        _api = api; _session = session; _localStore = localStore;
    }

    public async Task<bool> RestoreAsync()
    {
        if (_session.UserId == null) return false;
        RestoreProgress progress = _localStore.LoadRestoreProgress();
        if (progress.Completed) return false;
        if (!progress.Started)
        {
            if (_localStore.ListPlaythroughIds().Count > 0 || !progress.AllowActivation) return false;
            progress.Started = true;
            _localStore.SaveRestoreProgress(progress);
        }

        long userId = _session.UserId.Value;
        var list = await _session.CallAsync(token => _api.GetPlaythroughsAsync(userId, token));
        if (!list.Ok || list.Body == null) return false;
        bool complete = true;
        var candidates = list.Body.Where(p => p.ClientPlaythroughId != null && p.ChapterId != null)
            .OrderByDescending(p => p.LastSavedAt, StringComparer.Ordinal);
        foreach (PlaythroughSummaryDto summary in candidates)
        {
            try
            {
                if (await RestorePlaythroughAsync(summary))
                    _localStore.TryActivateRestored(summary.ClientPlaythroughId);
                else complete = false;
            }
            catch (Exception error)
            {
                complete = false;
                Debug.LogError($"[복구] 회차 {summary.ClientPlaythroughId} 보류\n{error}");
            }
        }
        complete &= await RestoreBookmarksAsync(userId);
        // await 동안 명시적 새 게임이 AllowActivation을 바꿨을 수 있다.
        progress = _localStore.LoadRestoreProgress();
        progress.Completed = complete;
        _localStore.SaveRestoreProgress(progress);
        return complete;
    }

    private async Task<bool> RestorePlaythroughAsync(PlaythroughSummaryDto summary)
    {
        if (_localStore.Open(summary.ClientPlaythroughId) != null) return true;
        var detail = await _session.CallAsync(token =>
            _api.GetSaveAsync(summary.Id, ServerSaveContract.PrimarySlotNo, token));
        if (!detail.Ok || detail.Body?.Snapshot == null) return false;
        LocalSaveFile file = detail.Body.Snapshot.ToObject<LocalSaveFile>(SaveJson.Serializer);
        if (file == null) return false;
        var choices = await _session.CallAsync(token =>
            _api.GetChoicesAsync(summary.Id, ServerSaveContract.PrimarySlotNo, token));
        if (!choices.Ok || choices.Body == null) return false;
        int lastSeq = choices.Body.Count == 0 ? 0 : choices.Body.Max(c => c.Seq);
        file.PlaythroughId = summary.ClientPlaythroughId;
        _localStore.ImportRestored(file, summary.Id, detail.Body.Revision, lastSeq + 1);
        return true;
    }

    private async Task<bool> RestoreBookmarksAsync(long userId)
    {
        var list = await _session.CallAsync(token => _api.GetBookmarksAsync(userId, token));
        if (!list.Ok || list.Body == null) return false;
        bool complete = true;
        foreach (BookmarkDetailDto item in list.Body)
        {
            try
            {
                string id = item.ClientBookmarkId;
                BookmarkFile local = _localStore.LoadBookmarks();
                if (local.Bookmarks.Any(b => b.Id == id) || local.DeletedIds.Contains(id)) continue;
                var single = await _session.CallAsync(token => _api.GetBookmarkAsync(userId, id, token));
                if (!single.Ok || single.Body?.Snapshot == null) { complete = false; continue; }
                Bookmark bookmark = single.Body.Snapshot.ToObject<Bookmark>(SaveJson.Serializer);
                if (bookmark == null) { complete = false; continue; }
                local = _localStore.LoadBookmarks();
                if (local.Bookmarks.Any(b => b.Id == id) || local.DeletedIds.Contains(id)) continue;
                bookmark.Id = id;
                bookmark.SyncedAtUtc = single.Body.UpdatedAt;
                bookmark.SyncError = null;
                local.Bookmarks.Add(bookmark);
                _localStore.SaveBookmarks(local);
            }
            catch (Exception error)
            {
                complete = false;
                Debug.LogError($"[복구] 즐겨찾기 {item.ClientBookmarkId} 보류\n{error}");
            }
        }
        return complete;
    }
}
