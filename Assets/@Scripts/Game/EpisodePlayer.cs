using System.Collections;
using UnityEngine;
using Yarn.Unity;

public interface IRollbackDialogueRestarter
{
    void RestartNode(string nodeName);
}

public sealed class EpisodePlayer : MonoBehaviour, IRollbackDialogueRestarter, IVNLoadDialogueRestarter
{
    private VnScreenBindings _vnScreenBindings;
    private PresentationViewUIBindings _dialogueUIBindings;
    private RollbackHistory  _nodeRollbackHistory;
    private ILinePresentationAborter _linePresentationAborter;
    
    
    
    public DialogueRunner dialogueRunner;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;
    [SerializeField] private PresentationResponseRig presentationResponseRig;
    
    [SerializeField] public string yarnEntryKey;
    [SerializeField] public string presentationEntryKey;
    
    
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;
    
    [Tooltip("Stop")]
    [SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public void Initialize(VnScreenBindings vnScreenBindings,
        PresentationViewUIBindings dialogueUIBindings,
        RollbackHistory nodeRollbackHistory,
        ILinePresentationAborter linePresentationAborter)
    {
        _vnScreenBindings = vnScreenBindings;
        _dialogueUIBindings = dialogueUIBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
        _linePresentationAborter = linePresentationAborter;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
        {
            presentationResponseRig.Clear();
            StartPresentationRoute(presentationEntryKey);
            
            OpenDialogueUI();
            
            StartYarnNode(yarnEntryKey);
        }

        if (Input.GetKeyDown(stopKey))
        {
            _nodeRollbackHistory.ClearRollbackHistory();
            StopDialogue();
        }
    }

    public void StartGame()
    {
        StartPresentationRoute(presentationEntryKey);
        
        OpenDialogueUI();
            
        StartYarnNode(yarnEntryKey);
    }


    public void OpenDialogueUI()
    {
        UIManager.Instance.SwitchRoot<PresentationUIRoot>();
        
        PresentationUIRoot dialogueUIRoot = UIManager.Instance.GetUI<PresentationUIRoot>();
        _dialogueUIBindings.Bind(dialogueUIRoot);
        
    }

    public void StartPresentationRoute(string routeKey)
    {
        presentationRouteEntry.StartRoute(routeKey);
    }
    
    public void RestartNode(string nodeName)
    {
        _linePresentationAborter?.AbortCurrentLinePresentationForRollback();
        //StopDialogue();
        presentationResponseRig.Clear();
        StartYarnNode(nodeName);
    }
    
    public void StartYarnNode(string episodeId)
    {
        dialogueRunner.StartDialogue(episodeId);
    }
    
    public void RestartNodeForLoad(string nodeName)
    {
        ClearRuntimeState();
        StartPresentationRoute(presentationEntryKey);
        
        OpenDialogueUI();
        StartYarnNode(nodeName);
    }
    
    private void StopDialogue()
    {
        if (dialogueRunner == null) 
            return;

        if (dialogueRunner.IsDialogueRunning)
            dialogueRunner.Stop();
    }
    
    private void ClearRuntimeState()
    {
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