using System.IO;
using Ked.Progression;
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
    private readonly EpisodeSkipController _episodeSkipController = new();

    private VNScreenBindings _screenBindings;
    private IUIThemePatchPort _uiThemePatch;

    private VNRuntimeStateProvider _vnRuntimeStateProvider;

    private PresentationUIRoot _presentationUIRoot;
    private PresentationShotResponseSystem _presentationResponseRig;
    private ScreenEffectRig _screenEffectRig;

    private PresentationScopeSession _presentationScopeSession;
    private VNFeatureController _vnFeatureController;
    private ScenePlaybackSession _scenePlayback;
    private ScenePlaybackDebugRunner _debugPlayback;

    private IChapterOptionsView _progressionOptions;
    private ProgressionDriver _progressionDriver;
    private ProgressionLauncher _progressionLauncher;

    private SaveCoordinator _saveCoordinator;
    private ILocalSaveStore _localSaveStore;
    private bool _useLearningSaveData;
    private string _saveRoot;
    private TextAsset _chapterJson;
    
    private AlbumUnlockService _albumUnlockService;
    private AlbumController _albumController;

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

    [Tooltip("진행 층 없이 노드 사슬만 시험한다. " +
             "씬에 값이 없으면 여기 적힌 기본값을 쓴다.")]
    [SerializeField] private string[] debugEpisodeChain = { "new01", "new02" };

    [Header("진행 층")]
    [Tooltip("툴이 낸 챕터 JSON. Assets/@Dialogue/ChapterProgression/ 아래의 .json 을 넣는다. " +
             "경로 문자열이 아니라 에셋 참조다 — 머신에 안 매이고 빌드에도 실린다.")]
    [SerializeField] private TextAsset progressionChapterJson;

    [Header("로컬 저장 데이터")]
    [FormerlySerializedAs("learningMode")]
    [Tooltip("기존 학습용 콘텐츠와 saves-learning 경로를 선택한다. 저장 기능과 네트워크 사용 여부에는 영향이 없다.")]
    [SerializeField] private bool useLearningSaveData;
    [SerializeField] private TextAsset learningChapterJson;
    [Tooltip("저장과 현재 대본의 호환성을 판별한다. 복원에 영향을 주는 대본 변경 시 값을 올린다.\n" +
             "2 — Yarn 변수 층 제거(2026-09-17). 1로 저장된 회차는 대사가 $작가변수를 읽던 대본이라 재개할 수 없다.")]
    [SerializeField] private string saveContentVersion = "2";

    [Header("Album")]
    [SerializeField] private VNAlbumDatabaseSO albumDatabase;

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
        // 실행 중 Inspector를 바꿔도 저장 경로와 기능 구성이 서로 엇갈리지 않는다.
        _useLearningSaveData = useLearningSaveData;
        _saveRoot = Path.Combine(Application.persistentDataPath,
            _useLearningSaveData ? "saves-learning" : "saves");
        _chapterJson = _useLearningSaveData ? learningChapterJson : progressionChapterJson;

        Debug.Log($"[저장] 경로: {_saveRoot}");

        if (_useLearningSaveData && _chapterJson == null)
        {
            Debug.LogError("[학습] Learning Chapter Json에 qwer_scene.progression.json을 지정해야 한다.");
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(saveContentVersion))
        {
            Debug.LogError("[저장] Save Content Version을 지정해야 한다.");
            enabled = false;
            return;
        }

        BootstrapUIManager();
        
        BootstrapAlbum();

        BootstrapPresentationRoots();

        BootstrapAudioSystem();

        BootstrapPresentationSession();

        BootstrapYarn();

        CreateScenePlayback();

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
    
    private void BootstrapAlbum()
    {
        string albumPath = Path.Combine(_saveRoot, "album.json");

        IAlbumProgressStore albumStore = new LocalAlbumProgressStore(albumPath);

        _albumUnlockService = new(albumDatabase, albumStore);
        _albumController = new(albumDatabase, _albumUnlockService);
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
        
        EaseCurveLibrary easeCurves = EaseCurveLibrary.LoadFrom(
            System.IO.Path.Combine(Application.dataPath, "@Dialogue", EaseCurveLibrary.BundleFileName));

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
    
    private void CreateScenePlayback()
    {
        IEpisodeNodeRunner nodeRunner =
            new YarnEpisodeNodeRunner(dialogueRunner);

        _scenePlayback = new ScenePlaybackSession(
            nodeRunner,
            _screenBindings,
            _rollbackHistory,
            customLinePresenter,
            _presentationResponseRig,
            _presentationStage,
            _presentationScopeSession,
            _choiceHistory,
            _episodeSkipController);

        _debugPlayback = new ScenePlaybackDebugRunner(
            _scenePlayback,
            _backlogRecorder);

        _progressionOptions = new ChapterOptionsView(
            uiManager.GetUI<VNDefaultOptionsPanel>(),
            optionItem);

        _saveCoordinator = CreateSaveCoordinator();

        // 진행 런타임은 아래 계약만 안다.
        // Stage / Save로 잇는 일은 전부 이 자리에서 끝난다.
        UnityProgressionLog progressionLog = new();

        ProgressionReplayState replayState = new(
            _linePresentationAdvanceState,
            _choiceHistory);

        ProgressionRollbackHistory rollbackHistory = new(_rollbackHistory);

        // 저장은 실행이고, 아래 lifecycle 로그는 관찰이다. 둘을 합치지 않는다.
        ProgressionSaveBridge savePersistence = new(
            _saveCoordinator,
            _backlogRecorder,
            _choiceHistory);

        ProgressionLifecycleLog lifecycleLog = new();

        SceneRunner sceneRunner = new SceneRunner(
            _scenePlayback,
            _progressionOptions,
            replayState,
            rollbackHistory,
            savePersistence,
            lifecycleLog,
            _backlogRecorder,
            progressionLog);

        _progressionDriver = new ProgressionDriver(
            sceneRunner,
            lifecycleLog,
            progressionLog);

        _progressionLauncher = new ProgressionLauncher(
            _progressionDriver,
            dialogueRunner,
            _chapterJson,
            _saveCoordinator.LoadActiveResumePoint,
            _saveCoordinator.PrepareNewPlaythroughAsync,
            _backlogRecorder,
            replayState);

        // 대사가 스탯을 읽는 길. 드라이버가 선 뒤에 등록해야 람다가 null을 잡지 않는다.
        new ProgressionStatFunction(() => _progressionDriver.CurrentState)
            .Register(dialogueRunner);
    }

    // 모든 실행 모드에서 로컬 저장을 기본으로 사용한다.
    private SaveCoordinator CreateSaveCoordinator()
    {
        _localSaveStore = new LocalFileSaveStore(_saveRoot);
        return new SaveCoordinator(_localSaveStore, saveContentVersion);
    }

    private void BootstrapPlaybackControls()
    {
        autoAdvanceScheduler.Initialize(
            _playbackState,
            _dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        RapidSkipController rapidSkipController = new(
            _dialogueAdvanceDispatcher);

        _episodeSkipController.Initialize(
            _dialogueAdvanceDispatcher);

        _vnFeatureController = new VNFeatureController(
            _playbackState,
            _linePresentationAdvanceState,
            ellipsisBreathTypewriter,
            _backlogRecorder,
            autoAdvanceScheduler,
            rapidSkipController,
            _episodeSkipController,
            _rollbackHistory,
            _choiceHistory);

        AdvanceGate advanceGate = new(
            _playbackState,
            _linePresentationAdvanceState,
            _presentationScopeSession);

        _dialogueAdvanceDispatcher.Initialize(
            advanceGate,
            dialogueRunner,
            _linePresentationAdvanceState);

        vnAdvanceInputPoller.Initialize(
            _dialogueAdvanceDispatcher,
            _vnFeatureController,
            _progressionLauncher);
    }
    
    private void BootstrapScreenBindings()
    {
        ManualSaveFlow manualSaveFlow = new(
            _saveCoordinator,
            _progressionLauncher,
            _vnFeatureController);
        
        _screenBindings.Configure(
            _vnFeatureController,
            _dialogueAdvanceDispatcher,
            _progressionLauncher,
            _saveCoordinator,
            _albumController,
            manualSaveFlow);
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

    private void Update()
    {
        _saveCoordinator?.TickMaintenance(Time.realtimeSinceStartup);
    }
    
    private void OpenInitialScreen()
    {
        _screenBindings.OpenTitleMenu();
    }

    [ContextMenu("Save/Log current snapshot")]
    private void LogSaveSnapshot()
    {
        if (!Application.isPlaying || _localSaveStore == null)
        {
            Debug.Log("[저장] 실행한 뒤 저장 snapshot을 확인할 수 있다.");
            return;
        }

        LocalSaveFile snapshot = _localSaveStore.LoadActive();

        if (snapshot == null)
        {
            Debug.Log("[저장] 아직 첫 장면에 진입하지 않아 저장된 회차가 없다.");
            return;
        }

        Debug.Log($"[저장] 현재 확정 snapshot (로컬 envelope 제외)\n{SaveJson.SerializePretty(snapshot)}");
    }
}
