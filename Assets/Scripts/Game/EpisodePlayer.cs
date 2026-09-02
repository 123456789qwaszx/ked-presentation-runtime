using System;
using System.Threading.Tasks;
using UnityEngine;

// 장면(Scene)의 연출 수명과 노드 실행.
//
// - EnterSceneAsync    (장면 진입 묶음): 이전 대화 Stop, 체크포인트, 기록 리셋, 연출 무대 기준선
//   PlayNodeAsync      (노드 하나 실행): 리플레이 요청 전달.
//   PrepareReplayAsync (리플레이 준비) : 요청 쪽 Stop 대기, 변수 되감기, 연출 무대 기준선
//
// StartGameAsync/ContinueEpisodeAsync는 디버깅용.
// 진행 층 없이 노드만 틀어 보는 경로(RunYarn, RunEpisodeChain)가 사용.
public enum NodePlayOutcome
{
    Completed = 0,       // 노드가 끝까지 갔다(또는 리플레이가 아닌 이유로 멈춤).
    ReplayRequested = 1, // 롤백 리플레이가 요청돼 멈춤. PrepareReplayAsync 뒤 장면 루트부터 다시.
}

public sealed class EpisodePlayer
{
    private readonly IEpisodeNodeRunner _nodeRunner;
    private readonly VNScreenBindings _vnScreenBindings;
    private readonly RollbackHistory _nodeRollbackHistory;
    private readonly IVNLineAborter _linePresentationAborter;
    private readonly BacklogRecorder _backlogRecorder;
    private readonly PresentationShotResponseSystem _shotResponseSystem;
    private readonly PresentationStage _presentationStage;
    private readonly PresentationScopeSession _presentationScopeSession;
    private readonly YarnVariableCheckpoint _variableCheckpoint;
    private readonly ChoiceHistory _choiceHistory;

    private string _sceneRootNodeName;
    
    private bool _replayRequested; // 롤백이 걸렸는가. 호출자의 장면 루프가 이 깃발을 보고 같은 자리에서 다시 튼다.
    private Task _replayStop;// 요청 쪽의 멈춤. 그것이 끝난 뒤에 다시 틀어야 Stop과 Start가 안 겹친다.
    
    // 리플레이가 요청됐는데 아직 되감지 않았다.
    // 노드 밖(선택지 대기)에서 취소가 왔을 때 리플레이 때문인지 가리는 용도.
    public bool IsReplayPending => _replayRequested;
    public string SceneRootNodeName => _sceneRootNodeName;
    
    // 노드가 돌지 않을 때 리플레이가 요청됐다 — 장면 루프가 기다리는 것(진행 선택지)을 접어야 한다.
    // 노드 안이면 Stop이 그 일을 하므로 안 울린다.
    public event Action ReplayRequestedWhileIdle;

    public EpisodePlayer(
        IEpisodeNodeRunner nodeRunner,
        VNScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        PresentationShotResponseSystem presentationResponseRig,
        PresentationStage presentationStage,
        PresentationScopeSession presentationScopeSession,
        YarnVariableCheckpoint variableCheckpoint,
        ChoiceHistory choiceHistory)
    {
        _nodeRunner = nodeRunner;
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _shotResponseSystem = presentationResponseRig;
        _presentationStage = presentationStage;
        _presentationScopeSession = presentationScopeSession;
        _variableCheckpoint = variableCheckpoint;
        _choiceHistory = choiceHistory;
    }
    
    // 장면 진입.
    // 이 순간의 (루트 노드, Yarn 변수)가 롤백 리플레이의 체크포인트가 된다.
    //
    // 백로그는 세션(회차) 스코프 — 새 판 시작에서만 비운다.
    // 선택지 기록은 장면 스코프 — 진입마다 리셋. 리플레이에서는 지우면 안 된다(롤백이 그
    // 기록으로 Yarn 선택을 복원한다).
    //
    // 무대는 이어야 하는 것이 맞지만 아직 못 잇는다 — 승계하려면 StageState를 굽고 되살리는
    // 짝이 필요한데 지금은 굽는 쪽(StageStateCapture)만 있다. v1은 장면 시작 = 무대 클리어.
    public async Task EnterSceneAsync(string rootNodeName, bool isNewSession)
    {
        await StopDialogueAsync();

        _sceneRootNodeName = rootNodeName;
        _variableCheckpoint.Capture();

        if (isNewSession)
            _backlogRecorder.ClearBacklog();

        _choiceHistory.ClearChoiceRecords();

        _replayRequested = false;

        BeginSceneRun();
    }

    public async Task<NodePlayOutcome> PlayNodeAsync(string nodeName)
    {
        if (_replayRequested)
            return NodePlayOutcome.ReplayRequested;

        await _nodeRunner.StartAsync(nodeName);

        return _replayRequested
            ? NodePlayOutcome.ReplayRequested
            : NodePlayOutcome.Completed;
    }

    public async Task PrepareReplayAsync()
    {
        // 요청 쪽의 Stop이 끝나기를 기다린 뒤에 다시 틈.
        // 겹치면 새 대화의 첫 라인이 조용히 소비되는 버그 발생.
        if (_replayStop != null)
        {
            await _replayStop;
            _replayStop = null;
        }

        _replayRequested = false;

        _variableCheckpoint.Restore();

        BeginSceneRun();
    }
    
    // 롤백 리플레이 요청을 받고 준비. - 지금 노드를 멈추기만 함.
    // 되돌리고 다시 트는 것은 호출자의 장면 루프 책임.
    //
    // Seek 표적 라인의 노드가 아니라 장면 루트에서 리플레이.
    // 표적이 detour 안이더라도 출발 노드명을 유지하기 위함.
    public async Task RequestReplayAsync()
    {
        // 노드 밖(진행 선택지 대기)에서도 받는다. 장면 안이면 되돌릴 루프가 있고,
        // 장면 안의 선택은 전부 미확정이라 어디로든 물릴 수 있다.
        bool wasRunning = _nodeRunner.IsRunning;

        _replayRequested = true;

        // 노드 밖에서도 같은 정리 — 라인 중단·롤백 포인트·샷 응답·스코프 종료.
        // 러너 Stop만 건너뛴다(돌고 있지 않으니까).
        Task stop = StopDialogueAsync();
        _replayStop = stop;

        // 노드 밖이면 장면 루프가 선택지를 기다리는 중 — 접으라고 알린다.
        if (!wasRunning)
            ReplayRequestedWhileIdle?.Invoke();

        await stop;
    }
    
    // 무대 기준선. 장면 진입과 리플레이 직전, 두 자리에서만.
    private void BeginSceneRun()
    {
        _vnScreenBindings.GoToPresentationView();

        _presentationStage.Clear();
        _presentationScopeSession.Start();
    }

    private async Task StopDialogueAsync()
    {
        if (_nodeRunner.IsRunning)
            await _nodeRunner.StopAsync();

        _linePresentationAborter.AbortCurrentVNLine();

        _nodeRollbackHistory.ClearRollbackPoints();

        _shotResponseSystem.Clear();
        _presentationScopeSession.End();
    }

    
    // 디버그 경로
    public Task StartGameAsync(string nodeName) => PlaySingleNodeSceneAsync(nodeName, isNewSession: true);

    public Task ContinueEpisodeAsync(string nodeName) => PlaySingleNodeSceneAsync(nodeName, isNewSession: false);

    private async Task PlaySingleNodeSceneAsync(string nodeName, bool isNewSession)
    {
        await EnterSceneAsync(nodeName, isNewSession);

        while (await PlayNodeAsync(_sceneRootNodeName) == NodePlayOutcome.ReplayRequested)
            await PrepareReplayAsync();
    }
}