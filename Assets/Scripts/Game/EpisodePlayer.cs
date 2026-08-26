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

    // 롤백이 걸렸는가. 장면 루프가 이 깃발을 보고 같은 자리에서 다시 튼다.
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

    // 세션 경계.
    // 이 순간의 (시작에피소드, variable변수)가 롤백 리플레이의 체크포인트 됨.
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
    // - 리플레이가 RunSceneAsync를 거치며 무대를 비우므로, 승계해 두면 롤백 한 번에 무대가 갈린다.
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
        // 아래 리플레이 루프에서는 지우면 안 됨 — 롤백이 그 기록으로 Yarn 선택을 복원한다.
        _choiceHistory.ClearChoiceRecords();

        // 롤백 리플레이를 이 안에서 접는다.
        //
        // 접지 않으면 리플레이의 Stop()이 DialogueTask를 완료시키고, 이 메서드를 기다리던
        // 진행 드라이버가 그것을 "대사가 끝났다"로 읽는다 — 화면은 되감겼는데 진행 선택지가
        // Yarn 옵션과 같은 박스에 뜬다. 롤백은 장면 스코프라 바깥이 보면 안 된다.
        while (true)
        {
            _replayRequested = false;

            await RunSceneAsync(_sceneRootNodeName);

            if (!_replayRequested)
                return;

            // 요청 쪽의 Stop이 끝나기를 기다린 뒤에 다시 튼다. 겹치면
            // CustomLinePresenter.OnDialogueCompleteAsync가 경고한 자리에 걸려
            // 새 대화의 첫 라인이 조용히 소비된다.
            if (_replayStop != null)
            {
                await _replayStop;
                _replayStop = null;
            }

            _variableCheckpoint.Restore();
        }
    }

    // 롤백 리플레이 요청 - 지금 장면을 멈추기만 한다.
    // 되돌리고 다시 트는 것은 EnterSceneAsync의 루프가 한다(바깥이 리플레이를 안 보도록).

    // Seek타겟 라인의 노드가 아니라 시작 노드에서 리플레이.
    // 타겟이 detour 안이더라도, 출발 노드명을 유지하기 위함.
    public async Task RequestReplayAsync()
    {
        if (string.IsNullOrEmpty(_sceneRootNodeName))
        {
            Debug.LogWarning("[EpisodePlayer] Replay requested before any scene started. Ignored.");
            return;
        }

        if (!_nodeRunner.IsRunning)
        {
            // 장면이 이미 끝났다 - 되돌려 줄 루프가 없다. 진행 선택지가 떠 있는 자리가
            // 여기이고, 그것을 물리려면 커밋된 스탯을 되감아야 해서 지금은 못 한다.
            Debug.LogWarning("[EpisodePlayer] 재생 중이 아니라 롤백을 무시한다.");
            return;
        }

        _replayRequested = true;

        Task stop = StopDialogueAsync();
        _replayStop = stop;

        await stop;
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