using UnityEngine;
using Yarn.Unity;

/// <summary>
/// 연출 런타임을 세우는 데 필요한 씬 참조와 데이터 에셋.
///
/// 부트스트랩이 인스펙터 값으로 채워 PresentationAssembler에 넘긴다.
/// 진행·저장 쪽 참조는 여기 없다 — 연출 런타임은 그것을 모른다.
/// </summary>
public sealed class PresentationSceneRefs
{
    // UI · 사운드
    public UIManager UIManager;
    public AudioSystem AudioSystem;

    // 대사창
    public DialogueSurfaceLayoutPresetDBSO SurfaceLayoutPresetDb;
    public DialogueSpeakerPresentationPolicyDBSO SpeakerPolicyDb;

    // 캐릭터 연출
    public RoleAnchorTuningDBSO RoleTuningDb;
    public PortraitGeneratedDBSO PortraitGeneratedDb;
    public CharacterFocusTuningDBSO CharacterFocusTuningDb;
    public CharacterVisualFocusPresetDBSO CharacterVisualFocusPresetDb;
    public CharacterDepthTuningSO CharacterDepthTuning;

    // 명령 실행
    public CommandExecutor CommandExecutor;

    // Yarn · 대사 표시
    public DialogueRunner DialogueRunner;
    public CustomLinePresenter CustomLinePresenter;
    public EllipsisBreathTypewriter EllipsisBreathTypewriter;
    public AutoAdvanceScheduler AutoAdvanceScheduler;
    public VNOptionsPresenter OptionsPresenter;
    public VNOptionItem OptionItem;

    // 입력
    public VNAdvanceInputPoller InputPoller;

    // 리그
    public RectTransform CharacterRigPrefab;
    public RectTransform BackgroundRigPrefab;

    // 화면 효과
    public ScreenNoisePresetDBSO ScreenNoisePresetDb;
    public ScreenVignettePresetDBSO ScreenVignettePresetDb;
    public ScreenFlashPresetDBSO ScreenFlashPresetDb;
    public StageMaskMotionPresetDBSO StageMaskMotionPresetDb;
    public RectTransform ScreenEffectRigMount;
    public RectTransform ScreenEffectRigPrefab;

    // 진단
    public CharacterFocusDebugView CharacterFocusDebugView;
    public bool EnableEquivalenceHarness;
    public Transform HarnessParent;
}
