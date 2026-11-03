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
    private readonly ILineAborter _linePresentationAborter;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    // 화면 효과(vignette·noise 의도, flash 이벤트)와 무대 마스크도 샷처럼 정지에서 기준으로 — 리플레이가 다시 건다.
    private readonly ScreenEffectRig _screenEffects;
    private readonly IStageMaskProvider _stageMasks;
    // 오디오 — BGM은 장면을 넘어 이어 가는 의도, 보이스·효과음은 이벤트(정지에서 끊는다).
    private readonly AudioSystem _audio;
    private readonly PresentationStage _presentationStage;
    // 대사창 레이아웃(surface_layout)도 무대처럼 단위마다 처음부터 — 리플레이가 다시 건다.
    private readonly DialogueSurfaceState _dialogueSurfaceState;
    private readonly PresentationScopeSession _presentationScope;
    private readonly ChoiceHistory _choiceHistory;
    private readonly EpisodeSkipController _episodeSkipController;
    private readonly SeekState _seekState;
    private readonly YarnPlaybackDriver _playbackDriver;

    // 이번 재생 구간에 정지가 걸렸나. 정지 없이 노드가 돌아오면 끝까지 간 것이다.
    private bool _stopRequested;
    private string _currentNodeName;

    // 이 단위가 열릴 때 흐르던 곡 — 리플레이(시크)가 BGM 의도를 여기서부터 다시 쌓는다.
    private AudioClip _sceneEntryBgm;

    public ScenePresentation(
        IEpisodeNodeRunner nodeRunner,
        IPresentationView presentationView,
        RollbackHistory rollbackHistory,
        ILineAborter linePresentationAborter,
        PresentationShotResponseSystem shotResponseSystem,
        ScreenEffectRig screenEffects,
        IStageMaskProvider stageMasks,
        AudioSystem audio,
        PresentationStage presentationStage,
        DialogueSurfaceState dialogueSurfaceState,
        PresentationScopeSession presentationScope,
        ChoiceHistory choiceHistory,
        EpisodeSkipController episodeSkipController,
        SeekState seekState,
        YarnPlaybackDriver playbackDriver)
    {
        _nodeRunner = nodeRunner;
        _presentationView = presentationView;
        _rollbackHistory = rollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _shotResponseSystem = shotResponseSystem;
        _screenEffects = screenEffects;
        _stageMasks = stageMasks;
        _audio = audio;
        _presentationStage = presentationStage;
        _dialogueSurfaceState = dialogueSurfaceState;
        _presentationScope = presentationScope;
        _choiceHistory = choiceHistory;
        _episodeSkipController = episodeSkipController;
        _seekState = seekState;
        _playbackDriver = playbackDriver;

        // 시크가 끝나면(도착·포기·불일치·새 장면) 쌓아 둔 BGM 의도를 실제와 맞춘다.
        _seekState.Ended += ReleaseBgm;
    }

    public void BeginScene()
    {
        _stopRequested = false;
        
        // 앞 단위가 남긴 명령은 이 단위의 것이 아님.
        DiscardPendingCommands(warn: false); 
        
        _choiceHistory.ClearChoiceRecords();
        
        // 앞 단위가 시크 도중에 끝났다면 그 표적은 이 단위의 것이 아님.
        _seekState.End(SeekEndReason.SceneReset); 

        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _dialogueSurfaceState.Reset();
        _presentationScope.Start();

        // BGM은 앞 장면에서 이어 간다 — 그 곡이 이 단위의 출발점이다.
        _sceneEntryBgm = _audio != null ? _audio.Bgm.Intent : null;
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
    
    // 단위의 Yarn 선택 기록을 되돌리고 표적 줄까지 시크한다. 로드 시크는 백로그를 다시 적는다(LineRecordPolicy).
    void IPlaybackStage.BeginResume(PresentationResumePoint resume)
    {
        _choiceHistory.RestoreChoices(resume.YarnChoices);

        LineTarget target = resume.Target;

        _seekState.Begin(new SeekTarget(
            SeekKind.Load,
            target.LineId,
            target.Occurrence));

        HoldBgm();
    }

    bool IPlaybackStage.IsSeeking => _seekState.IsSeeking;

    void IPlaybackStage.AbandonSeek() => _seekState.End(SeekEndReason.Abandoned);

    public void PrepareReplay()
    {
        // 같은 단위를 루트부터 다시 달린다 — 새 재생 구간이다. 남은 명령은 앞선 정지가 버렸다.
        _stopRequested = false;

        _presentationView.GoToPresentationView();
        _presentationStage.Clear();
        _dialogueSurfaceState.Reset();
        _presentationScope.Start();

        // 되감기 시크는 이미 걸려 있다(PlaybackFeatureController) — 표적까지 BGM은 의도만 쌓는다.
        if (_seekState.IsSeeking)
            HoldBgm();
    }

    public async Task StopAsync()
    {
        _stopRequested = true;
        _episodeSkipController.Reset();

        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        // Yarn이 멈춘 뒤라 더 쌓이지 않는다.
        DiscardPendingCommands(warn: false);

        _linePresentationAborter.AbortCurrentLine();
        _rollbackHistory.ClearRollbackPoints();
        _shotResponseSystem.Clear();

        if (_screenEffects != null)
            _screenEffects.ResetToBaselineImmediate();

        _stageMasks.ResetAllToBaseline();

        if (_audio != null)
        {
            _audio.Voice.Stop();
            _audio.Sfx.StopAll();
        }

        _presentationScope.End();
    }

    private void HoldBgm()
    {
        if (_audio != null)
            _audio.Bgm.Hold(_sceneEntryBgm);
    }

    private void ReleaseBgm()
    {
        if (_audio != null)
            _audio.Bgm.Release();
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
