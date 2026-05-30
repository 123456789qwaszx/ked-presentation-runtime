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

    public void LoadGame(string nodeName)
    {
        RestartGame(nodeName);
    }

    public void RestartGame(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
            return;

        StopDialogueInternal(
            clearHistory: true,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationSession: true,
            resetVisualState: true);

        _vnScreenBindings.GoToPresentationView();
        presentationRouteEntry.RestartRoute(presentationEntryKey);
        dialogueRunner.StartDialogue(nodeName);
    }
    
    public void RestartForRollback(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning("[EpisodePlayer] RestartForRollback ignored. nodeName is null or empty.", this);
            return;
        }

        StopDialogueInternal(
            clearHistory: false,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationSession: true,
            resetVisualState: true);

        _vnScreenBindings.GoToPresentationView();
        presentationRouteEntry.RestartRoute(presentationEntryKey);
        dialogueRunner.StartDialogue(nodeName);
    }

    public void StopDialogue()
    {
        StopDialogueInternal(
            clearHistory: true,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationSession: true,
            resetVisualState: true);
    }

    private void StopDialogueInternal(
        bool clearHistory,
        bool clearBacklog,
        bool stopYarnRunner,
        bool endPresentationSession,
        bool resetVisualState)
    {
        if (clearHistory)
            _nodeRollbackHistory.ClearRollbackHistory();

        if (clearBacklog)
            _backlogRecorder.ClearBacklog();

        if (stopYarnRunner)
            StopYarnRunnerNow();

        _linePresentationAborter?.AbortCurrentVnLine();

        if (endPresentationSession)
            EndPresentationRouteNow();

        if (resetVisualState)
            ResetVisualState();
    }

    private void StopYarnRunnerNow()
    {
        // if (dialogueRunner.IsDialogueRunning)
        //     dialogueRunner.Stop();
        
        // if (subPresentationRunner.IsDialogueRunning)
        //     subPresentationRunner.Stop();
        // 
    }

    private void EndPresentationRouteNow()
    {
        if (presentationRouteEntry == null)
            return;

        presentationRouteEntry.EndRouteNow();
    }

    private void ResetVisualState()
    {
        presentationResponseRig.Clear();

        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        SlantedMaskGraphic mask = provider.SlantedMaskEdgeGraphic.GetComponent<SlantedMaskGraphic>();
        mask?.ResetToHiddenOffset();
    }
}