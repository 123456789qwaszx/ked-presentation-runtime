using System;
using System.Text;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Dto;
using UnityEngine;

// EpisodeFlow를 쥐는 유일한 소유자.
// 진행 층과 대사 층을 잇는 유일한 자리.
public sealed class ProgressionDriver
{
    private readonly EpisodePlayer _player;
    private readonly IChapterOptionsView _options;

    private EpisodeFlow _flow;
    private bool _stopRequested;
    private bool _firstEpisode;

    public bool IsRunning { get; private set; }

    public ProgressionDriver(EpisodePlayer player, IChapterOptionsView options)
    {
        _player = player;
        _options = options;
    }

    // 새 게임 또는 이어 하기.
    public async Task RunAsync(ScenarioProgression scenario, ProgressionState restored = null)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[진행] 이미 돌고 있다. 새 요청을 무시한다.");
            return;
        }

        IsRunning = true;
        _stopRequested = false;
        _firstEpisode = true;

        try
        {
            _flow = restored == null
                ? EpisodeFlow.Begin(scenario)
                : EpisodeFlow.Resume(scenario, restored);

            Debug.Log($"[진행] 시작 — {_flow}");

            await PumpAsync();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[진행] 취소됨.");
        }
        catch (Exception error)
        {
            Debug.LogError($"[진행] 멈췄다 — {_flow}\n{error}");
        }
        finally
        {
            IsRunning = false;
            _flow = null;
        }
    }

    // 타이틀로 나가기 등.
    public void RequestStop()
    {
        _stopRequested = true;
        _options.Cancel();
    }

    private async Task PumpAsync()
    {
        while (!_flow.IsFinished)
        {
            FlowRequest request = _flow.Pending;

            switch (request.Kind)
            {
                case FlowRequestKind.PlayDialogue:
                    if (!await PlayNodeAsync(request.NodeName, "대사"))
                        return;

                    _flow.DialogueCompleted();
                    break;

                case FlowRequestKind.PresentOptions:
                    int picked = await _options.ShowAsync(request.Options, request.HiddenCount);

                    if (_stopRequested)
                        return;

                    Debug.Log($"[진행] 골랐다 — {request.Options[picked].Option.ChoiceLabel}");
                    _flow.Choose(picked);
                    break;

                case FlowRequestKind.PlayVia:
                    // 연출도 Story 노드. 별도 경로를 만들지 않음.
                    if (!await PlayNodeAsync(request.NodeName, "연출"))
                        return;

                    _flow.ViaCompleted();
                    break;

                case FlowRequestKind.PersistSave:
                    // 지금은 로그만. "처리했다"지 "디스크에 썼다"가 아님.
                    Debug.Log(DescribeSave(request.Save));
                    _flow.SavePersisted();
                    break;

                default:
                    throw new InvalidOperationException($"모르는 요청: {request.Kind}");
            }
        }

        ShowEnding(_flow.Pending.Outcome);
    }

    private async Task<bool> PlayNodeAsync(string nodeName, string what)
    {
        Debug.Log($"[진행] {what} 시작 — \"{nodeName}\"");

        // 첫 진입 이후로는 백로그를 유지해야 하기 때문에 구분.
        if (_firstEpisode)
        {
            _firstEpisode = false;
            await _player.StartGameAsync(nodeName);
        }
        else
        {
            await _player.ContinueEpisodeAsync(nodeName);
        }

        Debug.Log($"[진행] {what} 끝 — \"{nodeName}\"");

        return !_stopRequested;
    }

    private static void ShowEnding(ScenarioAdvance outcome)
    {
        // 의도한 종착과 막다른 곳을 섞지 않는다. 화면에서 구별되어야 한다.
        if (outcome.Kind == ScenarioAdvanceKind.ScenarioEnded)
        {
            Debug.Log($"[진행] 엔딩 — \"{outcome.EndingKey}\"");
            return;
        }

        Debug.LogWarning(
            "[진행] 미완성 — 엔딩키가 없는 노드에서 멈췄다. " +
            "나가는 길이 하나도 없는데 엔딩도 아니다(작가가 아직 안 이은 자리).");
    }

    private static string DescribeSave(ProgressionSaveDto save)
    {
        var text = new StringBuilder();

        text.Append("[진행] 세이브 요청 — ")
            .Append(save.CurrentChapterId).Append('/').Append(save.CurrentEpisodeId)
            .Append("  스탯:");

        foreach (var stat in save.Stats)
            text.Append(' ').Append(stat.Key).Append('=').Append(stat.Value);

        if (save.EndingHistory != null && save.EndingHistory.Count > 0)
        {
            text.Append("  엔딩이력:");

            for (int i = 0; i < save.EndingHistory.Count; i++)
                text.Append(' ').Append(save.EndingHistory[i].EndingKey);
        }

        return text.ToString();
    }
}