using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

public interface IRollbackDialogueRestarter
{
    void RestartNode(string nodeName);
}

public sealed class EpisodePlayer : MonoBehaviour, IRollbackDialogueRestarter
{
    private VnScreenBindings _vnScreenBindings;
    private PresentationViewUIBindings _dialogueUIBindings;
    private NodeRollbackHistory  _nodeRollbackHistory;
    
    public DialogueRunner dialogueRunner;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;
    [SerializeField] private BGHost bgHost;
    [SerializeField] private PresentationResponseRig presentationResponseRig;
    
    [SerializeField] public string yarnEntryKey;
    [SerializeField] public string presentationEntryKey;
    
    
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;
    
    [Tooltip("Stop")]
    [SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public void Initialize(VnScreenBindings vnScreenBindings, PresentationViewUIBindings dialogueUIBindings, NodeRollbackHistory nodeRollbackHistory)
    {
        _vnScreenBindings = vnScreenBindings;
        _dialogueUIBindings = dialogueUIBindings;
        _nodeRollbackHistory = nodeRollbackHistory;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
        {
            //Debug.Log("TryStartYarnNode", this);
            OpenDialogueUI();
            
            bgHost.ClearRuntimeBackgrounds();
            presentationResponseRig.ClearRuntimeState();
            
            StartPresentationRoute(presentationEntryKey);
            StartYarnNode(yarnEntryKey);
        }

        if (Input.GetKeyDown(stopKey))
        {
            _nodeRollbackHistory.ClearRollbackHistory();
            StopDialogue();
        }
    }


    public void OpenDialogueUI()
    {
        UIManager.Instance.SwitchRoot<PresentationUIRoot>();
        
        PresentationUIRoot dialogueUIRoot = UIManager.Instance.GetUI<PresentationUIRoot>();
        _dialogueUIBindings.Bind(dialogueUIRoot);
        
        // UIManager.Instance.SwitchRoot<DialogueUIRoot>();
        // DialogueUIRoot dialogueUIRoot = UIManager.Instance.GetUI<DialogueUIRoot>();
        // _dialogueUIBindings.Bind(dialogueUIRoot);
    }
    
    // public void OpenDialogueUI()
    // {
    //     UIManager.Instance.SwitchRoot<DialogueUIRoot>();
    //     DialogueUIRoot dialogueUIRoot = UIManager.Instance.GetUI<DialogueUIRoot>();
    //     _dialogueUIBindings.Bind(dialogueUIRoot);
    // }

    public void StartPresentationRoute(string routeKey)
    {
        presentationRouteEntry.StartRoute(routeKey);
    }
    
    public void RestartNode(string nodeName)
    {
        //OpenDialogueUI();
        //StopDialogue();
        bgHost.ClearRuntimeBackgrounds();
        presentationResponseRig.ClearRuntimeState();
        StartYarnNode(nodeName);
        Debug.Log(nodeName + " started");
        //StartPresentationRoute(presentationEntryKey);
        
        //StartCoroutine(StartYarnNextFrame(nodeName));
    }
    
    private IEnumerator StartYarnNextFrame(string nodeName)
    {
        yield return null; // 한 프레임 대기 — Session Tick이 한 번 돌고 나서
        StartYarnNode(nodeName);
    }
    
    public void StartYarnNode(string episodeId)
    {
        //yarnUIBridge.HasCharNameBox();
        dialogueRunner.StartDialogue(episodeId);
    }
    
    private IEnumerator WaitUntilBoundOrTimeout()
    {
        float start = Time.unscaledTime;
        float timeout = 10;

        if (IsBoundReady())
            yield break;

        while (!IsBoundReady())
        {
            if (timeout > 0f && (Time.unscaledTime - start) >= timeout)
            {
                Debug.LogError($"Bind timeout ({timeout:0.00}s)");
                yield break;
            }

            yield return null;
        }
    }

    private bool IsBoundReady()
    {
        if (dialogueTextRouter.LineText != null)
            return false;

        return true;
    }
    
    
    public void StopDialogue()
    {
        bgHost.ClearRuntimeBackgrounds();
        presentationResponseRig.ClearRuntimeState();
        
        if (dialogueRunner == null) return;

        if (dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.Stop();
        }
    }
}