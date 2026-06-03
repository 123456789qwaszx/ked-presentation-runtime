using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class EpisodePlayer : MonoBehaviour
{
    private VnScreenBindings _vnScreenBindings;
    private RollbackController _nodeRollbackHistory;
    private IVNLineAborter _linePresentationAborter;
    private BacklogRecorder _backlogRecorder;
    private ChoiceHistory _choiceHistory;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;

    [Header("Presentation")]
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;
    [SerializeField] private PresentationResponseRig presentationResponseRig;

    [Header("Entry Keys")]
    [SerializeField] private string yarnEntryKey;
    [SerializeField] private string presentationEntryKey;

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
        RollbackController nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        ChoiceHistory choiceHistory)
    {
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _choiceHistory = choiceHistory;
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
        presentationRouteEntry.RestartRoute(presentationEntryKey);

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
        presentationRouteEntry.EndRouteNow();
        ResetVisualState();
    }

    private async YarnTask StopYarnRunnersAsync()
    {
        List<YarnTask> tasks = new List<YarnTask>();
        //
        // if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        //     tasks.Add(dialogueRunner.Stop());
        //
        // if (subPresentationRunner != null && subPresentationRunner.IsDialogueRunning)
        //     tasks.Add(subPresentationRunner.Stop());
        //
        // if (tasks.Count <= 0)
        //     return;
        
        await YarnTask.WhenAll(tasks);
    }

    private void ResetVisualState()
    {
        presentationResponseRig.Clear();
        
        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        SlantedMaskGraphic mask = provider.SlantedMaskEdgeGraphic.GetComponent<SlantedMaskGraphic>();
        mask?.ResetToHiddenOffset();
    }
}