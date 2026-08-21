using System.Threading.Tasks;
using UnityEngine;

// 전제: 이 프로젝트의 롤백은 "복원"이 아니라 "다시 재생"임.
// 리플레이시 직전, 어느 노드에서 출발했는지와, 변수 스냅샷 챙김.

// <<jump>>와 <<detour>>의 차이 - detour와 콜스택
// jump = goto. 그 노드로 넘어가면 끝.
// detour = 함수 호출. VM이 "끝나면 여기로 돌아와라"라는 복귀 주소를 스택에 쌓고 들어감.

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

    // 새 장면 진입. 이 순간의 (시작에피소드와 variable변수)가 롤백 리플레이의 체크포인트가 된다.
    public async Task StartGameAsync(string nodeName)
    {
        await StopDialogueAsync();

        _sceneRootNodeName = nodeName;
        _variableCheckpoint.Capture();

        // 백로그는 세션 스코프 — 새 판 시작인 여기서만 비운다. 장면 체이닝이 생기면
        // "이어지는 장면 진입"은 비우지 않는 별도 경로로 갈라야 한다.
        _backlogRecorder.ClearBacklog();

        // 선택지 기록은 장면 스코프. 따라서 여기서만 리셋.
        // ReplayCurrentSceneAsync에서는 지우면 안 됨.
        _choiceHistory.ClearChoiceRecords();

        await RunSceneAsync(nodeName);
    }

    // 롤백 리플레이 - 변수를 장면 진입 시점으로 되돌리고 장면 시작에피소드에서 다시 재생.
    
    // Seek타겟 라인의 노드가 아니라 시작 노드에서 리플레이.
    // 타겟이 detour 안이더라도, 출발 노드명을 유지하기 위함.
    public async Task ReplayCurrentSceneAsync()
    {
        if (string.IsNullOrEmpty(_sceneRootNodeName))
        {
            Debug.LogWarning("[EpisodePlayer] Replay requested before any scene started. Ignored.");
            return;
        }

        await StopDialogueAsync();

        _variableCheckpoint.Restore();

        await RunSceneAsync(_sceneRootNodeName);
    }

    private async Task RunSceneAsync(string rootNodeName)
    {
        _vnScreenBindings.GoToPresentationView();

        _presentationStage.Clear();
        _presentationScopeSession.Start();

        await _nodeRunner.StartAsync(rootNodeName);
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