using System.Collections;
using UnityEngine;
using Yarn.Unity;

public interface IRollbackDialogueRestarter
{
    void RestartNode(string nodeName);
}

public sealed class EpisodePlayer : MonoBehaviour, IRollbackDialogueRestarter
{
    private VnScreenBindings _vnScreenBindings;
    private DialogueUIBindings _dialogueUIBindings;
    
    public DialogueRunner dialogueRunner;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    [SerializeField] private PresentationSessionEntry presentationRouteEntry;
    
    [SerializeField] public string yarnEntryKey;
    [SerializeField] public string presentationEntryKey;
    
    [Tooltip("Yarn 실행")]
    [SerializeField] private KeyCode runYarnKey = KeyCode.Alpha2;
    
    [Tooltip("Stop")]
    [SerializeField] private KeyCode stopKey = KeyCode.Alpha3;

    public void Initialize(VnScreenBindings vnScreenBindings, DialogueUIBindings dialogueUIBindings)
    {
        _vnScreenBindings = vnScreenBindings;
        _dialogueUIBindings = dialogueUIBindings;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(runYarnKey))
        {
            Debug.Log("TryStartYarnNode", this);
            OpenDialogueUI();
            StartPresentationRoute(presentationEntryKey);
            StartYarnNode(yarnEntryKey);
        }
        
        if (Input.GetKeyDown(stopKey))
            StopDialogue();
    }

    public void OpenDialogueUI()
    {
        UIManager.Instance.SwitchRoot<DialogueUIRoot>();
        DialogueUIRoot dialogueUIRoot = UIManager.Instance.GetUI<DialogueUIRoot>();
        _dialogueUIBindings.Bind(dialogueUIRoot);
    }

    public void StartPresentationRoute(string routeKey)
    {
        presentationRouteEntry.StartRoute(routeKey);
    }
    
    public void RestartNode(string nodeName)
    {
        StartYarnNode(nodeName);
    }
    
    public void StartYarnNode(string episodeId)
    {
        yarnUIBridge.HasCharNameBox();
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
        if (!yarnUIBridge.IsDialogueBoxReady)
            return false;

        return true;
    }
    
    
    public void StopDialogue()
    {
        if (dialogueRunner == null) return;

        if (dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.Stop();
            Debug.Log("Stop Dialogue", this);
        }
    }
}