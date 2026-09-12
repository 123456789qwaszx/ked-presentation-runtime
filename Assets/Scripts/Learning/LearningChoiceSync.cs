using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// U5: LocalSaveFile의 확정 선택 경로를 서버의 현재 ChoiceRecord 집합으로 교체한다.
// append 이벤트가 아니라 최신 snapshot 하나만 의미가 있다.
public sealed class LearningChoiceSync
{
    private readonly AnalyticsApi _api;

    private LocalSaveFile _pendingSnapshot;
    private Task _syncTask;

    public LearningChoiceSync(string baseUrl)
    {
        _api = new AnalyticsApi(baseUrl);
    }

    public void OnSnapshotCommitted(LocalSaveFile snapshot)
    {
        if (snapshot == null)
            return;

        _pendingSnapshot = snapshot;
        TrySync();
    }

    public void OnServerBound()
    {
        TrySync();
    }

    private void TrySync()
    {
        if (_syncTask != null && !_syncTask.IsCompleted)
            return;

        LocalSaveFile snapshot = _pendingSnapshot;
        long? serverPlaythroughId = LearningSession.ServerPlaythroughId;

        if (snapshot == null || !serverPlaythroughId.HasValue)
            return;

        if (LearningSession.ClientPlaythroughId != snapshot.PlaythroughId)
            return;

        _pendingSnapshot = null;
        _syncTask = ReplaceAsync(serverPlaythroughId.Value, snapshot);
    }

    private async Task ReplaceAsync(
        long serverPlaythroughId,
        LocalSaveFile snapshot)
    {
        try
        {
            List<AnalyticsChoiceItemDto> choices = Flatten(snapshot);

            AnalyticsApiResult<bool> result =
                await _api.ReplaceChoicesAsync(
                    serverPlaythroughId,
                    choices);

            if (LearningSession.ClientPlaythroughId != snapshot.PlaythroughId
                || LearningSession.ServerPlaythroughId != serverPlaythroughId)
            {
                return;
            }

            if (result.NetworkError)
            {
                _pendingSnapshot = snapshot;
                Debug.LogWarning(
                    $"[U5 선택 동기화] 통신 실패 — 최신 snapshot 보류\n" +
                    result.ErrorMessage);
                return;
            }

            if (!result.IsSuccess)
            {
                Debug.LogWarning(
                    $"[U5 선택 동기화] HTTP {result.Status} " +
                    $"{result.ErrorCode}: {result.ErrorMessage}");
                return;
            }

            Debug.Log(
                $"[U5 선택 동기화] HTTP {result.Status}\n" +
                $"server playthroughId: {serverPlaythroughId}\n" +
                $"choiceCount: {choices.Count}");
        }
        catch (Exception error)
        {
            _pendingSnapshot = snapshot;
            Debug.LogWarning(
                $"[U5 선택 동기화] 처리 실패 — 최신 snapshot 보류\n" +
                error.Message);
        }
        finally
        {
            // 전송 중 더 최신 snapshot이 들어왔다면 이어서 그것만 보낸다.
            _syncTask = null;
            TrySync();
        }
    }

    private static List<AnalyticsChoiceItemDto> Flatten(
        LocalSaveFile snapshot)
    {
        var result = new List<AnalyticsChoiceItemDto>();

        if (snapshot.Scenes == null)
            return result;

        foreach (SceneRecord scene in snapshot.Scenes)
        {
            if (scene?.Path == null)
                continue;

            foreach (SavedChoice choice in scene.Path)
            {
                if (choice == null)
                    continue;

                result.Add(new AnalyticsChoiceItemDto
                {
                    EpisodeKey = choice.FromEpisodeId,
                    OptionIndex = choice.OptionIndex,
                });
            }
        }

        return result;
    }
}
