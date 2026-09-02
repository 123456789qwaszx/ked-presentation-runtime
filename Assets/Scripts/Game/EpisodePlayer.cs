using System.Threading.Tasks;
using UnityEngine;

// 전제: 이 프로젝트의 롤백은 "복원"이 아니라 "다시 재생"임.
// 리플레이시 직전, 어느 노드에서 출발했는지와, 변수 스냅샷 챙김.

// <<jump>>와 <<detour>>의 차이 - detour와 콜스택
// jump = goto. 그 노드로 넘어가면 끝.
// detour = 함수 호출. VM이 "끝나면 여기로 돌아와라"라는 복귀 주소를 스택에 쌓고 들어감.

// 장면(Scene)의 연출 수명과 노드 실행을 맡음.
//
// - EnterSceneAsync   (장면 진입 묶음): 이전 대화 Stop, 체크포인트, 기록 리셋, 무대 기준선
//   PlayNodeAsync     (노드 하나 실행): 리플레이가 요청됐으면 그렇다고 알려준다
//   PrepareReplayAsync (리플레이 준비): 요청 쪽 Stop 대기, 변수 되감기, 무대 기준선
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

    // 롤백이 걸렸는가. 호출자의 장면 루프가 이 깃발을 보고 같은 자리에서 다시 튼다.
    private bool _replayRequested;

    // 요청 쪽의 멈춤. 그것이 끝난 뒤에 다시 틀어야 Stop과 Start가 안 겹친다.
    private Task _replayStop;

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

    // 노드 하나 실행.
    public async Task<NodePlayOutcome> PlayNodeAsync(string nodeName)
    {
        await _nodeRunner.StartAsync(nodeName);

        return _replayRequested
            ? NodePlayOutcome.ReplayRequested
            : NodePlayOutcome.Completed;
    }

    // 리플레이 준비.
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

    public string SceneRootNodeName => _sceneRootNodeName;

    // 롤백 리플레이 요청을 받고 준비. - 지금 노드를 멈추기만 함.
    // 되돌리고 다시 트는 것은 호출자의 장면 루프 책임.
    //
    // Seek 표적 라인의 노드가 아니라 장면 루트에서 리플레이.
    // 표적이 detour 안이더라도 출발 노드명을 유지하기 위함.
    public async Task RequestReplayAsync()
    {
        if (string.IsNullOrEmpty(_sceneRootNodeName))
        {
            Debug.LogWarning("[EpisodePlayer] Replay requested before any scene started. Ignored.");
            return;
        }

        if (!_nodeRunner.IsRunning)
        {
            // 노드가 이미 끝났다 - 되돌려 줄 루프가 없다. 진행 선택지가 떠 있는 자리가 여기.
            
            // 이걸 챕터단위로 롤백가능하게 하려면,
            // 확정된 상태를 되돌리고 저장 기록까지 다시 써야하는 보상/취소 연산.
            
            // 따라서 챕터 단위 롤백을 원할 시,
            // 방법1.챕터를 하나의 장면으로 통일하는 방식을 사용하는 방법.
            // 방법2. 커밋유예.
            Debug.LogWarning("[EpisodePlayer] 재생 중이 아니라 롤백을 무시한다.");
            return;
        }

        _replayRequested = true;

        Task stop = StopDialogueAsync();
        _replayStop = stop;

        await stop;
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
}