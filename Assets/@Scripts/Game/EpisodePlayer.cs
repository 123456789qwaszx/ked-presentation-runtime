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

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;
    [SerializeField] private DialogueRunner oneShotRunner;

    [Header("Presentation")]
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;

    [Header("Entry Keys")]
    [SerializeField] private string yarnEntryKey;

    [Header("Debug Input")]
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;

    //[Tooltip("Stop")]
    //[SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public string YarnEntryKey => yarnEntryKey;

    private bool _isRestarting;
    private int _restartGeneration;

    public void Initialize(
        VnScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        VNSideRunnerSyncHub sideRunnerSyncHub,
        PresentationShotResponseSystem presentationResponseRig)
    {
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _sideRunnerSyncHub = sideRunnerSyncHub;
        _presentationResponseRig = presentationResponseRig;
    }

    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
            StartGame(yarnEntryKey);
    }

    private Coroutine _restartCoroutine;
    
    public void StartGame(string nodeName)
    {
        if (_restartCoroutine != null)
            StopCoroutine(_restartCoroutine);

        _restartCoroutine = StartCoroutine(RestartInternalCoroutine(nodeName));
    }

    private IEnumerator RestartInternalCoroutine(string nodeName)
    {
        if (_isRestarting)
            yield break;

        _isRestarting = true;
        int generation = ++_restartGeneration;

        YarnTask stopTask = StopDialogueInternalAsync();
        yield return WaitForYarnTask(stopTask);

        if (generation != _restartGeneration)
        {
            if (generation == _restartGeneration)
                _isRestarting = false;

            _restartCoroutine = null;
            yield break;
        }

        _vnScreenBindings.GoToPresentationView();
        presentationRouteEntry.RestartRoute();

        YarnTask startTask = dialogueRunner.StartDialogue(nodeName);
        yield return WaitForYarnTask(startTask);

        if (generation == _restartGeneration)
            _isRestarting = false;

        _restartCoroutine = null;
    }
    private static IEnumerator WaitForYarnTask(YarnTask task)
    {
        var awaiter = task.GetAwaiter();

        while (!awaiter.IsCompleted)
            yield return null;

        awaiter.GetResult();
    }
    

    private async YarnTask StopDialogueInternalAsync()
    {
        _nodeRollbackHistory.ClearRollbackPoints();
        _backlogRecorder.ClearBacklog();
        await StopYarnRunnersAsync();
        _linePresentationAborter?.AbortCurrentVnLine();
        
        _presentationResponseRig.Clear();
        presentationRouteEntry.EndRouteNow();
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