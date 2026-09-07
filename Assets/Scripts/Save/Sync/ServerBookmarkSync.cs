using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 즐겨찾기를 서버에 올리고 지운다.
// 큐 없이 직접 - 멱등이라 실패는 다음에 다시.
// revision이 없다. 마지막 PUT이 이긴다. 409 처리는 만들지 않는다.
//
// 로컬 파일이 진실.
// 아직 서버 동기화가 완료되지 않은 북마크는 SyncedAtUtc가 비어 있고,
// 서버 삭제가 완료되지 않은 북마크 id는 PendingDeletes에 남는다
public sealed class ServerBookmarkSync
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ChapterVersionResolver _versionResolver;
    private readonly ILocalSaveStore _localStore;

    public ServerBookmarkSync(
        ServerApi api,
        GuestSession session,
        ChapterVersionResolver versionResolver,
        ILocalSaveStore localStore)
    {
        _api = api;
        _session = session;
        _versionResolver = versionResolver;
        _localStore = localStore;
    }

    public async Task SyncAllAsync()
    {
        BookmarkFile file = _localStore.LoadBookmarks();

        var deletes = new List<string>(file.PendingDeletes);

        for (int i = 0; i < deletes.Count; i++)
            await DeleteAsync(deletes[i]);

        var pushes = new List<string>();

        for (int i = 0; i < file.Bookmarks.Count; i++)
        {
            Bookmark b = file.Bookmarks[i];

            // 서버에 올라간 적 없고, 재시도 불가능 표기도 없을 경우.
            if (b.SyncedAtUtc == null && b.SyncError == null)
                pushes.Add(b.Id);
        }

        for (int i = 0; i < pushes.Count; i++)
            await PushAsync(pushes[i]);
    }

    public async Task<bool> PushAsync(string bookmarkId)
    {
        // 전송 직전 최신 로컬 상태로 다시 읽기.
        Bookmark bookmark =
            Find(_localStore.LoadBookmarks(), bookmarkId);

        if (bookmark == null)
            return false;

        int? chapterVersion =
            await _versionResolver.ResolveAsync(bookmark.ChapterId);

        if (chapterVersion == null)
            return false;

        // 서버 상한: label 100, preview 200.
        // 라인 텍스트가 길면 잘라 보냄(원문은 스냅샷 안에 보관 중)
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
            await _session.CallAsync(
                token => _api.PutBookmarkAsync(
                    _session.UserId.Value,
                    bookmarkId,
                    request,
                    token));

        // 전송 중 Bookmark 수정이 흐름 상 허용되서 race 발생 가능.
        // 이건 UI로 막을 것.
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

        // 413은 재시도하지 않는다 - 줄여 보내야 한다. 표시해 두고 로그로 드러낸다.
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

    // 204면 PendingDeletes에서 뺀다.
    // 서버에 반영 실패하더라도, 클라측에서는 삭제 완료기에 204.
    // 차후 PendingDeletes를 토대로 서버 반영을 재시도 하면 그만임.
    public async Task<bool> DeleteAsync(string bookmarkId)
    {
        ApiResult<object> result =
            await _session.CallAsync(
                token => _api.DeleteBookmarkAsync(
                    _session.UserId.Value,
                    bookmarkId,
                    token));

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
        file.Bookmarks.Find(b => string.Equals(b.Id, id, StringComparison.Ordinal));
}
