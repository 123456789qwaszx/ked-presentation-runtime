using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 새 기기 — 로컬에 회차가 하나도 없을 때 서버 사본으로 재구성한다.
//
// 회차: 목록 GET → 슬롯 1 스냅샷 GET(우리가 올린 LocalSaveFile 그대로) + 선택 이력(다음 seq를 위해).
// 큐 파일은 서버가 아는 것만(서버 id·revision·다음 seq). 활성은 lastSavedAt 최신 — 서버에 "활성"은 없다.
// 즐겨찾기: 목록(메타) → 단건 GET(스냅샷 = Bookmark 통째).
//
// 계정(account.json)이 없으면 아무것도 하지 않는다 — 게스트 계정은 그 파일이 신원이다.
public sealed class ServerRestore
{
    private readonly ServerApi _api;
    private readonly GuestSession _session;
    private readonly ISaveStore _localStore;
    private readonly int _slotNo;

    public ServerRestore(ServerApi api, GuestSession session, ISaveStore localStore, int slotNo)
    {
        _api = api;
        _session = session;
        _localStore = localStore;
        _slotNo = slotNo;
    }

    // 하나라도 재구성했으면 true.
    public async Task<bool> RestoreAsync()
    {
        if (_session.UserId == null)
            return false;

        long userId = _session.UserId.Value;

        ApiResult<List<PlaythroughSummaryDto>> list =
            await _session.CallAsync(token => _api.GetPlaythroughsAsync(userId, token));

        if (!list.Ok)
        {
            if (!list.NetworkError)
                Debug.LogWarning($"[복구] 회차 목록 실패 — HTTP {list.Status} {list.ErrorCode}");

            return false;
        }

        // 클라 id와 슬롯이 있는 회차만. lastSavedAt 오름차순으로 저장하면 마지막 것이 활성으로 남는다.
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
            (restored > 0 ? $" — 활성 {_localStore.ActiveId}" : string.Empty));

        return restored > 0 || bookmarks > 0;
    }

    private async Task<bool> RestorePlaythroughAsync(PlaythroughSummaryDto summary)
    {
        ApiResult<SaveSlotDetailDto> detail =
            await _session.CallAsync(token => _api.GetSaveAsync(summary.Id, _slotNo, token));

        if (!detail.Ok || detail.Body.Snapshot == null)
        {
            Debug.LogWarning($"[복구] 회차 {summary.ClientPlaythroughId} 스냅샷 실패 — HTTP {detail.Status} {detail.ErrorCode}");
            return false;
        }

        LocalSaveFile file = detail.Body.Snapshot.ToObject<LocalSaveFile>(SaveJson.Serializer);

        if (file == null)
            return false;

        // 다음 seq는 서버 이력의 마지막 + 1. 이력을 못 읽으면 seq가 겹칠 수 있어 이 회차는 건너뛴다.
        ApiResult<List<ChoiceHistoryItemDto>> choices =
            await _session.CallAsync(token => _api.GetChoicesAsync(summary.Id, _slotNo, token));

        if (!choices.Ok)
        {
            Debug.LogWarning($"[복구] 회차 {summary.ClientPlaythroughId} 선택 이력 실패 — HTTP {choices.Status} {choices.ErrorCode}");
            return false;
        }

        int lastSeq = 0;

        for (int i = 0; i < choices.Body.Count; i++)
            lastSeq = Math.Max(lastSeq, choices.Body[i].Seq);

        file.PlaythroughId = summary.ClientPlaythroughId;
        file.SlotNo = _slotNo;

        _localStore.Save(file);

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
            await _session.CallAsync(token => _api.GetBookmarksAsync(userId, token));

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
                await _session.CallAsync(token => _api.GetBookmarkAsync(userId, id, token));

            if (!single.Ok || single.Body.Snapshot == null)
            {
                Debug.LogWarning($"[복구] 즐겨찾기 {id} 스냅샷 실패 — HTTP {single.Status} {single.ErrorCode}");
                continue;
            }

            Bookmark bookmark = single.Body.Snapshot.ToObject<Bookmark>(SaveJson.Serializer);

            if (bookmark == null)
                continue;

            bookmark.Id = id;
            bookmark.SyncedAtUtc = single.Body.UpdatedAt;
            bookmark.SyncError = null;

            file.Items.Add(bookmark);
        }

        if (file.Items.Count > 0)
            _localStore.SaveBookmarks(file);

        return file.Items.Count;
    }
}
