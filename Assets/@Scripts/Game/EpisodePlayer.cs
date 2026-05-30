using UnityEngine;
using Yarn.Unity;

public sealed class EpisodePlayer : MonoBehaviour
{
    private VnScreenBindings _vnScreenBindings;
    private RollbackHistory _nodeRollbackHistory;
    private ILinePresentationAborter _linePresentationAborter;
    private BacklogRecorder _backlogRecorder;
    private VNSaveLoadSystem _vnSaveLoadSystem;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;
    

    [Header("Presentation")]
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
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
    public string PresentationEntryKey => presentationEntryKey;

    public bool IsDialogueRunning
    {
        get
        {
            return dialogueRunner != null && dialogueRunner.IsDialogueRunning;
        }
    }

    public bool IsPresentationRunning
    {
        get
        {
            return presentationRouteEntry != null && presentationRouteEntry.IsRunning;
        }
    }

    public void Initialize(
        VnScreenBindings vnScreenBindings,
        RollbackHistory nodeRollbackHistory,
        ILinePresentationAborter linePresentationAborter,
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

    /// <summary>
    /// Public legacy entry point.
    /// Treat this as a fresh restart, not as "start only if idle".
    /// </summary>
    public void StartGame(string nodeName)
    {
        RestartGame(nodeName);
    }

    /// <summary>
    /// Public load entry point.
    /// This intentionally uses the same restart path as StartGame.
    /// Load seek needs a fresh Presentation route before Yarn starts replaying.
    /// </summary>
    public void LoadGame(string nodeName)
    {
        RestartGame(nodeName);
    }

    /// <summary>
    /// Fully restarts Yarn + Presentation for a node.
    /// This is the safe path for Load, Continue, Debug restart, and manual restart.
    /// </summary>
    public void RestartGame(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning("[EpisodePlayer] RestartGame ignored. nodeName is null or empty.", this);
            return;
        }

        StopDialogueInternal(
            clearHistory: true,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationNow: true,
            clearPresentationVisuals: true);

        PreparePresentationView();

        StartPresentationRouteFresh();

        StartYarn(nodeName);
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
            endPresentationNow: true,
            clearPresentationVisuals: true);

        PreparePresentationView();

        StartPresentationRouteFresh();

        StartYarn(nodeName);
    }

    /// <summary>
    /// Stops both Yarn and Presentation immediately.
    /// Unlike the old implementation, this does not merely request presentation end.
    /// </summary>
    public void StopDialogue()
    {
        StopDialogueInternal(
            clearHistory: true,
            clearBacklog: true,
            stopYarnRunner: true,
            endPresentationNow: true,
            clearPresentationVisuals: true);
    }

    private void StopDialogueInternal(
        bool clearHistory,
        bool clearBacklog,
        bool stopYarnRunner,
        bool endPresentationNow,
        bool clearPresentationVisuals)
    {
        if (clearHistory)
            _nodeRollbackHistory?.ClearRollbackHistory();

        if (clearBacklog)
            _backlogRecorder?.ClearBacklog();

        if (stopYarnRunner)
            StopYarnRunnerNow();

        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();

        if (endPresentationNow)
            EndPresentationRouteNow();

        if (clearPresentationVisuals)
            ClearPresentationVisualState();
    }

    private void StopYarnRunnerNow()
    {
        // if (dialogueRunner.IsDialogueRunning)
        //     dialogueRunner.Stop();
        //
        // 
        
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

    private void ClearPresentationVisualState()
    {
        if (presentationResponseRig != null)
            presentationResponseRig.Clear();

        ResetSlantedMasks();
    }

    private void PreparePresentationView()
    {
        _vnScreenBindings.GoToPresentationView();
    }

    private void StartPresentationRouteFresh()
    {
        presentationRouteEntry.RestartRoute(presentationEntryKey);
    }

    private void StartYarn(string nodeName)
    {
        dialogueRunner.StartDialogue(nodeName);
    }

    private void ResetSlantedMasks()
    {
        PresentationUIRoot root = UIManager.Instance.GetUI<PresentationUIRoot>();
        if (root == null)
            return;

        IPresentationTransitionSlotProvider provider = root;
        if (provider == null || provider.SlantedMaskEdgeGraphic == null)
            return;

        SlantedMaskGraphic mask = provider.SlantedMaskEdgeGraphic.GetComponent<SlantedMaskGraphic>();
        mask?.ResetToHiddenOffset();
    }
}