using System;
using UnityEngine;

// 연출 런타임 조립 — 무대 · 명령 · Yarn · 대사 표시 · 재생 제어를 잇는다(옛 VNAppBootstrap.Awake의 연출 쪽).
// 진행 · 저장을 모른다. 결과(PresentationRuntime)에 재생 호스트를 붙이면 돈다(Awake → InstallProgression).
public partial class SampleBootstrap
{
    /// <summary>예제 대본 번들 폴더(Assets/Samples/Dialogue). 커브(curves.json)·모션(motions.json)도 대본 곁에 산다.</summary>
    private static string DialogueFolder =>
        System.IO.Path.Combine(Application.dataPath, "Samples", "Dialogue");

    private MotionLibrary _motions = MotionLibrary.Empty;

    private readonly PlaybackRuntimeState _playbackState = new();

    private readonly RollbackHistory _rollbackHistory = new();
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

    // 화면(_kedUI)이 먼저 서 있어야 한다 — 대사창 · 선택지 상자 · 재생 화면 전환을 화면 쪽에서 받는다.
    private PresentationRuntime AssemblePresentation()
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
            _backlogRecorder,
            vnAdvanceInputPoller);
    }

    private void BootstrapPresentationRoots()
    {
        _presentationUIRoot = stage
            ? stage
            : throw new InvalidOperationException("[연출 조립] 무대(PresentationUIRoot)가 없다 — 부트스트랩의 '무대' 칸을 채운다.");

        _presentationUIRoot.Initialize();

        IShotResponseStageProvider shotResponseStageProvider = _presentationUIRoot;

        _presentationResponseRig = new PresentationShotResponseSystem(shotResponseStageProvider);
        // 배치는 무대 좌표 — 배치 계산이 샷이 움직이는 무대 노드를 지날 때 샷 시스템의 중립값을 쓴다.
        _presentationStage.AttachStageView(_presentationResponseRig);
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
        // 블러 의도(focus_on·screen_blur) — 커맨드는 의도만 넘기고, 줄 머리에서 시스템이 리그를 다시 맞춘다.
        _rigBlur = new RigBlurIntentSystem(_presentationStage, _presentationUIRoot);

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new(_presentationUIRoot);
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(portraitGeneratedDb);

        CharacterRigCommandFactory charRigFactory = new(
            _presentationStage,
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            roleTuningDb,
            characterFocusTuningDb,
            characterVisualFocusPresetDb,
            characterDepthTuning,
            _presentationUIRoot,
            _rigBlur);

        ShotResponseCommandFactory presentationShotFactory = new(
            _presentationStage, _presentationResponseRig, characterFocusTuningDb, _presentationUIRoot);

        // Presentation Control
        PresentationControlCommandFactory presentationControlFactory = new(_presentationStage);

        // Audio
        AudioCommandFactory audioFactory = new(audioSystem);

        ScreenEffectCommandFactory screenEffectFactory = new(
            _presentationStage,
            _screenEffectRig,
            screenFlashPresetDb,
            screenNoisePresetDb,
            screenVignettePresetDb,
            stageMaskMotionPresetDb,
            _presentationUIRoot,
            _rigBlur);

        CompositeCommandFactory factory = new(
            charRigFactory,
            presentationShotFactory,
            presentationControlFactory,
            audioFactory,
            screenEffectFactory);

        commandExecutor.Initialize(factory);

        PresentationSessionContext pacePolicy = new(_playbackState, _seekState);

        _presentationScopeSession = new(
            commandExecutor,
            pacePolicy);
    }

    private void BootstrapYarn()
    {
        YarnPlaybackDriver yarnPlaybackDriver = new(commandExecutor, _presentationScopeSession);
        _yarnPlaybackDriver = yarnPlaybackDriver;
        yarnPlaybackDriver.LineHeadPlayed += _rigBlur.Reconcile;

        // 대사창 · 선택지 상자는 화면 쪽(새 예제 UI)이 넘긴다.
        IPresentationDialogueBoxView dialogueSurfaceBox = _kedUI.DialogueBox
            ?? throw new InvalidOperationException("[연출 조립] 대사창(DialogueBox)이 없다.");

        DialogueBoxCurrentState dialogueBoxState = new();

        DialogueBoxPresentationController dialogueBoxPresentationController = new(
            dialogueBoxState,
            dialogueSurfaceBox,
            _dialogueSurfaceState,
            _kedUI.SurfaceLayoutDb,
            speakerPolicyDb);
        _dialogueBoxPresentation = dialogueBoxPresentationController;

        EaseCurveLibrary easeCurves = LoadEaseCurves();
        _motions = LoadMotions();

        // 배경(bg_*)도 캐릭터 리그로 선다 — 캐스팅하지 않은 슬롯. 리그 프리팹은 하나다.
        StageCommandParser stageCommandParser = new(
            rigPrefab,
            easeCurves,
            _motions);

        _ = new YarnCommandBridge(
            dialogueRunner,
            yarnPlaybackDriver,
            stageCommandParser,
            _presentationStage,
            dialogueBoxPresentationController);

        YarnLineBoundary yarnLineBoundary = new(
            _backlogRecorder,
            _rollbackHistory);

        LineHurrySpeedController lineHurrySpeed = new(ellipsisBreathTypewriter);
        _lineVoice = new LineVoiceController(audioSystem.Voice);
        _sceneBgm = new SceneBgmController(audioSystem.Bgm);

        _linePresentationFlow = new LinePresentationFlow(
            yarnLineBoundary,
            _lineDisplayState,
            _seekState,
            dialogueBoxPresentationController,
            ellipsisBreathTypewriter,
            yarnPlaybackDriver,
            lineHurrySpeed,
            _lineVoice,
            _sceneBgm);

        customLinePresenter.Initialize(
            dialogueRunner,
            _linePresentationFlow,
            ellipsisBreathTypewriter,
            _playbackState);


        //    선택지 상자(_kedUI.OptionsBox)와 항목 프리팹은 진행 층의 ChapterOptionsView가 쓴다(InstallProgression).
    }

    private void CreateScenePresentation()
    {
        IEpisodeNodeRunner nodeRunner =
            new YarnEpisodeNodeRunner(dialogueRunner);

        _scenePresentation = new ScenePresentation(
            nodeRunner,
            _kedUI.Flow,
            _rollbackHistory,
            customLinePresenter,
            _presentationResponseRig,
            _rigBlur,
            _screenEffectRig,
            _presentationUIRoot,
            audioSystem,
            _sceneBgm,
            _presentationStage,
            _dialogueSurfaceState,
            _dialogueBoxPresentation,
            _presentationScopeSession,
            _episodeSkipController,
            _seekState,
            _yarnPlaybackDriver);
    }

    // 입력 폴러의 연결은 여기서 하지 않는다 — 재생 호스트가 정해진 뒤 PresentationRuntime.AttachHost가 한다.
    private void BootstrapPlaybackControls()
    {
        autoAdvanceScheduler.Initialize(
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
            ellipsisBreathTypewriter,
            _backlogRecorder,
            autoAdvanceScheduler,
            rapidSkipController,
            _episodeSkipController,
            _rollbackHistory,
            _lineVoice);

        AdvanceGate advanceGate = new(
            _playbackState,
            _seekState,
            _presentationScopeSession);

        _dialogueAdvanceDispatcher.Initialize(
            advanceGate,
            dialogueRunner,
            _lineDisplayState);
    }

    private void BootstrapEquivalenceHarness()
    {
        if (!enableEquivalenceHarness)
            return;

        GameObject harnessGo = new("StageEquivalenceHarness");
        harnessGo.transform.SetParent(transform, false);

        StageEquivalenceHarness harness = harnessGo.AddComponent<StageEquivalenceHarness>();

        harness.Initialize(
            _rollbackHistory,
            dialogueRunner,
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
        if (easeCurvesJson != null)
            return EaseCurveLibrary.Parse(easeCurvesJson.text, easeCurvesJson.name);

        string json = ReadBundleFileEditorOnly(EaseCurveLibrary.BundleFileName);
        return json == null ? EaseCurveLibrary.Empty : EaseCurveLibrary.Parse(json, EaseCurveLibrary.BundleFileName);
    }

    private MotionLibrary LoadMotions()
    {
        string[] nodeIds = CharacterRigSchema.NodeIds();

        if (motionsJson != null)
            return MotionLibrary.Parse(motionsJson.text, motionsJson.name, nodeIds);

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
