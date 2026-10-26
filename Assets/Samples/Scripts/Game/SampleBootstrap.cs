using System;
using System.IO;
using Ked.Progression;
using Ked.Progression.Save;
using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

/// <summary>
/// 예제 씬의 진입점. 연출 런타임을 세우고, 고른 예제에 맞는 재생 호스트를 하나 붙인다.
/// 두 예제 모두 타이틀([처음부터] [이어하기])에서 시작한다. 예제는 인스펙터의 '예제' 칸이 기본이고,
/// 타이틀의 [다른 예제로]를 누르면 그 예제로 씬을 다시 연다(SampleExampleSwitch).
///
/// - 연출 단독: 진행 없이 Yarn 노드 하나(NodePlaybackHost). 이어하기는 연출 기억만으로 —
///   노드 이름과 PlaybackMemory의 문자열을 파일 하나에 둔다(NodePlaythrough).
/// - 진행·저장: 챕터 진행 → 장면 경계 저장 → 슬롯·갈라지기(ProgressionLauncher).
///   같은 연출 기억 문자열을 진행 저장의 HostState 칸에 둔다.
///
/// 연출 런타임의 조립은 PresentationAssembler가 한다. 여기는 씬 참조를 넘기고,
/// 그 위에 고른 예제의 호스트와 저장을 얹는다.
///
/// 스크립트 GUID는 예전 VNAppBootstrap 그대로다 — 씬에 직렬화된 값이 유지된다.
///
/// 리그 프리팹은 IRigPrefabSource로도 내준다 — 본체의 튜닝 덤프 도구가 이 계약으로 찾는다.
/// </summary>
public class SampleBootstrap : MonoBehaviour, IRigPrefabSource
{
    RectTransform IRigPrefabSource.CharacterRigPrefab => rigPrefab;
    RectTransform IRigPrefabSource.BackgroundRigPrefab => backgroundRigPrefab;

    public enum SampleExample
    {
        ProgressionAndSave = 0,
        PresentationOnly = 1,
    }

    private VNScreenBindings _screenBindings;
    private PresentationRuntime _runtime;

    // 연출 단독
    private NodePlaybackHost _nodePlaybackHost;
    private NodePlaythrough _nodePlaythrough;

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
    [Tooltip("연출 단독: 진행 없이 아래 노드 하나를 재생한다. 이어하기는 연출 기억만으로.\n" +
             "진행·저장: 챕터 진행 → 장면 경계 저장 → 슬롯·갈라지기.\n" +
             "둘 다 타이틀에서 시작한다.")]
    [SerializeField] private SampleExample example = SampleExample.ProgressionAndSave;

    [Tooltip("연출 단독 예제가 재생할 Yarn 노드 이름(title:). 기본은 연출 데모(Assets/@Dialogue/Demo_Presentation.yarn).")]
    [SerializeField] private string presentationEntryNode = "Demo_Presentation";

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

    // 이번 조립의 예제. 인스펙터 값 또는 타이틀에서 고른 것(SampleExampleSwitch).
    private SampleExample _example;

    private bool IsPresentationOnly => _example == SampleExample.PresentationOnly;

    private void Awake()
    {
        _example = SampleExampleSwitch.Resolve(example);

        // 진행·저장 예제는 진행 데이터를 먼저 확인한다 — 없으면 아무것도 세우지 않는다.
        if (!IsPresentationOnly && !TryBootstrapProgressionData())
            return;

        _screenBindings = new VNScreenBindings(uiManager);

        _runtime = PresentationAssembler.Build(
            CreatePresentationSceneRefs(),
            _screenBindings);

        IPlaybackHost host = IsPresentationOnly
            ? InstallPresentationOnly()
            : InstallProgression();

        _runtime.AttachHost(host);

        _screenBindings.ConfigurePlayback(
            _runtime.Features,
            _runtime.AdvanceDispatcher,
            host);

        if (IsPresentationOnly)
            _screenBindings.ConfigureTitle(_nodePlaythrough);
        else
            ConfigureProgressionScreens();

        SampleExample other = SampleExampleSwitch.Other(_example);

        _screenBindings.ConfigureExampleSwitch(
            SampleExampleSwitch.DisplayName(_example),
            SampleExampleSwitch.DisplayName(other),
            () => SampleExampleSwitch.SwitchTo(other));
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

    // 진행도 진행 저장도 없다. 이어하기 파일은 예제가 직접 쓴다 — 저장 위치는 런타임이 정하지 않는다.
    private IPlaybackHost InstallPresentationOnly()
    {
        _nodePlaybackHost = new NodePlaybackHost(_runtime.Unit);

        _nodePlaythrough = new NodePlaythrough(
            _nodePlaybackHost,
            _runtime.Memory,
            presentationEntryNode,
            Path.Combine(Application.persistentDataPath, "presentation-only", "resume.json"));

        _runtime.LineFlow.LineDisplayed += _nodePlaythrough.SaveCurrentLine;

        return _nodePlaybackHost;
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

        // 진행 런타임은 아래 계약만 안다. 연출 쪽에서 쓰는 것은 재생 단위(Unit)와 연출 기억(Memory) 둘이다.
        // Progression은 IScenePersistence까지만 안다.
        // Bridge가 연출 기억을 문자열로 꺼내 싣고 SaveCoordinator는 저장 정책만 수행한다.
        ProgressionSaveBridge savePersistence = new(
            _saveCoordinator,
            _runtime.Memory);

        SceneRunner sceneRunner = new SceneRunner(
            _runtime.Unit,
            _progressionOptions,
            savePersistence);

        _progressionDriver = new ProgressionDriver(sceneRunner);

        _progressionLauncher = new ProgressionLauncher(
            _progressionDriver,
            _scenarioDef,
            _saveCoordinator,
            _runtime.Memory);

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
        // 저장 계층은 엔진을 모른다. 시계와 로그를 여기서 꽂는다.
        UnityProgressionLog log = new();
        Func<double> clock = () => Time.realtimeSinceStartupAsDouble;

        _localSaveStore = new LocalFileSaveStore(_saveRoot);

        _saveCoordinator = new SaveCoordinator(
            _localSaveStore,
            saveContentVersion,
            clock,
            log);

        _saveSlotService = new SaveSlotService(
            _localSaveStore,
            saveContentVersion,
            log);

        _playthroughForkService = new PlaythroughForkService(
            _localSaveStore,
            saveContentVersion,
            log);

        _saveMaintenance = new LocalSaveMaintenance(_localSaveStore, log);
    }

    private void ConfigureProgressionScreens()
    {
        _activeResumeSaveFlow = new(
            _saveCoordinator,
            _progressionLauncher,
            _runtime.Memory);

        _runtime.LineFlow.LineDisplayed += _activeResumeSaveFlow.Update;

        ManualSaveFlow manualSaveFlow = new(
            _saveCoordinator,
            _saveSlotService,
            _playthroughForkService,
            _progressionLauncher,
            _runtime.Memory);

        _screenBindings.ConfigureTitle(_progressionLauncher);

        _screenBindings.ConfigureSaveSlots(
            _saveSlotService,
            manualSaveFlow);
    }

    #endregion

    private void Start()
    {
        _screenBindings.OpenTitleMenu();
    }

    private void Update()
    {
        _saveMaintenance?.Tick(Time.realtimeSinceStartupAsDouble);
    }

    [ContextMenu("Save/Log current snapshot")]
    private void LogSaveSnapshot()
    {
        if (!Application.isPlaying || _localSaveStore == null)
        {
            Debug.Log("[저장] 진행·저장 예제를 실행한 뒤 저장 snapshot을 확인할 수 있다.");
            return;
        }

        PlaythroughSave snapshot = _localSaveStore.LoadActive();

        if (snapshot == null)
        {
            Debug.Log("[저장] 아직 첫 장면에 진입하지 않아 저장된 회차가 없다.");
            return;
        }

        Debug.Log($"[저장] 현재 확정 snapshot (로컬 envelope 제외)\n{SaveJson.SerializePretty(snapshot)}");
    }
}
