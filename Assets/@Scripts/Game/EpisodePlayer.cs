using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class EpisodePlayer : MonoBehaviour
{
    private VnScreenBindings _vnScreenBindings;
    private RollbackHistory _nodeRollbackHistory;
    private IVNLineAborter _linePresentationAborter;
    private BacklogRecorder _backlogRecorder;
    private VNSideRunnerSyncHub _sideRunnerSyncHub;
    private PresentationShotResponseSystem _presentationResponseRig;
    private PresentationLaneScopeSession _presentationLaneScopeSession;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;
    [SerializeField] private DialogueRunner oneShotRunner;

    [Header("Entry Keys")]
    [SerializeField] private string yarnEntryKey;

    [Header("Debug Input")]
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;

    public string YarnEntryKey => yarnEntryKey;

    // 가장 최근 재생 요청의 세대.
    // await 는 취소가 안 되므로, 늦게 깨어난 옛 호출이 스스로 물러나는 기준으로 쓴다.
    private int _runGeneration;

    // 정리 구간(러너 정지 ~ 무대 재구성)이 겹치지 않게 막는다.
    // 노드가 재생되는 동안에는 false 다. 재생 중 로드/롤백까지 막으면 안 되기 때문이다.
    private bool _inTransition;

    public void Initialize(
        VnScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        PresentationShotResponseSystem presentationResponseRig,
        PresentationLaneScopeSession presentationLaneScopeSession)
    {
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _presentationResponseRig = presentationResponseRig;
        _presentationLaneScopeSession = presentationLaneScopeSession;
    }

    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
            StartGame(yarnEntryKey);
    }

    /// <summary>
    /// 디버그 키 / 타이틀 버튼 / 로드/롤백 seek 용 진입점.
    /// 완료를 기다리지 않는다. 기다려야 하는 쪽은 StartGameAsync 를 직접 await 한다.
    /// </summary>
    public void StartGame(string nodeName)
    {
        StartGameAsync(nodeName).Forget();
    }

    /// <summary>
    /// 세션을 초기화한 뒤 노드를 재생한다.
    /// 노드 안의 대화가 전부 끝나기 전까지 이 태스크는 완료되지 않는다.
    /// </summary>
    public async YarnTask StartGameAsync(string nodeName)
    {
        if (dialogueRunner == null)
            return;

        if (!dialogueRunner.Dialogue.NodeExists(nodeName))
        {
            Debug.LogWarning($"[EpisodePlayer] Node not found. node={nodeName}");
            return;
        }

        int generation = ++_runGeneration;

        // 앞선 호출이 정리 중이면 그게 끝날 때까지 기다린다.
        // 여기서 곧장 return 하면 앞선 호출도 세대 검사에서 물러나기 때문에
        // 아무것도 재생되지 않는 상태가 된다.
        while (_inTransition)
        {
            if (generation != _runGeneration)
                return;

            await NextFrameAsync();
        }

        if (generation != _runGeneration)
            return;

        _inTransition = true;

        try
        {
            await StopDialogueInternalAsync();

            if (generation != _runGeneration)
                return;

            _vnScreenBindings.GoToPresentationView();
            _presentationLaneScopeSession.ClearStage();
            _presentationLaneScopeSession.Start();
        }
        finally
        {
            // 재생 시작 전에 반드시 푼다.
            _inTransition = false;
        }

        if (generation != _runGeneration)
            return;

        // Yarn Spinner 3.1 기준, 이 await 는 '모든 dialogue presenter 의 시작 준비 완료'
        // 시점에 반환된다. 노드 종료가 아니므로 아래에서 따로 기다려야 한다.
        await dialogueRunner.StartDialogue(nodeName);

        await WaitForDialogueEndAsync(generation);
    }

    /// <summary>
    /// 대화가 끝날 때까지 프레임 단위로 기다린다.
    ///
    /// StartDialogue 를 먼저 await 했으므로 이 시점에 IsDialogueRunning 은 이미 true 다.
    /// 아직 시작되지 않아 즉시 빠져나가는 레이스가 없다.
    /// </summary>
    private async YarnTask WaitForDialogueEndAsync(int generation)
    {
        while (dialogueRunner.IsDialogueRunning)
        {
            // 선점당했으면 조용히 물러난다. 정리는 새 호출이 이미 맡았다.
            if (generation != _runGeneration)
                return;

            await NextFrameAsync();
        }
    }

    /// <summary>한 프레임 양보. Yarn 버전에 따라 API 가 다르면 이 한 줄만 교체한다.</summary>
    private static YarnTask NextFrameAsync() => YarnTask.Yield();

    private async YarnTask StopDialogueInternalAsync()
    {
        _nodeRollbackHistory.ClearRollbackPoints();
        _backlogRecorder.ClearBacklog();

        await StopYarnRunnersAsync();

        _linePresentationAborter?.AbortCurrentVnLine();

        _presentationResponseRig.Clear();
        _presentationLaneScopeSession.End();
    }

    private async YarnTask StopYarnRunnersAsync()
    {
        List<YarnTask> tasks = new List<YarnTask>();

        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
            tasks.Add(dialogueRunner.Stop());

        if (subPresentationRunner != null && subPresentationRunner.IsDialogueRunning)
            tasks.Add(subPresentationRunner.Stop());

        if (oneShotRunner != null && oneShotRunner.IsDialogueRunning)
            tasks.Add(oneShotRunner.Stop());

        if (tasks.Count > 0)
            await YarnTask.WhenAll(tasks);

        _sideRunnerSyncHub.ResetPresentationLane();
    }
}