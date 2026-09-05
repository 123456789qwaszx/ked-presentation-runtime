using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 즐겨찾기를 서버에 올리고 지운다. 큐 없이 직접 — 멱등이라 실패는 다음 기회(다음 시작·다음 찍기)에 다시.
// revision이 없다. 마지막 PUT이 이긴다. 409 처리는 만들지 않는다.
//
// 로컬 파일이 진실. 못 올린 것은 SyncedAtUtc가 비어 있고, 못 지운 것은 PendingDeletes에 남는다.
public sealed class ServerBookmarkSync
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ISaveStore _localStore;

    public ServerBookmarkSync(
        ServerApi api, GuestSession session, ChapterVersionResolver versionResolver, ISaveStore localStore)
    {
        _api = api;
        _session = session;
        _versionResolver = versionResolver;
        _localStore = localStore;
    }

    // 시작 시 — 못 지운 것부터, 그다음 못 올린 것.
    public async Task SyncAllAsync()
    {
        BookmarkFile file = _localStore.LoadBookmarks();

        var deletes = new List<string>(file.PendingDeletes);

        for (int i = 0; i < deletes.Count; i++)
            await DeleteAsync(deletes[i]);

        var pushes = new List<string>();

        for (int i = 0; i < file.Items.Count; i++)
        {
            Bookmark b = file.Items[i];

            if (b.SyncedAtUtc == null && b.SyncError == null)
                pushes.Add(b.Id);
        }

        for (int i = 0; i < pushes.Count; i++)
            await PushAsync(pushes[i]);
    }

    // 파일에서 다시 읽어 보낸다 — 부르는 사이에 이름이 바뀌었을 수 있다. 성공하면 SyncedAtUtc를 적는다.
    public async Task<bool> PushAsync(string bookmarkId)
    {
        Bookmark bookmark = Find(_localStore.LoadBookmarks(), bookmarkId);

        if (bookmark == null)
            return false;

        int? chapterVersion = await _versionResolver.ResolveAsync(bookmark.ChapterId);

        if (chapterVersion == null)
            return false;

        // 서버 상한: label 100, preview 200. 라인 텍스트가 길면 잘라 보낸다 — 원문은 스냅샷 안에 그대로 있다.
        var request = new BookmarkUpsertRequestDto
        {
            Label = Clip(bookmark.Label, 100),
            Preview = Clip(bookmark.Preview ?? string.Empty, 200),
            ChapterId = bookmark.ChapterId,
            ChapterVersion = chapterVersion.Value,
            PlaythroughClientId = bookmark.PlaythroughId,
            SceneIndex = bookmark.SceneIndex,
            CreatedAt = bookmark.CreatedAtUtc,
            Snapshot = bookmark,
        };

        ApiResult<BookmarkUpsertResponseDto> result =
            await _session.CallAsync(token => _api.PutBookmarkAsync(_session.UserId.Value, bookmarkId, request, token));

        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark now = Find(file, bookmarkId);

        if (result.Ok)
        {
            if (now != null)
            {
                now.SyncedAtUtc = result.Body.UpdatedAt;
                now.SyncError = null;
                _localStore.SaveBookmarks(file);
            }

            Debug.Log($"[즐겨찾기] 서버 {(result.Status == 201 ? "등록" : "갱신")}({result.Status}) — \"{bookmark.Label}\"");

            return true;
        }

        // 413은 재시도하지 않는다 — 줄여 보내야 한다. 표시해 두고 로그로 드러낸다.
        if (result.Status == 413)
        {
            if (now != null)
            {
                now.SyncError = result.ErrorCode ?? "PAYLOAD_TOO_LARGE";
                _localStore.SaveBookmarks(file);
            }

            Debug.LogError($"[즐겨찾기] 서버 상한 초과(413) — \"{bookmark.Label}\": {result.RawBody}");
        }
        else if (!result.NetworkError)
        {
            Debug.LogWarning($"[즐겨찾기] 서버 등록 실패 — HTTP {result.Status} {result.ErrorCode}. 다음 기회에.");
        }

        return false;
    }

    // 204면 PendingDeletes에서 뺀다. 서버에 없어도 204라 재시도가 자유롭다.
    public async Task<bool> DeleteAsync(string bookmarkId)
    {
        ApiResult<object> result =
            await _session.CallAsync(token => _api.DeleteBookmarkAsync(_session.UserId.Value, bookmarkId, token));

        if (!result.Ok)
        {
            if (!result.NetworkError)
                Debug.LogWarning($"[즐겨찾기] 서버 삭제 실패 — HTTP {result.Status} {result.ErrorCode}. 다음 기회에.");

            return false;
        }

        BookmarkFile file = _localStore.LoadBookmarks();

        if (file.PendingDeletes.Remove(bookmarkId))
            _localStore.SaveBookmarks(file);

        Debug.Log($"[즐겨찾기] 서버 삭제(204) — {bookmarkId}");

        return true;
    }

    private static string Clip(string text, int max) =>
        text == null || text.Length <= max ? text : text.Substring(0, max);

    private static Bookmark Find(BookmarkFile file, string id) =>
        file.Items.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));
}
