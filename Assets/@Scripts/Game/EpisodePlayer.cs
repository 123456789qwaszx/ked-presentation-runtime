using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class EpisodePlayer : MonoBehaviour
{
    private VnScreenBindings _vnScreenBindings;
    private RollbackHistory _nodeRollbackHistory;
    private IVNLineAborter _linePresentationAborter;
    private BacklogRecorder _backlogRecorder;
    private VNSaveLoadSystem _vnSaveLoadSystem;

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

    [Tooltip("Stop")]
    [SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public string YarnEntryKey => yarnEntryKey;

    private bool _isRestarting;
    private int _restartGeneration;

    public void Initialize(
        VnScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        IVNLineAborter linePresentationAborter,
        BacklogRecorder backlogRecorder,
        VNSaveLoadSystem saveLoadSystem)
    {
        _vnScreenBindings = vnScreenBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
        _vnSaveLoadSystem = saveLoadSystem;
    }

    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
            RestartGame(yarnEntryKey);
        
        if (Input.GetKeyDown(stopKey))
            StopDialogue();
    }

    public void StartGame(string nodeName)
    {
        RestartGame(nodeName);
    }

    public YarnTask StartGameAsync(string nodeName)
    {
        return RestartGameAsync(nodeName);
    }

    public void LoadGame(string nodeName)
    {
        RestartGame(nodeName);
    }

    public YarnTask LoadGameAsync(string nodeName)
    {
        return RestartGameAsync(nodeName);
    }

    public void RestartGame(string nodeName)
    {
        RestartGameAsync(nodeName).Forget();
    }

    public YarnTask RestartGameAsync(string nodeName)
    {
        return RestartInternalAsync(
            nodeName,
            clearHistory: true,
            clearBacklog: true,
            label: "RestartGame");
    }

    public void RestartForRollback(string nodeName)
    {
        RestartForRollbackAsync(nodeName).Forget();
    }

    public YarnTask RestartForRollbackAsync(string nodeName)
    {
        return RestartInternalAsync(
            nodeName,
            clearHistory: false,
            clearBacklog: true,
            label: "RestartForRollback");
    }

    public void StopDialogue()
    {
        StopDialogueAsync().Forget();
    }

    public YarnTask StopDialogueAsync()
    {
        return StopDialogueInternalAsync(
            clearHistory: true,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationSession: true,
            resetVisualState: true,
            label: "StopDialogue");
    }

    private async YarnTask RestartInternalAsync(
        string nodeName,
        bool clearHistory,
        bool clearBacklog,
        string label)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning($"[EpisodePlayer] {label} ignored. nodeName is null or empty.", this);
            return;
        }

        if (_isRestarting)
        {
            Debug.LogWarning($"[EpisodePlayer] {label} ignored. Restart is already in progress. node={nodeName}, frame={Time.frameCount}", this);
            return;
        }

        _isRestarting = true;
        int generation = ++_restartGeneration;

        try
        {
            Debug.Log($"[EpisodePlayer] {label} begin. gen={generation}, node={nodeName}, frame={Time.frameCount}", this);

            await StopDialogueInternalAsync(
                clearHistory: clearHistory,
                clearBacklog: clearBacklog,
                stopYarnRunner: true,
                endPresentationSession: true,
                resetVisualState: true,
                label: label);

            Debug.Log($"[EpisodePlayer] {label} stop complete. gen={generation}, node={nodeName}, frame={Time.frameCount}", this);

            if (generation != _restartGeneration)
            {
                Debug.LogWarning($"[EpisodePlayer] {label} canceled by newer restart. gen={generation}, current={_restartGeneration}, frame={Time.frameCount}", this);
                return;
            }

            _vnScreenBindings.GoToPresentationView();

            Debug.Log($"[EpisodePlayer] {label} presentation route restart. gen={generation}, route={presentationEntryKey}, frame={Time.frameCount}", this);
            presentationRouteEntry.RestartRoute(presentationEntryKey);

            Debug.Log($"[EpisodePlayer] {label} StartDialogue begin. gen={generation}, node={nodeName}, frame={Time.frameCount}", this);
            await dialogueRunner.StartDialogue(nodeName);
            Debug.Log($"[EpisodePlayer] {label} StartDialogue returned. gen={generation}, node={nodeName}, frame={Time.frameCount}", this);
        }
        finally
        {
            if (generation == _restartGeneration)
                _isRestarting = false;

            Debug.Log($"[EpisodePlayer] {label} end. gen={generation}, node={nodeName}, restarting={_isRestarting}, frame={Time.frameCount}", this);
        }
    }

    private async YarnTask StopDialogueInternalAsync(
        bool clearHistory,
        bool clearBacklog,
        bool stopYarnRunner,
        bool endPresentationSession,
        bool resetVisualState,
        string label)
    {
        Debug.Log($"[EpisodePlayer] StopDialogueInternal begin. label={label}, frame={Time.frameCount}", this);

        if (clearHistory)
        {
            Debug.Log($"[EpisodePlayer] Clear rollback history. label={label}, frame={Time.frameCount}", this);
            _nodeRollbackHistory.ClearRollbackHistory();
        }

        if (clearBacklog)
        {
            Debug.Log($"[EpisodePlayer] Clear backlog. label={label}, frame={Time.frameCount}", this);
            _backlogRecorder.ClearBacklog();
        }

        if (stopYarnRunner)
        {
            Debug.Log($"[EpisodePlayer] StopYarnRunnersAsync begin. label={label}, frame={Time.frameCount}", this);
            //await StopYarnRunnersAsync(label);
            Debug.Log($"[EpisodePlayer] StopYarnRunnersAsync complete. label={label}, frame={Time.frameCount}", this);
        }

        Debug.Log($"[EpisodePlayer] Abort current VN line. label={label}, frame={Time.frameCount}", this);
        _linePresentationAborter?.AbortCurrentVnLine();

        if (endPresentationSession)
        {
            Debug.Log($"[EpisodePlayer] End presentation route. label={label}, frame={Time.frameCount}", this);
            EndPresentationRouteNow();
        }

        if (resetVisualState)
        {
            Debug.Log($"[EpisodePlayer] Reset visual state. label={label}, frame={Time.frameCount}", this);
            ResetVisualState();
        }

        Debug.Log($"[EpisodePlayer] StopDialogueInternal end. label={label}, frame={Time.frameCount}", this);
    }

    private async YarnTask StopYarnRunnersAsync(string label)
    {
        List<YarnTask> tasks = new List<YarnTask>();

        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"[EpisodePlayer] main stop requested. label={label}, frame={Time.frameCount}", this);
            tasks.Add(dialogueRunner.Stop());
        }
        else
        {
            Debug.Log($"[EpisodePlayer] main stop skipped. label={label}, running={(dialogueRunner != null && dialogueRunner.IsDialogueRunning)}, frame={Time.frameCount}", this);
        }

        if (subPresentationRunner != null && subPresentationRunner.IsDialogueRunning)
        {
            Debug.Log($"[EpisodePlayer] sub stop requested. label={label}, frame={Time.frameCount}", this);
            tasks.Add(subPresentationRunner.Stop());
        }
        else
        {
            Debug.Log($"[EpisodePlayer] sub stop skipped. label={label}, running={(subPresentationRunner != null && subPresentationRunner.IsDialogueRunning)}, frame={Time.frameCount}", this);
        }

        if (tasks.Count <= 0)
        {
            Debug.Log($"[EpisodePlayer] no runner stop needed. label={label}, frame={Time.frameCount}", this);
            return;
        }

        await YarnTask.WhenAll(tasks);

        Debug.Log($"[EpisodePlayer] all runner stops completed. label={label}, frame={Time.frameCount}", this);
    }

    private void EndPresentationRouteNow()
    {
        if (presentationRouteEntry == null)
            return;

        presentationRouteEntry.EndRouteNow();
    }

    private void ResetVisualState()
    {
        if (presentationResponseRig != null)
            presentationResponseRig.Clear();

        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        if (provider == null)
            return;

        SlantedMaskGraphic mask = provider.SlantedMaskEdgeGraphic.GetComponent<SlantedMaskGraphic>();
        mask?.ResetToHiddenOffset();
    }
}