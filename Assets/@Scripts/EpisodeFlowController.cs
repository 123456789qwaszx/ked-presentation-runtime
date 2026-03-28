using System;
using UnityEngine;

public class EpisodeFlowController : IDisposable
{
    private int CHAPTERCOUNT = 6;
    
    private readonly UIBindingContext _ctx = new();
    
    private readonly DialogueUIBindings _dialogueInput;
    private readonly EpisodePlayer _episodePlayer;
    
    private EpisodePlayState _vnRuntimeBridge;
    
    public EpisodeFlowController(
        DialogueUIBindings dialogueInput,
        EpisodePlayer episodePlayer,
        EpisodePlayState vnRuntimeBridge)
    {
        _dialogueInput = dialogueInput;
        _episodePlayer = episodePlayer;
        _vnRuntimeBridge = vnRuntimeBridge;
    }
    
    public void OpenSelectChapterPanel()
    {
        UIManager.Instance.PushPanel<ChapterSelectionPanel>(panel =>
        {
            _ctx.BindScreen(() => panel, BindChapterSelectPanelEvents);
            RebuildAndPresentChapterPanel(panel);
        });
    }
    
    private void BindChapterSelectPanelEvents(ChapterSelectionPanel panel)
    {
        _ctx.Bind(panel,
            p => p.OnChapterRequested += OnChapterRequested,
            p => p.OnChapterRequested -= OnChapterRequested);

        _ctx.Bind(panel,
            p => p.OnBackRequested += CloseTopPanel,
            p => p.OnBackRequested -= CloseTopPanel);
    }
    
    private void RebuildAndPresentChapterPanel(ChapterSelectionPanel panel)
    {
        var models = new ChapterButtonCardModel[CHAPTERCOUNT];

        for (int i = 0; i < CHAPTERCOUNT; i++)
        {
            int chapterId = i + 1;

            models[i] = new ChapterButtonCardModel(
                chapterId,
                indexText: chapterId.ToString(),
                chapterIndexLabel: $"챕터 {chapterId}",
                chapterTitle: $"Chapter {chapterId}",
                episodeHeading: "",
                locked: true
            );
        }

        panel.PresentChapters(models, selectedChapterId: _vnRuntimeBridge.CurrentChapterId);
    }
    
    private void OnChapterRequested(int chapterId) => OpenEpisodeSelectPanel(chapterId);
    
    private void CloseTopPanel() => UIManager.Instance.PopPanel();
    
    private void OpenEpisodeSelectPanel(int chapterId)
    {
        _vnRuntimeBridge.SetCurrentChapter(chapterId);
        string _selectedEpisodeId = "main05.02";
        _vnRuntimeBridge.SetSelectedEpisode(_selectedEpisodeId);

        UIManager.Instance.PushPanel<EpisodeSelectionPanel>(panel =>
        {
            _ctx.BindScreen(() => panel, BindEpisodeSelectPanelEvents);
            RebuildAndPresentEpisodeSelectionPanel(panel);
        });
    }
    
    private void BindEpisodeSelectPanelEvents(EpisodeSelectionPanel panel)
    {
        _ctx.Bind(panel,
            p => p.OnCloseRequested += CloseTopPanel,
            p => p.OnCloseRequested -= CloseTopPanel);

        _ctx.Assign(panel,
            p => p.SetHandlers(onMain: StartEpisodeImmediately, onBranch: HandleAttachmentRequested),
            p => p.SetHandlers(onMain: null, onBranch: null)
        );
    }
    
    private void StartEpisodeImmediately(string ownerEpisodeId)
    {
        if (string.IsNullOrEmpty(ownerEpisodeId))
            return;

        _vnRuntimeBridge.BeginMainEpisode(ownerEpisodeId);

        RefreshEpisodeSelectionPanel();

        UIManager.Instance.PopAllPanels();

        UIManager.Instance.SwitchRoot<DialogueUIRoot>(root =>
        {
            _dialogueInput.Bind(root);
            _episodePlayer.StartYarnNode(ownerEpisodeId);
        });
    }

    private void HandleAttachmentRequested(string ownerEpisodeId, LinkKind kind, string targetEpisodeId)
    {
        if (string.IsNullOrEmpty(ownerEpisodeId) || string.IsNullOrEmpty(targetEpisodeId))
            return;

        _vnRuntimeBridge.BeginAttachmentEpisode(ownerEpisodeId, targetEpisodeId);

        UIManager.Instance.PopAllPanels();

        UIManager.Instance.SwitchRoot<DialogueUIRoot>(root =>
        {
            _dialogueInput.Bind(root);
            _episodePlayer.StartYarnNode(targetEpisodeId);
        });
    }
    
    private void RebuildAndPresentEpisodeSelectionPanel(EpisodeSelectionPanel panel)
    {
        var chapterMeta = new ChapterMetaModel(
            chapterIndex: $"챕터 {_vnRuntimeBridge.CurrentChapterId}",
            eraText: "성력 996년",
            chapterTitle: "짙은 밤에 드리운 불빛"
        );
        
        var nodes = new[]
        {
            new EpisodeNodeModel(
                episodeId: "main05.01",
                indexText: "01",
                title: "첫 만남",
                anchoredPos: new Vector2(0f, 0f),
                locked: false,
                interactable: true,
                selected: true,
                isCurrent: true,
                completed: false,
                upperAttachment: null,
                lowerAttachment: null
            ),
            new EpisodeNodeModel(
                episodeId: "main05.02",
                indexText: "02",
                title: "방송 준비",
                anchoredPos: new Vector2(400f, 0f),
                locked: false,
                interactable: true,
                selected: false,
                isCurrent: false,
                completed: false,
                upperAttachment: null,
                lowerAttachment: new EpisodeAttachmentModel(
                    targetEpisodeId: "sub05.02A",
                    title: "개인 메시지",
                    interactable: true
                )
            ),
        };
        
        EpisodeSelectionPanelModel model = new EpisodeSelectionPanelModel(
            chapterId: _vnRuntimeBridge.CurrentChapterId,
            chapterMeta: chapterMeta,
            graph: new EpisodeGraphModel(nodes),
            selectedEpisodeId: "main05.01"
        );

        var snapshot = new PlayerStateSnapshot(30, 36, 34);
        panel.Present(model, snapshot);
    }
    
    private void RefreshEpisodeSelectionPanel()
    {
        var panel = UIManager.Instance.GetUI<EpisodeSelectionPanel>();
        if (panel == null) return;
        RebuildAndPresentEpisodeSelectionPanel(panel);
    }
    
    
    public void Dispose()
    {
        _ctx.Dispose();
    }
}