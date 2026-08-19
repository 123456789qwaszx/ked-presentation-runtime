using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    private readonly VnPlaybackRuntimeState _playbackState = new();
    
    private readonly RollbackHistory _rollbackHistory = new();
    private readonly ChoiceHistory _choiceHistory = new();
    private readonly VNLinePresentationState _linePresentationAdvanceState = new();
    
    private readonly PresentationStage _presentationStage = new();
    private readonly BacklogRecorder _backlogRecorder = new ();
    
    private VnScreenBindings _screenBindings;

    [Header("UIManager")]
    [SerializeField] private UIManager uiManager;
    
    [Header("Sound")] 
    [SerializeField] private AudioSystem audioSystem;
    
    [Header("DialogueBox")] 
    [SerializeField] private DialogueSurfaceLayoutPresetDBSO surfaceLayoutPresetDbSo;
    [SerializeField] private DialogueSpeakerPresentationPolicyDBSO _dialogueSpeakerPresentationPolicyDbSo;
    
    [Header("Presentation")] 
    [SerializeField] private RoleAnchorTuningDBSO roleTuningDb;
    
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private CharacterFocusTuningDBSO characterFocusTuningDb;
    [SerializeField] private CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb;
    
    [SerializeField] private CharacterDepthTuningSO characterDepthTuning;
    
    [SerializeField] private CommandExecutor commandExecutor;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [SerializeField] private CustomLinePresenter customLinePresenter;
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;
    [SerializeField] private AutoAdvanceScheduler autoAdvanceScheduler;
    
    [SerializeField] private VNOptionsPresenter vnOptionsPresenter;
    [SerializeField] private VNOptionItem optionItem;
    
    [Header("Entry Keys")]
    [Tooltip("디버그 키(2번)와 타이틀에서 재생할 yarn 노드 이름.")]
    [SerializeField] private string yarnEntryKey;

    [Header("VnAdvanceGate")]
    [SerializeField] private VnAdvanceInputPoller vnAdvanceInputPoller;


    [Header("RigPrefab")] [Tooltip("CharacterRig prefab used for command presentation. " +
                                   "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
                                   "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")] 
    [SerializeField] private RectTransform rigPrefab;
    [SerializeField] private RectTransform backgroundRigPrefab;

    [Header("등가성 하네스")]
    [Tooltip("켜면 재생 중 라인마다 (코어 리듀서로 접은 상태) vs (실제 무대)를 비교하고 " +
             "종료 시 EquivalenceReports/*.json을 남긴다. 판정 전용 — 재생에 영향 없음.")]
    [SerializeField] private bool enableEquivalenceHarness;
    
    [Header("UI")]
    [SerializeField] private ScreenNoisePresetDBSO screenNoisePresetDbso;
    [SerializeField] private ScreenVignettePresetDBSO screenVignettePresetDbso;
    [SerializeField] private ScreenFlashPresetDBSO screenFlashPresetDbso;
    
    [SerializeField] private StageMaskMotionPresetDBSO stageMaskMotionPresetDbSo;
    
    [Header("Screen Effect Rig")]
    [SerializeField] private RectTransform screenEffectRigMount;
    [SerializeField] private RectTransform screenEffectRigPrefab;
    
    [Header("NodeDebug")] 
    [SerializeField] private CharacterFocusDebugView characterFocusDebugView;
    
    private IUIThemePatchPort _uiThemePatch;
    
    private VNRuntimeStateProvider _vnRuntimeStateProvider;
    private PresentationShotResponseSystem _presentationResponseRig;
    private PresentationScopeSession _presentationScopeSession;

    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher = new();
    private readonly VnFeatureController _vnFeatureController = new();
    private EpisodePlayer _episodePlayer;
    
    private PresentationUIRoot _presentationUIRoot;
    
    
    private void Awake()
    {
        BootstrapUIManager();
        
        _vnRuntimeStateProvider = new VNRuntimeStateProvider(_rollbackHistory, _choiceHistory);
        
        _presentationUIRoot = uiManager.GetUI<PresentationUIRoot>();
        
        _screenBindings = new VnScreenBindings(uiManager);
        
        
        IShotResponseStageProvider shotResponseStageProvider = _presentationUIRoot;
        
        _presentationResponseRig = new PresentationShotResponseSystem(shotResponseStageProvider);
        
        characterFocusDebugView.Initialize(
            _presentationStage,
            shotResponseStageProvider,
            characterFocusTuningDb);
        

        BootstrapAudioSystem();

        BootstrapPresentationSession();
        
        BootstrapYarn();
        
        // poller가 EpisodePlayer를 물고 있으므로 재생 컨트롤보다 먼저 만든다.
        CreateEpisodePlayer();

        BootstrapPlaybackControls();
        BootstrapScreenBindings();

        BootstrapEquivalenceHarness();
    }

    private void BootstrapAudioSystem()
    {
        audioSystem.Initialize();
    }

    private void BootstrapUIManager()
    {
        SpritePortAssignmentBuilder spritePortAssignmentBuilder = new();
        ResourcesUISpriteLoader resourcesUISpriteLoader = new();
        UISpritePatcher uiSpritePatcher = new(resourcesUISpriteLoader);
        UIPatchService uiPatchService = new(spritePortAssignmentBuilder, uiSpritePatcher);

        uiManager.Init();
        uiManager.AttachUIPatchService(uiPatchService);

        _uiThemePatch = new UIThemePatchAdapter(uiManager, uiPatchService);
    }

    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        UnitySignalBus unitySignalBus = new();
        unitySignalBus.OnSignal += signalLatch.Latch;

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new(_presentationUIRoot);
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(portraitGeneratedDbSo);

        CharacterRigCommandFactory charRigFactory = new(
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            roleTuningDb,
            characterFocusTuningDb,
            characterVisualFocusPresetDb,
            characterDepthTuning,
            _presentationUIRoot);

        // Background Rig
        BackgroundRigBuilder backgroundRigBuilder = new();
        BackgroundRigSlotResolver backgroundRigSlotResolver = new(_presentationUIRoot);
        
        BackgroundRigCommandFactory backgroundRigFactory = new(
            backgroundRigBuilder,
            backgroundRigSlotResolver);

        ShotResponseCommandFactory presentationShotFactory = new(
            _presentationResponseRig, characterFocusTuningDb, _presentationUIRoot);

        // Presentation Control
        UnityTimeSource unityTimeSource = new();

        PresentationControlCommandFactory presentationControlFactory = new(
            _uiThemePatch,
            unityTimeSource,
            unitySignalBus,
            signalLatch);

        // Audio
        AudioCommandFactory audioFactory = new(audioSystem);
        
        ScreenEffectRig screenEffectRig = EnsureScreenEffectRig();

        ScreenEffectCommandFactory screenEffectFactory = new(
            screenEffectRig,
            screenFlashPresetDbso,
            screenNoisePresetDbso, 
            screenVignettePresetDbso,
            stageMaskMotionPresetDbSo,
            _presentationUIRoot,
            _presentationUIRoot);
        
        CompositeCommandFactory factory = new(
            charRigFactory,
            backgroundRigFactory,
            presentationShotFactory,
            presentationControlFactory,
            audioFactory,
            screenEffectFactory);

        commandExecutor.Initialize(factory);

        PresentationSessionContext presentationSessionContext = new(_playbackState);

        _presentationScopeSession = new(
            commandExecutor,
            presentationSessionContext,
            _linePresentationAdvanceState,
            _presentationStage);
    }
    
    private ScreenEffectRig EnsureScreenEffectRig()
    {
        ScreenEffectRig screenEffectRig = screenEffectRigMount.GetComponentInChildren<ScreenEffectRig>(true);

        if (screenEffectRig == null)
        {
            ScreenEffectRigBuilder screenEffectRigBuilder = new();
            
            RectTransform rigRoot = screenEffectRigBuilder.BuildRigRoot(
                screenEffectRigPrefab);

            rigRoot.SetParent(screenEffectRigMount, false);

            if (!rigRoot.TryGetComponent(out screenEffectRig))
                screenEffectRig = rigRoot.gameObject.AddComponent<ScreenEffectRig>();
        }

        screenEffectRig.Initialize();

        return screenEffectRig;
    }
    
    private void BootstrapYarn()
    {
        YarnPlaybackDriver yarnPlaybackDriver = new(commandExecutor, _presentationScopeSession);

        DialogueSurfaceBox dialogueSurfaceBox = uiManager.GetUI<DialogueSurfaceBox>();

        DialogueBoxCurrentState dialogueBoxState = new();
        DialogueSurfaceState dialogueSurfaceState = new();
        DialogueBoxMetadataResolver metadataResolver = new();

        DialogueBoxPresentationController dialogueBoxPresentationController = new(
            dialogueBoxState, 
            dialogueSurfaceBox,
            metadataResolver, 
            dialogueSurfaceState, 
            surfaceLayoutPresetDbSo,
            _dialogueSpeakerPresentationPolicyDbSo);
        
        // 생성자가 러너에 커맨드 핸들러를 전부 등록.
        _ = new YarnCommandBridge(
            dialogueRunner,
            yarnPlaybackDriver,
            rigPrefab,
            backgroundRigPrefab,
            dialogueBoxPresentationController);
        
        VNYarnLineBoundary vnYarnLineBoundary = new (
            _backlogRecorder,
            _rollbackHistory,
            _vnRuntimeStateProvider);
        
        LineHurrySpeedController lineHurrySpeed = new(ellipsisBreathTypewriter);

        VNLinePresentationFlow vnLinePresentationFlow = new(
            vnYarnLineBoundary,
            _linePresentationAdvanceState,
            dialogueBoxPresentationController,
            ellipsisBreathTypewriter,
            yarnPlaybackDriver,
            lineHurrySpeed);

        customLinePresenter.Initialize(
            dialogueRunner,
            vnLinePresentationFlow,
            ellipsisBreathTypewriter,
            _playbackState);
        
        VNChoiceBoundary vnChoiceBoundary = new(
            _choiceHistory,
            _rollbackHistory);

        VNDefaultOptionsPanel vnDefaultOptionsPanel = uiManager.GetUI<VNDefaultOptionsPanel>();

        VNOptionsPresentationFlow optionsPresentationFlow = new(
            vnDefaultOptionsPanel,
            vnChoiceBoundary,
            _linePresentationAdvanceState);

        vnOptionsPresenter.Initialize(
            dialogueRunner,
            optionsPresentationFlow,
            optionItem);
    }
    
    private void CreateEpisodePlayer()
    {
        _episodePlayer = new EpisodePlayer(
            new YarnEpisodeNodeRunner(dialogueRunner),
            _screenBindings,
            _rollbackHistory,
            customLinePresenter,
            _backlogRecorder,
            _presentationResponseRig,
            _presentationStage,
            _presentationScopeSession);
    }

    private void BootstrapPlaybackControls()
    {
        autoAdvanceScheduler.Initialize(
            _playbackState,
            _dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        RapidSkipController rapidSkipController = new(_dialogueAdvanceDispatcher);

        _vnFeatureController.Initialize(
            _playbackState,
            _linePresentationAdvanceState,
            ellipsisBreathTypewriter,
            _backlogRecorder,
            autoAdvanceScheduler,
            rapidSkipController,
            _rollbackHistory,
            _choiceHistory);

        AdvanceGate advanceGate = new(
            _playbackState,
            _linePresentationAdvanceState,
            _presentationScopeSession);

        _dialogueAdvanceDispatcher.Initialize(advanceGate, dialogueRunner, _linePresentationAdvanceState);

        // 키 배치는 poller의 인스펙터 필드가 원본이다.
        vnAdvanceInputPoller.Initialize(
            _dialogueAdvanceDispatcher,
            _vnFeatureController,
            _linePresentationAdvanceState,
            _episodePlayer,
            yarnEntryKey);
    }
    
    // 등가성 하네스. 토글이 꺼져 있으면 아무것도 만들지 않는다 — 재생 경로 무영향.
    private void BootstrapEquivalenceHarness()
    {
        if (!enableEquivalenceHarness)
            return;

        GameObject harnessGo = new("StageEquivalenceHarness");
        harnessGo.transform.SetParent(transform, false);

        StageEquivalenceHarness harness = harnessGo.AddComponent<StageEquivalenceHarness>();

        harness.Initialize(
            _vnRuntimeStateProvider,
            _presentationScopeSession,
            _presentationResponseRig,
            _dialogueAdvanceDispatcher);
    }

    private void BootstrapScreenBindings()
    {
        _screenBindings.ConfigurePresentationView(
            _vnFeatureController,
            _dialogueAdvanceDispatcher,
            _linePresentationAdvanceState);

        _screenBindings.ConfigureTitleView(_episodePlayer);
    }
    
    private void Start()
    {
        OpenInitialScreen();
    }
    
    private void OpenInitialScreen()
    {
        _screenBindings.OpenTitleMenu();
    }
}