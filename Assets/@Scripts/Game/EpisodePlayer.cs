using System.Collections;
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

    //[Tooltip("Stop")]
    //[SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public string YarnEntryKey => yarnEntryKey;

    private int _runGeneration;

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
    
    // 디버그 키/외부 트리거용
    public void StartGame(string nodeName)
    {
        StartGameAsync(nodeName).Forget();
    }

    public async YarnTask StartGameAsync(string nodeName)
    {
        if (dialogueRunner == null)
            return;

        if (!dialogueRunner.Dialogue.NodeExists(nodeName))
        {
            Debug.LogWarning($"[EpisodePlayer] Node not found. node={nodeName}");
            return;
        }
        Debug.Log($"[EpisodePlayer] Running game {nodeName}");

        int generation = ++_runGeneration;

        await StopDialogueInternalAsync();

        // 대기 중에 다른 노드 요청이 들어왔으면 이번 호출은 포기한다.
        if (generation != _runGeneration)
            return;

        _vnScreenBindings.GoToPresentationView();
        _presentationLaneScopeSession.ClearStage();
        _presentationLaneScopeSession.Start();

        await dialogueRunner.StartDialogue(nodeName);
    }
    
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