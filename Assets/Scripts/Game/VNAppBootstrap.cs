using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

public class VNAppBootstrap : MonoBehaviour
{
    private readonly VNPlaybackRuntimeState _playbackState = new();

    private readonly RollbackHistory _rollbackHistory = new();
    private readonly ChoiceHistory _choiceHistory = new();
    private readonly VNLinePresentationState _linePresentationAdvanceState = new();

    private readonly PresentationStage _presentationStage = new();
    private readonly BacklogRecorder _backlogRecorder = new();

    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher = new();

    private VNScreenBindings _screenBindings;
    private IUIThemePatchPort _uiThemePatch;

    private VNRuntimeStateProvider _vnRuntimeStateProvider;

    private PresentationUIRoot _presentationUIRoot;
    private PresentationShotResponseSystem _presentationResponseRig;
    private ScreenEffectRig _screenEffectRig;

    private PresentationScopeSession _presentationScopeSession;
    private VNFeatureController _vnFeatureController;
    private EpisodePlayer _episodePlayer;

    [Header("UIManager")]
    [SerializeField] private UIManager uiManager;

    [Header("Sound")]
    [SerializeField] private AudioSystem audioSystem;

    [Header("DialogueBox")]
    [FormerlySerializedAs("surfaceLayoutPresetDbSo")]
    [SerializeField] private DialogueSurfaceLayoutPresetDBSO surfaceLayoutPresetDb;
    [FormerlySerializedAs("_dialogueSpeakerPresentationPolicyDbSo")]
    [SerializeField] private DialogueSpeakerPresentationPolicyDBSO speakerPolicyDb;

    [Header("Presentation")]
    [SerializeField] private RoleAnchorTuningDBSO roleTuningDb;

    [FormerlySerializedAs("portraitGeneratedDbSo")]
    [SerializeField] private PortraitGeneratedDBSO portraitGeneratedDb;
    [SerializeField] private CharacterFocusTuningDBSO characterFocusTuningDb;
    [SerializeField] private CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb;

    [SerializeField] private CharacterDepthTuningSO characterDepthTuning;

    [Header("Command")]
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

    [Header("VNAdvanceGate")]
    [SerializeField] private VNAdvanceInputPoller vnAdvanceInputPoller;

    [Header("RigPrefab")]
    [Tooltip("CharacterRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    [SerializeField] private RectTransform rigPrefab;
    [SerializeField] private RectTransform backgroundRigPrefab;

    [Header("등가성 하네스")]
    [Tooltip("켜면 재생 중 라인마다 (코어 리듀서로 접은 상태) vs (실제 무대)를 비교하고 " +
             "종료 시 EquivalenceReports/*.json을 남긴다. 판정 전용 — 재생에 영향 없음.")]
    [SerializeField] private bool enableEquivalenceHarness;

    [Header("UI")]
    [FormerlySerializedAs("screenNoisePresetDbso")]
    [SerializeField] private ScreenNoisePresetDBSO screenNoisePresetDb;
    [FormerlySerializedAs("screenVignettePresetDbso")]
    [SerializeField] private ScreenVignettePresetDBSO screenVignettePresetDb;
    [FormerlySerializedAs("screenFlashPresetDbso")]
    [SerializeField] private ScreenFlashPresetDBSO screenFlashPresetDb;

    [FormerlySerializedAs("stageMaskMotionPresetDbSo")]
    [SerializeField] private StageMaskMotionPresetDBSO stageMaskMotionPresetDb;

    [Header("Screen Effect Rig")]
    [SerializeField] private RectTransform screenEffectRigMount;
    [SerializeField] private RectTransform screenEffectRigPrefab;

    [Header("NodeDebug")]
    [SerializeField] private CharacterFocusDebugView characterFocusDebugView;

    private void Awake()
    {
        BootstrapUIManager();

        BootstrapPresentationRoots();

        BootstrapAudioSystem();

        BootstrapPresentationSession();

        BootstrapYarn();

        CreateEpisodePlayer();

        BootstrapPlaybackControls();

        BootstrapScreenBindings();

        BootstrapEquivalenceHarness();
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
        _screenBindings = new VNScreenBindings(uiManager);
    }

    private void BootstrapPresentationRoots()
    {
        _vnRuntimeStateProvider = new VNRuntimeStateProvider(_rollbackHistory, _choiceHistory);

        _presentationUIRoot = uiManager.GetUI<PresentationUIRoot>();

        IShotResponseStageProvider shotResponseStageProvider = _presentationUIRoot;

        _presentationResponseRig = new PresentationShotResponseSystem(shotResponseStageProvider);
        _screenEffectRig = EnsureScreenEffectRig();

        characterFocusDebugView.Initialize(
            _presentationStage,
            shotResponseStageProvider,
            characterFocusTuningDb);
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

    private void BootstrapAudioSystem()
    {
        audioSystem.Initialize();
    }
    
    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        UnitySignalBus unitySignalBus = new();
        unitySignalBus.OnSignal += signalLatch.Latch;

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new(_presentationUIRoot);
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(portraitGeneratedDb);

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

        ScreenEffectCommandFactory screenEffectFactory = new(
            _screenEffectRig,
            screenFlashPresetDb,
            screenNoisePresetDb, 
            screenVignettePresetDb,
            stageMaskMotionPresetDb,
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

    private void BootstrapYarn()
    {
        YarnPlaybackDriver yarnPlaybackDriver = new(commandExecutor, _presentationScopeSession);

        DialogueSurfaceBox dialogueSurfaceBox = uiManager.GetUI<DialogueSurfaceBox>();

        DialogueBoxCurrentState dialogueBoxState = new();
        DialogueSurfaceState dialogueSurfaceState = new();

        DialogueBoxPresentationController dialogueBoxPresentationController = new(
            dialogueBoxState, 
            dialogueSurfaceBox,
            dialogueSurfaceState, 
            surfaceLayoutPresetDb,
            speakerPolicyDb);
        
        // 커스텀 이징 곡선 — 번들 옆 curves.json. 없으면 커브 0개(무음)가 정상 경로.
        EaseCurveLibrary easeCurves = EaseCurveLibrary.LoadFrom(
            System.IO.Path.Combine(Application.dataPath, "@Dialogue", EaseCurveLibrary.BundleFileName));

        // 생성자가 러너에 커맨드 핸들러를 전부 등록.
        _ = new YarnCommandBridge(
            dialogueRunner,
            yarnPlaybackDriver,
            rigPrefab,
            backgroundRigPrefab,
            dialogueBoxPresentationController,
            easeCurves);
        
        VNYarnLineBoundary vnYarnLineBoundary = new (
            _backlogRecorder,
            _rollbackHistory,
            _vnRuntimeStateProvider,
            _linePresentationAdvanceState);
        
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

        OptionsBoxPresentationController optionsBoxPresentation = new(vnDefaultOptionsPanel);

        VNOptionsPresentationFlow optionsPresentationFlow = new(
            optionsBoxPresentation,
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
            _presentationScopeSession,
            new YarnVariableCheckpoint(dialogueRunner.VariableStorage),
            _choiceHistory);
    }

    private void BootstrapPlaybackControls()
    {
        autoAdvanceScheduler.Initialize(
            _playbackState,
            _dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        RapidSkipController rapidSkipController = new(_dialogueAdvanceDispatcher);

        _vnFeatureController = new(
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

        vnAdvanceInputPoller.Initialize(
            _dialogueAdvanceDispatcher,
            _vnFeatureController,
            _linePresentationAdvanceState,
            _episodePlayer,
            yarnEntryKey);
    }
    
    private void BootstrapScreenBindings()
    {
        _screenBindings.ConfigurePresentationView(
            _vnFeatureController,
            _dialogueAdvanceDispatcher,
            _linePresentationAdvanceState);

        _screenBindings.ConfigureTitleView(_episodePlayer);
    }
    
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
    
    private void Start()
    {
        OpenInitialScreen();
    }
    
    private void OpenInitialScreen()
    {
        _screenBindings.OpenTitleMenu();
    }
}