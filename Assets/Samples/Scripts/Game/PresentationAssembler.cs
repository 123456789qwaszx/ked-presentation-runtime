using System;
using UnityEngine;

/// <summary>
/// 연출 런타임 조립. 씬 참조를 받아 무대·명령·Yarn·대사 표시·재생 제어를 잇는다.
///
/// 진행·저장을 모른다. 결과(PresentationRuntime)에 재생 호스트(ProgressionLauncher)를 붙이면 돈다.
///
/// 조립 순서와 내용은 VNAppBootstrap.Awake의 연출 쪽을 그대로 옮긴 것이다.
/// </summary>
public sealed class PresentationAssembler
{
    /// <summary>예제 대본 번들 폴더(Assets/Samples/Dialogue). 커브(curves.json)·모션(motions.json)도 대본 곁에 산다.</summary>
    private static string DialogueFolder =>
        System.IO.Path.Combine(Application.dataPath, "Samples", "Dialogue");

    private MotionLibrary _motions = MotionLibrary.Empty;

    private readonly PresentationSceneRefs _refs;
    private readonly IPresentationView _presentationView;

    private readonly PlaybackRuntimeState _playbackState = new();

    private readonly RollbackHistory _rollbackHistory = new();
    private readonly ChoiceHistory _choiceHistory = new();
    private readonly SeekState _seekState = new();
    private readonly LineDisplayState _lineDisplayState = new();

    private readonly PresentationStage _presentationStage = new();
    private readonly DialogueSurfaceState _dialogueSurfaceState = new();
    private readonly BacklogRecorder _backlogRecorder = new();

    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher = new();
    private readonly EpisodeSkipController _episodeSkipController = new();

    private PresentationUIRoot _presentationUIRoot;
    private PresentationShotResponseSystem _presentationResponseRig;
    private ScreenEffectRig _screenEffectRig;

    private PresentationScopeSession _presentationScopeSession;
    private PlaybackFeatureController _playbackFeatureController;
    private LinePresentationFlow _linePresentationFlow;
    private LineVoiceController _lineVoice;
    private SceneBgmController _sceneBgm;
    private RigBlurIntentSystem _rigBlur;
    private DialogueBoxPresentationController _dialogueBoxPresentation;
    private ScenePresentation _scenePresentation;
    private YarnPlaybackDriver _yarnPlaybackDriver;

    private PresentationAssembler(
        PresentationSceneRefs refs,
        IPresentationView presentationView)
    {
        _refs = refs;
        _presentationView = presentationView;
    }

    /// <param name="presentationView">재생을 시작할 때 무대 화면을 앞으로 가져올 자리.</param>
    public static PresentationRuntime Build(
        PresentationSceneRefs refs,
        IPresentationView presentationView)
    {
        return Build(refs, presentationView, out _);
    }

    /// <param name="internals">예제 UI가 읽는 조립 안쪽 상태(재생 모드). 연출 런타임은 내놓지 않는 것이라 여기서 건넨다.</param>
    public static PresentationRuntime Build(
        PresentationSceneRefs refs,
        IPresentationView presentationView,
        out PresentationInternals internals)
    {
        var assembler = new PresentationAssembler(refs, presentationView);
        PresentationRuntime runtime = assembler.Assemble();

        internals = new PresentationInternals(assembler._playbackState);

        return runtime;
    }

    private PresentationRuntime Assemble()
    {
        BootstrapPresentationRoots();

        BootstrapAudioSystem();

        BootstrapPresentationSession();

        BootstrapYarn();

        CreateScenePresentation();

        BootstrapPlaybackControls();

        BootstrapEquivalenceHarness();

        return new PresentationRuntime(
            _scenePresentation,
            _playbackFeatureController,
            _dialogueAdvanceDispatcher,
            _linePresentationFlow,
            _rollbackHistory,
            _choiceHistory,
            _backlogRecorder,
            _refs.InputPoller);
    }

    private void BootstrapPresentationRoots()
    {
        _presentationUIRoot = _refs.Stage
            ? _refs.Stage
            : throw new InvalidOperationException("[연출 조립] 무대(PresentationUIRoot)가 없다 — 부트스트랩의 '무대' 칸을 채운다.");

        _presentationUIRoot.Initialize();

        IShotResponseStageProvider shotResponseStageProvider = _presentationUIRoot;

        _presentationResponseRig = new PresentationShotResponseSystem(shotResponseStageProvider);
        _screenEffectRig = EnsureScreenEffectRig();

        _refs.CharacterFocusDebugView.Initialize(
            _presentationStage,
            shotResponseStageProvider,
            _refs.CharacterFocusTuningDb);
    }

    private ScreenEffectRig EnsureScreenEffectRig()
    {
        ScreenEffectRig screenEffectRig = _refs.ScreenEffectRigMount.GetComponentInChildren<ScreenEffectRig>(true);

        if (screenEffectRig == null)
        {
            ScreenEffectRigBuilder screenEffectRigBuilder = new();

            RectTransform rigRoot = screenEffectRigBuilder.BuildRigRoot(
                _refs.ScreenEffectRigPrefab);

            rigRoot.SetParent(_refs.ScreenEffectRigMount, false);

            if (!rigRoot.TryGetComponent(out screenEffectRig))
                screenEffectRig = rigRoot.gameObject.AddComponent<ScreenEffectRig>();
        }

        screenEffectRig.Initialize();

        return screenEffectRig;
    }

    private void BootstrapAudioSystem()
    {
        _refs.AudioSystem.Initialize();
    }

    private void BootstrapPresentationSession()
    {
        // 블러 의도(focus_on·screen_blur) — 커맨드는 의도만 넘기고, 줄 머리에서 시스템이 리그를 다시 맞춘다.
        _rigBlur = new RigBlurIntentSystem(_presentationStage, _presentationUIRoot);

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new(_presentationUIRoot);
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(_refs.PortraitGeneratedDb);

        CharacterRigCommandFactory charRigFactory = new(
            _presentationStage,
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            _refs.RoleTuningDb,
            _refs.CharacterFocusTuningDb,
            _refs.CharacterVisualFocusPresetDb,
            _refs.CharacterDepthTuning,
            _presentationUIRoot,
            _rigBlur);

        ShotResponseCommandFactory presentationShotFactory = new(
            _presentationStage, _presentationResponseRig, _refs.CharacterFocusTuningDb, _presentationUIRoot);

        // Presentation Control
        PresentationControlCommandFactory presentationControlFactory = new(_presentationStage);

        // Audio
        AudioCommandFactory audioFactory = new(_refs.AudioSystem);

        ScreenEffectCommandFactory screenEffectFactory = new(
            _presentationStage,
            _screenEffectRig,
            _refs.ScreenFlashPresetDb,
            _refs.ScreenNoisePresetDb,
            _refs.ScreenVignettePresetDb,
            _refs.StageMaskMotionPresetDb,
            _presentationUIRoot,
            _rigBlur);

        CompositeCommandFactory factory = new(
            charRigFactory,
            presentationShotFactory,
            presentationControlFactory,
            audioFactory,
            screenEffectFactory);

        _refs.CommandExecutor.Initialize(factory);

        PresentationSessionContext pacePolicy = new(_playbackState, _seekState);

        _presentationScopeSession = new(
            _refs.CommandExecutor,
            pacePolicy);
    }

    private void BootstrapYarn()
    {
        YarnPlaybackDriver yarnPlaybackDriver = new(_refs.CommandExecutor, _presentationScopeSession);
        _yarnPlaybackDriver = yarnPlaybackDriver;
        yarnPlaybackDriver.LineHeadPlayed += _rigBlur.Reconcile;

        // 대사창 · 선택지 상자는 화면 쪽(새 예제 UI)이 넘긴다.
        IPresentationDialogueBoxView dialogueSurfaceBox = _refs.DialogueBoxView
            ?? throw new InvalidOperationException("[연출 조립] 대사창(DialogueBoxView)이 없다.");

        DialogueBoxCurrentState dialogueBoxState = new();

        DialogueBoxPresentationController dialogueBoxPresentationController = new(
            dialogueBoxState,
            dialogueSurfaceBox,
            _dialogueSurfaceState,
            _refs.SurfaceLayoutPresetDb,
            _refs.SpeakerPolicyDb);
        _dialogueBoxPresentation = dialogueBoxPresentationController;

        EaseCurveLibrary easeCurves = LoadEaseCurves();
        _motions = LoadMotions();

        // 배경(bg_*)도 캐릭터 리그로 선다 — 캐스팅하지 않은 슬롯. 리그 프리팹은 하나다.
        StageCommandParser stageCommandParser = new(
            _refs.CharacterRigPrefab,
            easeCurves,
            _motions);

        _ = new YarnCommandBridge(
            _refs.DialogueRunner,
            yarnPlaybackDriver,
            stageCommandParser,
            dialogueBoxPresentationController);

        YarnLineBoundary yarnLineBoundary = new(
            _backlogRecorder,
            _rollbackHistory);

        LineHurrySpeedController lineHurrySpeed = new(_refs.EllipsisBreathTypewriter);
        _lineVoice = new LineVoiceController(_refs.AudioSystem.Voice);
        _sceneBgm = new SceneBgmController(_refs.AudioSystem.Bgm);

        _linePresentationFlow = new LinePresentationFlow(
            yarnLineBoundary,
            _lineDisplayState,
            _seekState,
            dialogueBoxPresentationController,
            _refs.EllipsisBreathTypewriter,
            yarnPlaybackDriver,
            lineHurrySpeed,
            _lineVoice,
            _sceneBgm);

        _refs.CustomLinePresenter.Initialize(
            _refs.DialogueRunner,
            _linePresentationFlow,
            _refs.EllipsisBreathTypewriter,
            _playbackState);

        ChoiceBoundary choiceBoundary = new(
            _choiceHistory,
            _rollbackHistory);

        IPresentationOptionsBoxView optionsBox = _refs.OptionsBoxView
            ?? throw new InvalidOperationException("[연출 조립] 선택지 상자(OptionsBoxView)가 없다.");

        OptionsBoxPresentationController optionsBoxPresentation = new(optionsBox);

        OptionsPresentationFlow optionsPresentationFlow = new(
            optionsBoxPresentation,
            choiceBoundary,
            _seekState);

        _refs.OptionsPresenter.Initialize(
            optionsPresentationFlow,
            _refs.OptionItem);
    }

    private void CreateScenePresentation()
    {
        IEpisodeNodeRunner nodeRunner =
            new YarnEpisodeNodeRunner(_refs.DialogueRunner);

        _scenePresentation = new ScenePresentation(
            nodeRunner,
            _presentationView,
            _rollbackHistory,
            _refs.CustomLinePresenter,
            _presentationResponseRig,
            _rigBlur,
            _screenEffectRig,
            _presentationUIRoot,
            _refs.AudioSystem,
            _sceneBgm,
            _presentationStage,
            _dialogueSurfaceState,
            _dialogueBoxPresentation,
            _presentationScopeSession,
            _choiceHistory,
            _episodeSkipController,
            _seekState,
            _yarnPlaybackDriver);
    }

    // 입력 폴러의 연결은 여기서 하지 않는다 — 재생 호스트가 정해진 뒤 PresentationRuntime.AttachHost가 한다.
    private void BootstrapPlaybackControls()
    {
        _refs.AutoAdvanceScheduler.Initialize(
            _playbackState,
            _dialogueAdvanceDispatcher,
            _lineVoice,
            () => Time.unscaledTimeAsDouble);

        RapidSkipController rapidSkipController = new(
            _dialogueAdvanceDispatcher);

        _episodeSkipController.Initialize(
            _dialogueAdvanceDispatcher);

        _playbackFeatureController = new PlaybackFeatureController(
            _playbackState,
            _lineDisplayState,
            _seekState,
            _refs.EllipsisBreathTypewriter,
            _backlogRecorder,
            _refs.AutoAdvanceScheduler,
            rapidSkipController,
            _episodeSkipController,
            _rollbackHistory,
            _choiceHistory,
            _lineVoice);

        AdvanceGate advanceGate = new(
            _playbackState,
            _seekState,
            _presentationScopeSession);

        _dialogueAdvanceDispatcher.Initialize(
            advanceGate,
            _refs.DialogueRunner,
            _lineDisplayState);
    }

    private void BootstrapEquivalenceHarness()
    {
        if (!_refs.EnableEquivalenceHarness)
            return;

        GameObject harnessGo = new("StageEquivalenceHarness");
        harnessGo.transform.SetParent(_refs.HarnessParent, false);

        StageEquivalenceHarness harness = harnessGo.AddComponent<StageEquivalenceHarness>();

        harness.Initialize(
            _rollbackHistory,
            _refs.DialogueRunner,
            _presentationScopeSession,
            _presentationStage,
            _presentationResponseRig,
            _dialogueAdvanceDispatcher,
            _motions,
            DialogueFolder);
    }

    // ── 작가 자산 — TextAsset이 정본이다(빌드에서도 읽힌다). 안 꽂혀 있으면 에디터에서만 옛 경로의 파일을 읽는다 ──

    private EaseCurveLibrary LoadEaseCurves()
    {
        if (_refs.EaseCurvesJson != null)
            return EaseCurveLibrary.Parse(_refs.EaseCurvesJson.text, _refs.EaseCurvesJson.name);

        string json = ReadBundleFileEditorOnly(EaseCurveLibrary.BundleFileName);
        return json == null ? EaseCurveLibrary.Empty : EaseCurveLibrary.Parse(json, EaseCurveLibrary.BundleFileName);
    }

    private MotionLibrary LoadMotions()
    {
        string[] nodeIds = CharacterRigSchema.NodeIds();

        if (_refs.MotionsJson != null)
            return MotionLibrary.Parse(_refs.MotionsJson.text, _refs.MotionsJson.name, nodeIds);

        string json = ReadBundleFileEditorOnly(MotionLibrary.BundleFileName);
        return json == null ? MotionLibrary.Empty : MotionLibrary.Parse(json, MotionLibrary.BundleFileName, nodeIds);
    }

    /// <summary>
    /// 인스펙터에 TextAsset이 없을 때의 길. 에디터에서는 대본 폴더의 파일을 읽어 주되 소리 낸다 —
    /// 같은 코드가 빌드에서는 파일을 못 보므로(Application.dataPath는 빌드에 없다) 거기서는 0개다.
    /// 파일이 없으면 조용히 null — 자산을 안 쓰는 프로젝트가 정상 경로다.
    /// </summary>
    private static string ReadBundleFileEditorOnly(string fileName)
    {
#if UNITY_EDITOR
        string path = System.IO.Path.Combine(DialogueFolder, fileName);

        if (!System.IO.File.Exists(path))
            return null;

        Debug.LogWarning(
            $"[연출 조립] {fileName}을 파일 경로로 읽었다 — 빌드에서는 읽히지 않는다. " +
            "SampleBootstrap의 '작가 자산'에 TextAsset을 꽂을 것.");

        return System.IO.File.ReadAllText(path);
#else
        return null;
#endif
    }
}

/// <summary>
/// 조립 안쪽에서 예제 UI(KedUI)가 읽는 것.
/// - PlaybackState: 자동·빨리 넘기기의 켜짐(퀵메뉴 표시)과 재생 설정(자동 대기·빠르게 재생 배속).
/// 연출 런타임(PresentationRuntime)의 공개 면을 넓히지 않으려고 예제 조립이 예제 층 안에서만 건넨다.
/// </summary>
public sealed class PresentationInternals
{
    public PlaybackRuntimeState PlaybackState { get; }

    public PresentationInternals(PlaybackRuntimeState playbackState)
    {
        PlaybackState = playbackState;
    }
}
