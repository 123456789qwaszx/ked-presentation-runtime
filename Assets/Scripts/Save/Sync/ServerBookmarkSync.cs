using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// 슬롯별 PUT/DELETE를 직렬화한다. 덮어쓰기 이전 응답으로 최신 슬롯을 완료 처리하지 않는다.
public sealed class ServerBookmarkSync
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ILocalSaveStore _localStore;
    private readonly Dictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    public ServerBookmarkSync(ServerApi api, GuestSession session, ChapterVersionResolver versionResolver, ILocalSaveStore localStore)
    {
        _api = api; _session = session; _versionResolver = versionResolver; _localStore = localStore;
    }

    private SemaphoreSlim Gate(string id)
    {
        if (!_gates.TryGetValue(id, out SemaphoreSlim gate)) _gates.Add(id, gate = new SemaphoreSlim(1, 1));
        return gate;
    }

    public async Task SyncAllAsync()
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        foreach (string id in file.PendingDeletes) await DeleteAsync(id);
        foreach (Bookmark bookmark in file.Bookmarks)
            if (bookmark.SyncedAtUtc == null && bookmark.SyncError == null) await PushAsync(bookmark.Id);
    }

    public async Task<bool> PushAsync(string id)
    {
        SemaphoreSlim gate = Gate(id);
        await gate.WaitAsync();
        try
        {
            Bookmark bookmark = _localStore.LoadBookmark(id);
            if (bookmark == null || bookmark.SyncedAtUtc != null || bookmark.SyncError != null) return false;
            int? version = await _versionResolver.ResolveAsync(bookmark.ChapterId);
            if (version == null) return false;
            var request = new BookmarkUpsertRequestDto
            {
                Label = Clip(bookmark.Label, 100), Preview = Clip(bookmark.Preview ?? "", 200),
                ChapterId = bookmark.ChapterId, ChapterVersion = version.Value,
                PlaythroughClientId = bookmark.PlaythroughId, SceneIndex = bookmark.SceneIndex,
                CreatedAt = bookmark.CreatedAtUtc, Snapshot = bookmark, ClientVersion = bookmark.LocalVersion,
                BaseVersion = bookmark.SyncedVersion,
            };
            var result = await _session.CallAsync(token => _api.PutBookmarkAsync(_session.UserId.Value, id, request, token));
            BookmarkFile latest = _localStore.LoadBookmarks();
            Bookmark now = latest.Bookmarks.Find(b => b.Id == id);
            if (now != null)
            {
                if (result.Ok)
                {
                    now.SyncedVersion = Math.Max(now.SyncedVersion, bookmark.LocalVersion);
                    if (now.LocalVersion == bookmark.LocalVersion)
                    {
                        now.SyncedAtUtc = result.Body.UpdatedAt;
                        now.SyncError = null;
                    }
                    _localStore.SaveBookmarks(latest);
                }
                else if (now.LocalVersion == bookmark.LocalVersion && result.Status >= 400
                    && result.Status < 500 && result.Status != 408 && result.Status != 429)
                {
                    now.SyncError = result.ErrorCode ?? "BOOKMARK_REJECTED";
                    _localStore.SaveBookmarks(latest);
                }
            }
            return result.Ok;
        }
        catch (Exception error) { Debug.LogError($"[수동 저장] {id} 전송 보류\n{error}"); return false; }
        finally { gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        SemaphoreSlim gate = Gate(id);
        await gate.WaitAsync();
        try
        {
            if (!_localStore.LoadBookmarks().PendingDeletes.Contains(id)) return true;
            var result = await _session.CallAsync(token => _api.DeleteBookmarkAsync(_session.UserId.Value, id, token));
            if (!result.Ok) return false;
            BookmarkFile latest = _localStore.LoadBookmarks();
            if (latest.PendingDeletes.Remove(id)) _localStore.SaveBookmarks(latest);
            return true;
        }
        catch (Exception error) { Debug.LogError($"[수동 저장] {id} 삭제 전달 보류\n{error}"); return false; }
        finally { gate.Release(); }
    }

    private static string Clip(string text, int max) => text == null || text.Length <= max ? text : text.Substring(0, max);
}
