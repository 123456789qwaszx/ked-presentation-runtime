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
    // 무대 · 사운드
    public PresentationUIRoot Stage;
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

    // 대사창 · 선택지 상자 — 화면 쪽(새 예제 UI, KedUI)의 구현. 연출 런타임은 계약만 안다.
    public IPresentationDialogueBoxView DialogueBoxView;
    public IPresentationOptionsBoxView OptionsBoxView;

    // 리그 — 배경도 캐릭터 리그다(캐스팅하지 않은 슬롯)
    public RectTransform CharacterRigPrefab;

    // 작가 자산 — 대본 번들 곁의 curves.json · motions.json. TextAsset이라 빌드에서도 읽힌다.
    // 안 꽂혀 있으면 조립기가 에디터에서만 대본 폴더의 파일을 읽고(옛 경로), 빌드에서는 0개로 간다.
    public TextAsset EaseCurvesJson;
    public TextAsset MotionsJson;

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
