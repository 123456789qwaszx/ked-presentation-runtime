using System.Threading.Tasks;
using UnityEngine;

// 재생 단위(노드 하나 / 챕터의 장면) 하나에 필요한 Yarn / Presentation 동작.
// 부르는 순서는 PlaybackUnit이 소유한다 — 두 호스트(노드 재생 / 진행)는 이 타입을 직접 부르지 않는다.
// 줄 시크(재개 지점까지 빨리 감기)도 여기서 건다. 언제 거는지는 PlaybackUnit이 정한다.
public sealed class ScenePresentation : IPlaybackStage
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly IPresentationView _presentationView;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    private readonly PresentationStage _presentationStage;
    private readonly PresentationScopeSession _presentationScope;
    private readonly ChoiceHistory _choiceHistory;
    private readonly EpisodeSkipController _episodeSkipController;
    private readonly VNLinePresentationState _lineState;
    private readonly YarnPlaybackDriver _playbackDriver;

    // 이번 재생 구간에 정지가 걸렸나. 정지 없이 노드가 돌아오면 끝까지 간 것이다.
    private bool _stopRequested;
    private string _currentNodeName;

    public ScenePresentation(
        IEpisodeNodeRunner nodeRunner,
        IPresentationView presentationView,
        RollbackHistory rollbackHistory,
        IVNLineAborter linePresentationAborter,
        PresentationShotResponseSystem shotResponseSystem,
        PresentationStage presentationStage,
        PresentationScopeSession presentationScope,
        ChoiceHistory choiceHistory,
        EpisodeSkipController episodeSkipController,
        VNLinePresentationState lineState,
        YarnPlaybackDriver playbackDriver)
    {
        _nodeRunner = nodeRunner;
        _presentationView = presentationView;
        _rollbackHistory = rollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _shotResponseSystem = shotResponseSystem;
        _presentationStage = presentationStage;
        _presentationScope = presentationScope;
        _choiceHistory = choiceHistory;
        _episodeSkipController = episodeSkipController;
        _lineState = lineState;
        _playbackDriver = playbackDriver;
    }

    public void BeginScene()
    {
        _stopRequested = false;

        // 앞 단위가 남긴 명령은 이 단위의 것이 아니다(정지가 이미 버렸어야 한다 — 빠진 길이 있어도 새지 않게).
        DiscardPendingCommands(warn: false);

        _choiceHistory.ClearChoiceRecords();

        // 앞 단위가 시크 도중에 끝났다면 그 표적은 이 단위의 것이 아니다.
        _lineState.ClearSeek();

        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();
    }
    
    public async Task PlayEpisodeAsync(string nodeName)
    {
        _currentNodeName = nodeName;

        try
        {
            await _nodeRunner.RunNodeAsync(nodeName);

            if (!_stopRequested)
                DiscardPendingCommands(warn: true);
        }
        finally
        {
            // 현재 Yarn node의 수명이 끝났으므로
            // one-shot Episode Skip도 여기서 끝난다.
            _episodeSkipController.Reset();
        }
    }
    
    // 단위의 Yarn 선택 기록을 되돌리고 표적 줄까지 시크한다. 로드 시크는 백로그를 다시 적는다(VNYarnLineBoundary).
    void IPlaybackStage.BeginResume(PresentationResumePoint resume)
    {
        _choiceHistory.RestoreChoices(resume.YarnChoices);

        LineTarget target = resume.Target;

        _lineState.BeginLoadSeek(
            target.NodeName,
            target.LineId,
            target.Occurrence);
    }

    bool IPlaybackStage.IsSeeking => _lineState.IsSeekingActive;

    void IPlaybackStage.AbandonSeek() => _lineState.ClearSeek();

    public void PrepareReplay()
    {
        // 같은 단위를 루트부터 다시 달린다 — 새 재생 구간이다. 남은 명령은 앞선 정지가 버렸다.
        _stopRequested = false;

        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _presentationScope.Start();
    }

    public async Task StopAsync()
    {
        _stopRequested = true;
        _episodeSkipController.Reset();

        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        // Yarn이 멈춘 뒤라 더 쌓이지 않는다.
        DiscardPendingCommands(warn: false);

        _linePresentationAborter.AbortCurrentVNLine();
        _rollbackHistory.ClearRollbackPoints();
        _shotResponseSystem.Clear();
        _presentationScope.End();
    }

    private void DiscardPendingCommands(bool warn)
    {
        if (_playbackDriver == null || !_playbackDriver.TryDiscardPending(out int count, out string summary))
            return;

        string node = string.IsNullOrEmpty(_currentNodeName) ? "(노드 없음)" : $"'{_currentNodeName}'";

        if (warn)
        {
            Debug.LogWarning(
                $"[연출 명령] {node} 재생이 끝났는데 다음 줄을 기다리던 명령 {count}개가 남아 실행하지 않고 버린다: {summary}. " +
                "명령은 다음 줄의 앞머리로 실행된다 — 노드를 줄로 끝낸다(연출만이면 #boxhide 줄).");
            return;
        }

        Debug.Log($"[연출 명령] {node} 정지 — 다음 줄을 기다리던 명령 {count}개를 버렸다: {summary}.");
    }
}
