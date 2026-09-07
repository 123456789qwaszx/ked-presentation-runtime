using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// - 새기기 사용 시,
//   서버 사본을 토대로 초기 복구 용도
// (로컬에 데이터 있으면 merge 금지.)
//
// Server:
// - Playthrough 목록, SaveSnapshot, ChoiceHistory, BookmarkSnapshot을 통해
// - ServerRestore
// - LocalSaveFile 생성, SyncQueue 복원, BookmarkFile 복원
// - LocalFileSaveStore
//
// 계정(account.json)이 없으면 아무것도 하지 않는다.
public sealed class ServerRestore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ILocalSaveStore _localStore;

    public ServerRestore(
        ServerApi api,
        GuestSession session,
        ILocalSaveStore localStore)
    {
        _api = api;
        _session = session;
        _localStore = localStore;
    }
    
    public async Task<bool> RestoreAsync()
    {
        if (_session.UserId == null)
            return false;

        long userId = _session.UserId.Value;

        ApiResult<List<PlaythroughSummaryDto>> list =
            await _session.CallAsync(
                token => _api.GetPlaythroughsAsync(userId, token));

        if (!list.Ok)
        {
            if (!list.NetworkError)
                Debug.LogWarning(
                    $"[복구] 회차 목록 획득 실패 - HTTP {list.Status} {list.ErrorCode}");

            return false;
        }

        // 클라 id와 슬롯이 있는 회차만.
        // lastSavedAt 오름차순으로 저장하면 마지막 것이 활성으로 남는다.
        // _localStore.SaveAndSetActive는 A,B,C 순서대로 복구 -> active를 진행하기 때문.
        var candidates = new List<PlaythroughSummaryDto>();

        for (int i = 0; i < list.Body.Count; i++)
        {
            PlaythroughSummaryDto p = list.Body[i];

            if (p.ClientPlaythroughId != null && p.ChapterId != null)
                candidates.Add(p);
        }

        candidates.Sort((a, b) => string.CompareOrdinal(a.LastSavedAt, b.LastSavedAt));

        int restored = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (await RestorePlaythroughAsync(candidates[i]))
                restored++;
        }

        int bookmarks = await RestoreBookmarksAsync(userId);

        Debug.Log(
            $"[복구] 서버에서 회차 {restored}/{candidates.Count}개, 즐겨찾기 {bookmarks}개 재구성" +
            (restored > 0 
                ? $" — 활성 {_localStore.ActiveId}"
                : string.Empty));

        return restored > 0 || bookmarks > 0;
    }

    // LocalSaveFile, SyncQueueFile 둘 다 복구.
    private async Task<bool> RestorePlaythroughAsync(PlaythroughSummaryDto summary)
    {
        // ServerSyncSaveStore가 'Snapshot = save'로 통째로 LocalSaveFile을 보관 중.
        ApiResult<SaveSlotDetailDto> detail =
            await _session.CallAsync(
                token => 
                    _api.GetSaveAsync(
                        summary.Id, ServerSaveContract.PrimarySlotNo, token));

        if (!detail.Ok || detail.Body.Snapshot == null)
        {
            Debug.LogWarning($"[복구] 회차 {summary.ClientPlaythroughId} 스냅샷 실패 — HTTP {detail.Status} {detail.ErrorCode}");
            return false;
        }

        LocalSaveFile file = 
            detail.Body.Snapshot.ToObject<LocalSaveFile>(SaveJson.Serializer);

        if (file == null)
            return false;

        // 서버에서 Choice History를 가져옴.
        // 다음 seq는 서버 이력의 마지막 + 1.
        // 이력을 못 읽으면 seq가 겹칠 수 있어 이 회차는 건너뛴다.
        ApiResult<List<ChoiceHistoryItemDto>> choices =
            await _session.CallAsync(
                token => 
                    _api.GetChoicesAsync(
                        summary.Id, ServerSaveContract.PrimarySlotNo, token));

        if (!choices.Ok)
        {
            Debug.LogWarning(
                $"[복구] 회차 {summary.ClientPlaythroughId} 선택 이력 실패" +
                $" HTTP {choices.Status} {choices.ErrorCode}");
            return false;
        }

        int lastSeq = 0;

        for (int i = 0; i < choices.Body.Count; i++)
            lastSeq = Math.Max(lastSeq, choices.Body[i].Seq);

        // 현재 서버측 PlaythroughId는 version 개념.
        // local GUID와는 다름.
        file.PlaythroughId = summary.ClientPlaythroughId;

        _localStore.SaveAndSetActive(file);

        // *.queue.json도 복구
        new SyncQueue(_localStore.QueuePathOf(file.PlaythroughId))
            .Restore(summary.Id, detail.Body.Revision, lastSeq + 1, file.Scenes?.Count ?? 0);

        Debug.Log(
            $"[복구] 회차 {file.PlaythroughId} ← 서버 {summary.Id} (revision {detail.Body.Revision}, 선택 {lastSeq}건, " +
            $"{file.ChapterId}/{file.CurrentEpisodeId}, 기록 {file.Scenes?.Count ?? 0}개)");

        return true;
    }

    private async Task<int> RestoreBookmarksAsync(long userId)
    {
        ApiResult<List<BookmarkDetailDto>> list =
            await _session.CallAsync(
                token => _api.GetBookmarksAsync(userId, token));

        if (!list.Ok)
        {
            if (!list.NetworkError)
                Debug.LogWarning($"[복구] 즐겨찾기 목록 실패 — HTTP {list.Status} {list.ErrorCode}");

            return 0;
        }

        var file = new BookmarkFile();

        for (int i = 0; i < list.Body.Count; i++)
        {
            string id = list.Body[i].ClientBookmarkId;

            ApiResult<BookmarkDetailDto> single =
                await _session.CallAsync(
                    token => 
                        _api.GetBookmarkAsync(userId, id, token));

            if (!single.Ok || single.Body.Snapshot == null)
            {
                Debug.LogWarning($"[복구] 즐겨찾기 {id} 스냅샷 실패 — HTTP {single.Status} {single.ErrorCode}");
                continue;
            }

            Bookmark bookmark = 
                single.Body.Snapshot.
                    ToObject<Bookmark>(SaveJson.Serializer);

            if (bookmark == null)
                continue;

            bookmark.Id = id;
            bookmark.SyncedAtUtc = single.Body.UpdatedAt;
            bookmark.SyncError = null;

            file.Bookmarks.Add(bookmark);
        }

        if (file.Bookmarks.Count > 0)
            _localStore.SaveBookmarks(file);

        return file.Bookmarks.Count;
    }
}
