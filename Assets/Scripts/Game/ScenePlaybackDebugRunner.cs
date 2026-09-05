using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Progression 계층을 거치지 않고 Yarn node를 직접 실행하는 디버그 경로.
public sealed class ScenePlaybackDebugRunner
{
    private readonly ScenePlaybackSession _playback;
    private readonly BacklogRecorder _backlog;

    public bool IsRunning { get; private set; }

    public ScenePlaybackDebugRunner(
        ScenePlaybackSession playback,
        BacklogRecorder backlog)
    {
        _playback = playback;
        _backlog = backlog;
    }

    public async Task RunSingleNodeAsync(string nodeName)
    {
        if (IsRunning)
            return;

        IsRunning = true;

        try
        {
            _backlog.ClearBacklog();

            await RunNodeAsync(nodeName);
        }
        finally
        {
            IsRunning = false;
        }
    }

    public async Task RunNodeChainAsync(IReadOnlyList<string> nodeNames)
    {
        if (IsRunning)
            return;

        IsRunning = true;

        try
        {
            _backlog.ClearBacklog();

            for (int i = 0; i < nodeNames.Count; i++)
            {
                string nodeName = nodeNames[i];

                Debug.Log(
                    $"[연결] {i + 1}/{nodeNames.Count} 시작 — \"{nodeName}\"");

                await RunNodeAsync(nodeName);

                Debug.Log(
                    $"[연결] {i + 1}/{nodeNames.Count} 끝 — \"{nodeName}\"");
            }

            Debug.Log("[연결] 사슬 끝.");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RunNodeAsync(string nodeName)
    {
        await _playback.BeginSceneAsync();

        _backlog.MarkSceneStart();

        while (await _playback.PlayNodeAsync(nodeName)
               == NodePlayOutcome.ReplayRequested)
        {
            await _playback.PrepareReplayAsync();
        }
    }
}