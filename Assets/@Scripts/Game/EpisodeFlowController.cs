// using System;
// using UnityEngine;
//
// public class EpisodeFlowController : IDisposable
// {
//     private int CHAPTERCOUNT = 6;
//     
//     private readonly UIBindingContext _ctx = new();
//     
//     private readonly PresentationViewUIBindings _dialogueUIBindings;
//     private readonly EpisodePlayer _episodePlayer;
//     
//     private EpisodePlayState _episodePlayState;
//     
//     public EpisodeFlowController(
//         PresentationViewUIBindings dialogueInput,
//         EpisodePlayer episodePlayer,
//         EpisodePlayState episodePlayState)
//     {
//         _dialogueUIBindings = dialogueInput;
//         _episodePlayer = episodePlayer;
//         _episodePlayState = episodePlayState;
//     }
//     
//     private ChapterSelectionPanel _chapterSelectionPanel;
//
//     public void OpenSelectChapterPanel()
//     {
//         UIManager.Instance.PushPanel<ChapterSelectionPanel>(panel =>
//         {
//             BindChapterSelectPanel(panel);
//             RebuildAndPresentChapterPanel(panel);
//         });
//     }
//
//     private void BindChapterSelectPanel(ChapterSelectionPanel panel)
//     {
//         if (panel == null)
//             return;
//
//         if (_chapterSelectionPanel != null && _chapterSelectionPanel != panel)
//             _ctx.Unbind(_chapterSelectionPanel);
//
//         _ctx.Unbind(panel);
//
//         _chapterSelectionPanel = panel;
//
//         BindChapterSelectPanelEvents(panel);
//     }
//
//     private void BindChapterSelectPanelEvents(ChapterSelectionPanel panel)
//     {
//         _ctx.Bind(panel,
//             p => p.OnChapterRequested += OnChapterRequested,
//             p => p.OnChapterRequested -= OnChapterRequested);
//
//         _ctx.Bind(panel,
//             p => p.OnBackRequested += CloseTopPanel,
//             p => p.OnBackRequested -= CloseTopPanel);
//     }
//     
//     private void RebuildAndPresentChapterPanel(ChapterSelectionPanel panel)
//     {
//         var models = new ChapterButtonCardModel[CHAPTERCOUNT];
//
//         for (int i = 0; i < CHAPTERCOUNT; i++)
//         {
//             int chapterId = i + 1;
//
//             models[i] = new ChapterButtonCardModel(
//                 chapterId,
//                 indexText: chapterId.ToString(),
//                 chapterIndexLabel: $"챕터 {chapterId}",
//                 chapterTitle: $"Chapter {chapterId}",
//                 episodeHeading: "",
//                 locked: true
//             );
//         }
//
//         panel.PresentChapters(models, selectedChapterId: _episodePlayState.CurrentChapterId);
//     }
//     
//     private void CloseChapterSelectPanel()
//     {
//         if (_chapterSelectionPanel != null)
//         {
//             _ctx.Unbind(_chapterSelectionPanel);
//             _chapterSelectionPanel = null;
//         }
//
//         CloseTopPanel();
//     }
//     
//     private void OnChapterRequested(int chapterId) => OpenEpisodeSelectPanel(chapterId);
//     
//     private EpisodeSelectionPanel _episodeSelectionPanel;
//     
//     
//     private void OpenEpisodeSelectPanel(int chapterId)
//     {
//         _episodePlayState.SetCurrentChapter(chapterId);
//
//         string selectedEpisodeId = "main05.02";
//         _episodePlayState.SetSelectedEpisode(selectedEpisodeId);
//
//         UIManager.Instance.PushPanel<EpisodeSelectionPanel>(panel =>
//         {
//             BindEpisodeSelectPanel(panel);
//             RebuildAndPresentEpisodeSelectionPanel(panel);
//         });
//     }
//
//     private void BindEpisodeSelectPanel(EpisodeSelectionPanel panel)
//     {
//         if (panel == null)
//             return;
//
//         if (_episodeSelectionPanel != null && _episodeSelectionPanel != panel)
//             _ctx.Unbind(_episodeSelectionPanel);
//
//         _ctx.Unbind(panel);
//
//         _episodeSelectionPanel = panel;
//
//         BindEpisodeSelectPanelEvents(panel);
//     }
//
//     private void BindEpisodeSelectPanelEvents(EpisodeSelectionPanel panel)
//     {
//         _ctx.Bind(panel,
//             p => p.OnCloseRequested += CloseEpisodeSelectPanel,
//             p => p.OnCloseRequested -= CloseEpisodeSelectPanel);
//
//         _ctx.Bind(panel,
//             p => p.SetHandlers(
//                 onMain: StartEpisodeImmediately,
//                 onBranch: HandleAttachmentRequested),
//             p => p.SetHandlers(
//                 onMain: null,
//                 onBranch: null));
//     }
//
//     private void CloseEpisodeSelectPanel()
//     {
//         if (_episodeSelectionPanel != null)
//         {
//             _ctx.Unbind(_episodeSelectionPanel);
//             _episodeSelectionPanel = null;
//         }
//
//         CloseTopPanel();
//     }
//     
//     private void StartEpisodeImmediately(string ownerEpisodeId)
//     {
//         if (string.IsNullOrEmpty(ownerEpisodeId))
//             return;
//
//         _episodePlayState.BeginMainEpisode(ownerEpisodeId);
//
//         RefreshEpisodeSelectionPanel();
//
//         UIManager.Instance.PopAllPanels();
//
//         UIManager.Instance.SwitchRoot<PresentationUIRoot>(root =>
//         {
//             _dialogueUIBindings.Bind(root);
//             _episodePlayer.StartGame(ownerEpisodeId);
//         });
//     }
//
//     private void HandleAttachmentRequested(string ownerEpisodeId, LinkKind kind, string targetEpisodeId)
//     {
//         if (string.IsNullOrEmpty(ownerEpisodeId) || string.IsNullOrEmpty(targetEpisodeId))
//             return;
//
//         _episodePlayState.BeginAttachmentEpisode(ownerEpisodeId, targetEpisodeId);
//
//         UIManager.Instance.PopAllPanels();
//
//         UIManager.Instance.SwitchRoot<PresentationUIRoot>(root =>
//         {
//             _dialogueUIBindings.Bind(root);
//             _episodePlayer.StartGame(targetEpisodeId);
//         });
//     }
//     
//     private void RebuildAndPresentEpisodeSelectionPanel(EpisodeSelectionPanel panel)
//     {
//         var chapterMeta = new ChapterMetaModel(
//             chapterIndex: $"챕터 {_episodePlayState.CurrentChapterId}",
//             eraText: "성력 996년",
//             chapterTitle: "짙은 밤에 드리운 불빛"
//         );
//         
//         var nodes = new[]
//         {
//             new EpisodeNodeModel(
//                 episodeId: "main05.01",
//                 kind: EpisodeNodeKind.Main,
//                 indexText: "01",
//                 title: "첫 만남",
//                 anchoredPos: new Vector2(0f, 0f),
//                 locked: false,
//                 interactable: true,
//                 selected: true,
//                 isCurrent: true,
//                 completed: false,
//                 upperAttachment: null,
//                 lowerAttachment: null
//             ),
//
//             new EpisodeNodeModel(
//                 episodeId: "main05.02",
//                 kind: EpisodeNodeKind.Main,
//                 indexText: "02",
//                 title: "방송 준비",
//                 anchoredPos: new Vector2(400f, 0f),
//                 locked: false,
//                 interactable: true,
//                 selected: false,
//                 isCurrent: false,
//                 completed: false,
//                 upperAttachment: null,
//                 lowerAttachment: new EpisodeAttachmentModel(
//                     hostEpisodeId: "sub05.02A",
//                     displayTitle: "개인 메시지",
//                     isInteractable: true
//                 )
//             ),
//         };
//         
//         EpisodeSelectionPanelModel model = new EpisodeSelectionPanelModel(
//             chapterId: _episodePlayState.CurrentChapterId,
//             chapterMeta: chapterMeta,
//             graph: new EpisodeGraphModel(nodes),
//             selectedEpisodeId: "main05.01"
//         );
//
//         var snapshot = new PlayerStateSnapshot(30, 36, 34);
//         panel.Present(model, snapshot);
//     }
//     
//     private void RefreshEpisodeSelectionPanel()
//     {
//         var panel = UIManager.Instance.GetUI<EpisodeSelectionPanel>();
//         if (panel == null) return;
//         RebuildAndPresentEpisodeSelectionPanel(panel);
//     }
//     
//     private void CloseTopPanel() => UIManager.Instance.PopPanel();
//     
//     public void Dispose()
//     {
//         _ctx.Dispose();
//     }
// }