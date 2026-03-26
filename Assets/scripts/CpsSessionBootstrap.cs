using System.Collections;
using UnityEngine;

public interface IInlineSignalHost
{
    void RaiseSignal(string key);
}

public interface ICpsSignalBridge
{
    IEnumerator Beat(string beatKey); 
    IEnumerator WaitSignal(string key);
}

public sealed class CpsSessionBootstrap : MonoBehaviour, ICpsSignalBridge, IInlineSignalHost
{
    [SerializeField]PlaybackSettings settings = new ();
    
    [Header("Ports / Adapters")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private UnitySignalBus signals;

    public PresentationSession Session { get; private set; }
    private StepGateAdvancer _gateAdvancer;
    private UnityInputSource _inputSource;
    
    [SerializeField] private PortraitGeneratedDBSO portraitRefsSo;

    private bool IsBootstrapped { get; set; }

    public void Initialize()
    {
        if (IsBootstrapped) return;

        if (commandExecutor == null)
        {
            Debug.LogError("[CpsSessionBootstrap] CommandExecutor is not assigned.");
            return;
        }

        if (signals == null)
        {
            GameObject go = new ("[Auto] UnitySignalBus");
            signals = go.AddComponent<UnitySignalBus>();
            Debug.LogWarning("[CpsSessionBootstrap] signals not found, created new UnitySignalBus GameObject.");
        }

        // Build gate stack
        StepGatePlanBuilder gatePlanner = new ();

        
        UnityTimeSource time = new();
        SignalLatch latch = new();
        signals.OnSignal += latch.Latch;
        
        
        _inputSource = new UnityInputSource();
        StepGateAdvancer gateAdvancer = new (_inputSource, time, signals, latch);
        _gateAdvancer = gateAdvancer;
        
        CharRigSlotResolver charRigSlotResolver = new(
            UIManager.Instance.GetUI<DialogueUIRoot>(),
            UIManager.Instance.GetUI<DialogueBox00_WithPortrait>()
            );
        PortraitResolver portraitResolver = new (portraitRefsSo);
        
        CharacterRigAccess charRigAccess = new(charRigSlotResolver);
        SignalCommandFactory signalFactory = new(time, signals, latch);
        CharRigCommandFactory charRigFactory = new(charRigAccess, portraitResolver);

        commandExecutor.InitializeCore(signalFactory, charRigFactory);


        Session = new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, settings);
        IsBootstrapped = true;
    }
    
    private void Update()
    {
        if (IsBootstrapped && Session != null)
            Session.Tick();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _inputSource.PulseAdvancePressed();
        }
    }
    
    
    public IEnumerator Beat(string beatKey)
    {
        if (Session == null || !Session.IsRunning) yield break;

        signals.Raise(beatKey);

        while (Session.IsNodeBusy())
            yield return null;
        
        Debug.Log($"[Command] {beatKey}");
    }
    
    /// <summary>
    /// 최대 10초 대기
    /// </summary>
    public IEnumerator WaitSignal(string key)
    {
        if (string.IsNullOrEmpty(key)) yield break;
        if (signals == null) yield break;

        bool received = false;
        float timeoutSeconds = 10f;
        float elapsedTime = 0f;

        void Handler(string raisedKey)
        {
            if (raisedKey == key)
                received = true;
        }

        signals.OnSignal += Handler;

        try
        {
            while (!received && elapsedTime < timeoutSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!received)
            {
                Debug.LogError($"[WaitSignal] Timeout after {timeoutSeconds}s waiting for signal: '{key}'");
            }
        }
        finally
        {
            signals.OnSignal -= Handler;
        }
    }
    
    public void RaiseSignal(string key)
    {
        if (signals == null) return;
        
        signals.Raise(key);
        
        Debug.Log($"[Markup] {key}");
    }

    private void OnDestroy()
    {
        _gateAdvancer?.Dispose();
    }
}