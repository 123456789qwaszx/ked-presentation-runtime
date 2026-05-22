using UnityEngine;
using Yarn.Unity;

public sealed class EpisodePlayer : MonoBehaviour
{
    private PresentationViewUIBindings _dialogueUIBindings;
    private RollbackHistory  _nodeRollbackHistory;
    private ILinePresentationAborter _linePresentationAborter;
    private BacklogRecorder _backlogRecorder;
    
    public DialogueRunner dialogueRunner;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;
    [SerializeField] private PresentationResponseRig presentationResponseRig;
    
    [SerializeField] public string yarnEntryKey;
    public string YarnEntryKey => yarnEntryKey;
    [SerializeField] public string presentationEntryKey;
    
    
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;
    
    [Tooltip("Stop")]
    [SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public void Initialize(
        PresentationViewUIBindings dialogueUIBindings,
        RollbackHistory nodeRollbackHistory,
        ILinePresentationAborter linePresentationAborter,
        BacklogRecorder backlogRecorder)
    {
        _dialogueUIBindings = dialogueUIBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
        _backlogRecorder = backlogRecorder;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
        {
            StopDialogue();
            StartGame(yarnEntryKey);
        }

        if (Input.GetKeyDown(stopKey))
        {
            StopDialogue();
        }
    }

    public void StartGame(string nodeName)
    {
        _backlogRecorder.ClearBacklog();
        
        if(!presentationRouteEntry.IsRunning)
            presentationRouteEntry.StartRoute(presentationEntryKey);
        
        UIManager.Instance.SwitchRoot<PresentationUIRoot>();
        
        PresentationUIRoot dialogueUIRoot = UIManager.Instance.GetUI<PresentationUIRoot>();
        _dialogueUIBindings.Bind(dialogueUIRoot);
        
        dialogueRunner.StartDialogue(nodeName);
    }
    
    public void StopDialogue()
    {
        _nodeRollbackHistory.ClearRollbackHistory();
        _backlogRecorder.ClearBacklog();
        
        if (dialogueRunner.IsDialogueRunning)
            dialogueRunner.Stop();

        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();
        presentationRouteEntry.RequestEnd();
        presentationResponseRig.Clear();
        ResetSlantedMasks();
    }
    
    private void ResetSlantedMasks()
    {
        IPresentationTransitionSlotProvider provider = UIManager.Instance.GetUI<PresentationUIRoot>();
        RectTransform[] roots =
        { 
            provider.SlantedMaskEdgeGraphic
        };
        
        for (int i = 0; i < roots.Length; i++)
        {
            SlantedMaskGraphic mask = roots[i].GetComponent<SlantedMaskGraphic>();
            mask?.ResetToHiddenOffset();
        }
    }
}