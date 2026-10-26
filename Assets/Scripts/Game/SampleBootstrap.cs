using System;
using System.IO;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

/// <summary>
/// 예제 씬의 진입점. 연출 런타임을 세우고, 고른 예제에 맞는 재생 호스트를 하나 붙인다.
///
/// - 연출 단독: 진행·저장 없이 Yarn 노드 하나를 바로 재생한다(NodePlaybackHost).
/// - 진행·저장: 타이틀 → 챕터 진행 → 장면 경계 저장(ProgressionLauncher).
///
/// 연출 런타임의 조립은 PresentationAssembler가 한다. 여기는 씬 참조를 넘기고,
/// 진행·저장 예제일 때만 그 위에 진행·저장을 얹는다.
///
/// 스크립트 GUID는 예전 VNAppBootstrap 그대로다 — 씬에 직렬화된 값이 유지된다.
/// </summary>
public class SampleBootstrap : MonoBehaviour
{
    public enum SampleExample
    {
        ProgressionAndSave = 0,
        PresentationOnly = 1,
    }

    private VNScreenBindings _screenBindings;
    private PresentationRuntime _runtime;

    // 연출 단독
    private NodePlaybackHost _nodePlaybackHost;

    // 진행·저장
    private IChapterOptionsView _progressionOptions;
    private ProgressionDriver _progressionDriver;
    private ProgressionLauncher _progressionLauncher;
    private ActiveResumeSaveFlow _activeResumeSaveFlow;

    private SaveCoordinator _saveCoordinator;
    private SaveSlotService _saveSlotService;
    private PlaythroughForkService _playthroughForkService;
    private LocalSaveMaintenance _saveMaintenance;
    private ILocalSaveStore _localSaveStore;
    private bool _useLearningSaveData;
    private string _saveRoot;

    private TextAsset _chapterJson;
    private ScenarioDefinition _scenarioDef;

    [Header("예제")]
    [Tooltip("연출 단독: 진행·저장 없이 아래 노드 하나를 바로 재생한다.\n" +
             "진행·저장: 타이틀 → 챕터 진행 → 장면 경계 저장.")]
    [SerializeField] private SampleExample example = SampleExample.ProgressionAndSave;

    [Tooltip("연출 단독 예제가 재생할 Yarn 노드 이름(title:).")]
    [SerializeField] private string presentationEntryNode = "SC_EP01";

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

    private bool IsPresentationOnly => example == SampleExample.PresentationOnly;

    private void Awake()
    {
        // 진행·저장 예제는 진행 데이터를 먼저 확인한다 — 없으면 아무것도 세우지 않는다.
        if (!IsPresentationOnly && !TryBootstrapProgressionData())
            return;

        _screenBindings = new VNScreenBindings(uiManager);

        _runtime = PresentationAssembler.Build(
            CreatePresentationSceneRefs(),
            _screenBindings);

        IPlaybackHost host = IsPresentationOnly
            ? CreateNodePlaybackHost()
            : InstallProgression();

        _runtime.AttachHost(host);

        _screenBindings.ConfigurePlayback(
            _runtime.Features,
            _runtime.AdvanceDispatcher,
            host);

        if (!IsPresentationOnly)
            ConfigureProgressionScreens();
    }

    private PresentationSceneRefs CreatePresentationSceneRefs()
    {
        return new PresentationSceneRefs
        {
            UIManager = uiManager,
            AudioSystem = audioSystem,

            SurfaceLayoutPresetDb = surfaceLayoutPresetDb,
            SpeakerPolicyDb = speakerPolicyDb,

            RoleTuningDb = roleTuningDb,
            PortraitGeneratedDb = portraitGeneratedDb,
            CharacterFocusTuningDb = characterFocusTuningDb,
            CharacterVisualFocusPresetDb = characterVisualFocusPresetDb,
            CharacterDepthTuning = characterDepthTuning,

            CommandExecutor = commandExecutor,

            DialogueRunner = dialogueRunner,
            CustomLinePresenter = customLinePresenter,
            EllipsisBreathTypewriter = ellipsisBreathTypewriter,
            AutoAdvanceScheduler = autoAdvanceScheduler,
            OptionsPresenter = vnOptionsPresenter,
            OptionItem = optionItem,

            InputPoller = vnAdvanceInputPoller,

            CharacterRigPrefab = rigPrefab,
            BackgroundRigPrefab = backgroundRigPrefab,

            ScreenNoisePresetDb = screenNoisePresetDb,
            ScreenVignettePresetDb = screenVignettePresetDb,
            ScreenFlashPresetDb = screenFlashPresetDb,
            StageMaskMotionPresetDb = stageMaskMotionPresetDb,
            ScreenEffectRigMount = screenEffectRigMount,
            ScreenEffectRigPrefab = screenEffectRigPrefab,

            CharacterFocusDebugView = characterFocusDebugView,
            EnableEquivalenceHarness = enableEquivalenceHarness,
            HarnessParent = transform,
        };
    }

    #region 연출 단독

    private IPlaybackHost CreateNodePlaybackHost()
    {
        _nodePlaybackHost = new NodePlaybackHost(
            _runtime.ScenePresentation,
            _runtime.Backlog,
            _runtime.RollbackHistory);

        return _nodePlaybackHost;
    }

    private async Task PlayEntryNodeAsync()
    {
        try
        {
            await _nodePlaybackHost.PlayAsync(presentationEntryNode);
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
        }
    }

    #endregion

    #region 진행·저장

    private bool TryBootstrapProgressionData()
    {
        // 실행 중 Inspector를 바꿔도 저장 경로와 기능 구성이 서로 엇갈리지 않는다.
        _useLearningSaveData = useLearningSaveData;

        _saveRoot = Path.Combine(
            Application.persistentDataPath,
            _useLearningSaveData ? "saves-learning" : "saves");

        _chapterJson =
            _useLearningSaveData
                ? learningChapterJson
                : progressionChapterJson;

        if (_chapterJson == null)
        {
            Debug.LogError(
                _useLearningSaveData
                    ? "[학습] Learning Chapter Json을 지정해야 한다."
                    : "[진행] Progression Chapter Json을 지정해야 한다.");

            enabled = false;
            return false;
        }

        if (string.IsNullOrWhiteSpace(saveContentVersion))
        {
            Debug.LogError("[저장] Save Content Version을 지정해야 한다.");

            enabled = false;
            return false;
        }

        _scenarioDef =
            ProgressionContentLoader.LoadSingleChapter(_chapterJson);

        if (_scenarioDef == null)
        {
            enabled = false;
            return false;
        }

        return true;
    }

    private IPlaybackHost InstallProgression()
    {
        _progressionOptions = new ChapterOptionsView(
            uiManager.GetUI<VNDefaultOptionsPanel>(),
            optionItem);

        CreateSaveServices();

        // 진행 런타임은 아래 계약만 안다.
        // Stage / Save로 잇는 일은 전부 이 자리에서 끝난다.
        ProgressionReplayState replayState = new(
            _runtime.LineState,
            _runtime.ChoiceHistory);

        // Progression은 IScenePersistence까지만 안다.
        // Bridge가 Host 상태를 캡처하고 SaveCoordinator는 저장 정책만 수행한다.
        ProgressionSaveBridge savePersistence = new(
            _saveCoordinator,
            _runtime.Backlog,
            _runtime.ChoiceHistory);

        SceneRunner sceneRunner = new SceneRunner(
            new ScenePresentationProgressionAdapter(_runtime.ScenePresentation),
            _progressionOptions,
            replayState,
            _runtime.RollbackHistory,
            savePersistence,
            _runtime.Backlog);

        _progressionDriver = new ProgressionDriver(sceneRunner);

        _progressionLauncher = new ProgressionLauncher(
            _progressionDriver,
            _scenarioDef,
            _saveCoordinator,
            _runtime.Backlog,
            replayState);

        // 대사가 스탯을 읽는 길. 드라이버가 선 뒤에 등록해야 람다가 null을 잡지 않는다.
        new ProgressionStatFunction(() => _progressionDriver.CurrentState)
            .Register(dialogueRunner);

        return _progressionLauncher;
    }

    // 모든 실행 모드에서 로컬 저장을 기본으로 사용한다.
    //
    // 네 책임이 같은 저장소를 공유한다.
    // - Coordinator: 지금 회차의 저장 세션과 Scene 경계
    // - SlotService: 수동 슬롯
    // - ForkService: 과거 지점에서 새 회차 만들기
    // - Maintenance: 참조가 끝난 파일 정리
    private void CreateSaveServices()
    {
        _localSaveStore = new LocalFileSaveStore(_saveRoot);

        _saveCoordinator = new SaveCoordinator(
            _localSaveStore,
            saveContentVersion);

        _saveSlotService = new SaveSlotService(
            _localSaveStore,
            saveContentVersion);

        _playthroughForkService = new PlaythroughForkService(
            _localSaveStore,
            saveContentVersion);

        _saveMaintenance = new LocalSaveMaintenance(_localSaveStore);
    }

    private void ConfigureProgressionScreens()
    {
        _activeResumeSaveFlow = new(
            _saveCoordinator,
            _progressionLauncher,
            _runtime.Features);

        _runtime.LineFlow.LineDisplayed += _activeResumeSaveFlow.Update;

        ManualSaveFlow manualSaveFlow = new(
            _saveCoordinator,
            _saveSlotService,
            _playthroughForkService,
            _progressionLauncher,
            _runtime.Features);

        _screenBindings.ConfigureProgression(
            _progressionLauncher,
            _saveSlotService,
            manualSaveFlow);
    }

    #endregion

    private void Start()
    {
        if (IsPresentationOnly)
        {
            _ = PlayEntryNodeAsync();
            return;
        }

        _screenBindings.OpenTitleMenu();
    }

    private void Update()
    {
        _saveMaintenance?.Tick(Time.realtimeSinceStartup);
    }

    [ContextMenu("Save/Log current snapshot")]
    private void LogSaveSnapshot()
    {
        if (!Application.isPlaying || _localSaveStore == null)
        {
            Debug.Log("[저장] 진행·저장 예제를 실행한 뒤 저장 snapshot을 확인할 수 있다.");
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
