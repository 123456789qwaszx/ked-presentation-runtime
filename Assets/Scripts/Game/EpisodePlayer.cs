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

    // 세션 경계.
    // 이 순간의 (시작에피소드와 variable변수)가 롤백 리플레이의 체크포인트가 된다.
    // 백로그를 포함해 전부 비움.
    public async Task StartGameAsync(string nodeName)
    {
        await EnterSceneAsync(nodeName, isNewSession: true);
    }

    // 이어지는 장면 진입 = 세션 안.
    // 백로그만 기록.
    //
    // 기록 하지 않는 것과 이유:
    
    // 롤백 포인트:
    // - 진행 층에 언커밋이 없음.
    // - 롤백이 에피소드 경계를 넘으면 되돌릴 수 없는 스탯 커밋 앞으로 돌아가게 된다.
    // - 에피소드 안으로만 제한.(차후 개선)
    
    // 변수 체크포인트:
    // - 롤백과 동일
    
    // 선택 기록:
    // - 롤백 리플레이용이고 롤백이 장면 스코프.
    
    // 무대:
    // - 이어야 하는 것이 맞지만 아직 못 잇는다.
    // - ReplayCurrentSceneAsync가 RunSceneAsync를 거치며 무대를 비우므로, 승계해 두면 롤백 한 번에 무대가 갈린다.
    // - 승계하려면 StageState를 굽고 되살리는 짝이 필요한데 지금은 굽는 쪽(StageStateCapture)만 있다.
    public async Task ContinueEpisodeAsync(string nodeName)
    {
        await EnterSceneAsync(nodeName, isNewSession: false);
    }

    private async Task EnterSceneAsync(string nodeName, bool isNewSession)
    {
        await StopDialogueAsync();

        _sceneRootNodeName = nodeName;
        _variableCheckpoint.Capture();

        // 백로그는 세션 스코프 - 새 판 시작에서만 비운다.
        if (isNewSession)
            _backlogRecorder.ClearBacklog();

        // 선택지 기록은 장면 스코프. 따라서 장면 진입마다 리셋.
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