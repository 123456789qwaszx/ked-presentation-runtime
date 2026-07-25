// using UnityEngine;
// using Yarn.Unity;
//
// /// <summary>
// /// 게스트하우스 시스템의 합성 루트.
// ///
// /// 씬에는 이 컴포넌트 하나만 두고, 콘텐츠 번들과 DialogueRunner, 화면 바인딩을 연결한다.
// /// 상위(에피소드 플레이어, 세이브 시스템)에서는 CampaignFlow 만 잡으면 된다.
// /// </summary>
// public sealed class GuesthouseRuntime : MonoBehaviour
// {
//     [Header("Content")]
//     [SerializeField] private GuesthouseContentBundleSO contentBundle;
//
//     [Header("Dialogue")]
//     [SerializeField] private DialogueRunner dialogueRunner;
//
//     [Header("Boot")]
//     [SerializeField] private bool runOnStart;
//
//     public GuesthouseContentDB Content { get; private set; }
//     public CampaignFlow Campaign { get; private set; }
//     public ServiceSessionFlow Session { get; private set; }
//
//     private IGuesthouseScreenBindings _screens;
//     private bool _isComposed;
//
//     /// <summary>
//     /// 화면 바인딩을 주입한다. VnScreenBindings 가 살아 있는 시점에 한 번 호출한다.
//     /// 주입 전에 Compose 가 호출되면 헤드리스 더미로 대체된다.
//     /// </summary>
//     public void ConfigureScreens(IGuesthouseScreenBindings screens)
//     {
//         _screens = screens;
//
//         if (_isComposed)
//             Compose();
//     }
//
//     private void Start()
//     {
//         if (!_isComposed)
//             Compose();
//
//         if (runOnStart)
//             RunCampaign();
//     }
//
//     public void Compose()
//     {
//         Content = contentBundle != null
//             ? contentBundle.BuildContentDB()
//             : GuesthouseDemoContent.Build();
//
//         IGuesthouseScreenBindings screens = _screens ?? new HeadlessGuesthouseScreens();
//
//         GuesthousePresentationPort port = new(
//             new ScenarioNodeRunner(dialogueRunner),
//             screens,
//             ResolveYarnContext());
//
//         Session = new ServiceSessionFlow(Content, port);
//
//         NightPhaseFlow nightFlow = new(Content, port);
//
//         DayCycleFlow dayFlow = new(
//             Content,
//             new RotatingBookingPlanner(Content),
//             Session,
//             nightFlow,
//             port);
//
//         Campaign = new CampaignFlow(Content, dayFlow, port);
//
//         _isComposed = true;
//     }
//
//     /// <summary>
//     /// 대본이 참조할 표시용 변수를 밀어 넣을 통로를 만든다.
//     /// 변수 저장소가 없으면 null 을 반환하고, 이 경우 문맥 주입만 생략된다.
//     /// </summary>
//     private GuesthouseYarnContext ResolveYarnContext()
//     {
//         if (dialogueRunner == null || dialogueRunner.VariableStorage == null)
//             return null;
//
//         return new GuesthouseYarnContext(dialogueRunner.VariableStorage);
//     }
//
//     public async void RunCampaign()
//     {
//         if (!_isComposed)
//             Compose();
//
//         Campaign.BeginCampaign();
//
//         CampaignEndingResult ending = await Campaign.RunAsync();
//
//         Debug.Log($"[GuesthouseRuntime] Ending={ending.EndingKey} ({ending.Title}) : {ending.Reason}");
//     }
//
//     private void OnDestroy()
//     {
//         Session?.Invalidate();
//     }
// }
