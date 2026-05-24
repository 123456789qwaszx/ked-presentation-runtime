using UnityEngine;

public sealed class StorySelectionBootstrap : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MainStoryCatalogSO mainStoryCatalog;

    [Header("Progress Source")]
    [Tooltip("MonoBehaviour that implements IEpisodeProgress.")]
    [SerializeField] private MonoBehaviour episodeProgressSource;

    [Header("Debug / Initial State")]
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private int initialChapterId = 5;
    [SerializeField] private string initialSelectedEpisodeId = "";

    private IEpisodePlayLookup _lookup;
    private IEpisodeProgress _progress;

    private ChapterButtonCardModelBuilder _chapterCardBuilder;
    private string _selectedEpisodeId = "";

    private void Awake()
    {
        Bootstrap();
    }

    private void Start()
    {
        if (!openOnStart)
            return;

        OpenEpisodeSelection(initialChapterId, initialSelectedEpisodeId);
    }

    private void Bootstrap()
    {
        if (mainStoryCatalog == null)
        {
            Debug.LogWarning("[StorySelectionBootstrap] MainStoryCatalogSO is null.", this);
            return;
        }

        MainStoryCatalogLookup lookup = new MainStoryCatalogLookup(mainStoryCatalog);
        lookup.BuildIfNeeded();

        _lookup = lookup;
        _chapterCardBuilder = new ChapterButtonCardModelBuilder();

        _progress = episodeProgressSource as IEpisodeProgress;

        if (_progress == null)
        {
            Debug.LogWarning(
                "[StorySelectionBootstrap] episodeProgressSource does not implement IEpisodeProgress.",
                this);
            return;
        }

        if (episodeProgressSource is EpisodeProgressRuntime runtime)
            runtime.Initialize(_lookup);

        _selectedEpisodeId = initialSelectedEpisodeId ?? "";
    }

    public void OpenChapterSelection(int selectedChapterId = -1)
    {
        if (!IsReady())
            return;

        UIManager.Instance.PushPanel<ChapterSelectionPanel>();

        ChapterSelectionPanel panelRoot = UIManager.Instance.GetUI<ChapterSelectionPanel>();
        if (panelRoot == null)
        {
            Debug.LogWarning("[StorySelectionBootstrap] ChapterSelectionPanel root not found.", this);
            return;
        }

        ChapterSelectionPanelView view =
            panelRoot.GetComponentInChildren<ChapterSelectionPanelView>(true);

        if (view == null)
        {
            Debug.LogWarning(
                "[StorySelectionBootstrap] ChapterSelectionPanelView not found under ChapterSelectionPanel.",
                panelRoot);
            return;
        }

        ChapterButtonCardModel[] models = _chapterCardBuilder.Build(_lookup, _progress);

        view.ChapterRequested -= HandleChapterRequested;
        view.BackRequested -= HandleChapterBackRequested;

        view.ChapterRequested += HandleChapterRequested;
        view.BackRequested += HandleChapterBackRequested;

        view.PresentChapters(models, selectedChapterId);
    }

    public void OpenEpisodeSelection(int chapterId, string selectedEpisodeId = "")
    {
        if (!IsReady())
            return;

        _selectedEpisodeId = selectedEpisodeId ?? "";

        UIManager.Instance.SwitchRoot<EpisodeSelectionPanel>();

        EpisodeSelectionPanel panelRoot = UIManager.Instance.GetUI<EpisodeSelectionPanel>();
        if (panelRoot == null)
        {
            Debug.LogWarning("[StorySelectionBootstrap] EpisodeSelectionPanel root not found.", this);
            return;
        }

        EpisodeSelectionPanelView view =
            panelRoot.GetComponentInChildren<EpisodeSelectionPanelView>(true);

        if (view == null)
        {
            Debug.LogWarning(
                "[StorySelectionBootstrap] EpisodeSelectionPanelView not found under EpisodeSelectionPanel. " +
                "Falling back to EpisodeGraphView-only render.",
                panelRoot);

            RenderGraphOnly(panelRoot, chapterId, _selectedEpisodeId);
            return;
        }

        view.CloseRequested -= HandleEpisodePanelCloseRequested;
        view.CloseRequested += HandleEpisodePanelCloseRequested;

        view.SetHandlers(
            onMain: HandleEpisodeMainClicked,
            onBranch: HandleEpisodeBranchClicked
        );

        EpisodeSelectionPanelModel panelModel = EpisodeSelectionModelBuilder.Build(
            chapterId,
            _selectedEpisodeId,
            _progress,
            _lookup
        );

        _selectedEpisodeId = panelModel.SelectedEpisodeId;

        PlayerStateSnapshot snapshot = _progress.GetPlayerStateSnapshot();

        view.Present(panelModel, snapshot);
    }

    private void RenderGraphOnly(
        EpisodeSelectionPanel panelRoot,
        int chapterId,
        string selectedEpisodeId)
    {
        EpisodeGraphView graphView =
            panelRoot.GetComponentInChildren<EpisodeGraphView>(true);

        if (graphView == null)
        {
            Debug.LogWarning(
                "[StorySelectionBootstrap] EpisodeGraphView not found under EpisodeSelectionPanel.",
                panelRoot);
            return;
        }

        EpisodeSelectionPanelModel panelModel = EpisodeSelectionModelBuilder.Build(
            chapterId,
            selectedEpisodeId,
            _progress,
            _lookup
        );

        _selectedEpisodeId = panelModel.SelectedEpisodeId;

        graphView.SetHandlers(
            onMainClicked: HandleEpisodeMainClicked,
            onBranchClicked: HandleEpisodeBranchClicked
        );

        graphView.Render(panelModel.Graph);
    }

    private void HandleChapterRequested(int chapterId)
    {
        OpenEpisodeSelection(chapterId, "");
    }

    private void HandleChapterBackRequested()
    {
        Debug.Log("[StorySelectionBootstrap] Chapter selection back requested.", this);
    }

    private void HandleEpisodePanelCloseRequested()
    {
        OpenChapterSelection(initialChapterId);
    }

    private void HandleEpisodeMainClicked(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        _selectedEpisodeId = episodeId;

        if (!_lookup.TryGetEpisode(episodeId, out EpisodeSpec episode) || episode == null)
        {
            Debug.LogWarning(
                $"[StorySelectionBootstrap] Clicked episode not found. episodeId='{episodeId}'",
                this);
            return;
        }

        Debug.Log(
            $"[StorySelectionBootstrap] Main episode clicked. " +
            $"episodeId='{episode.episodeId}', yarnStartNode='{episode.yarnStartNode}', entryKey='{episode.entryKey}'",
            this);

        // 실제 플레이 연결 지점.
        // episodePlayer.StartEpisode(episode.yarnStartNode, episode.entryKey);
    }

    private void HandleEpisodeBranchClicked(
        string ownerEpisodeId,
        LinkKind kind,
        string targetEpisodeId)
    {
        if (string.IsNullOrEmpty(targetEpisodeId))
            return;

        _selectedEpisodeId = targetEpisodeId;

        if (!_lookup.TryGetEpisode(targetEpisodeId, out EpisodeSpec target) || target == null)
        {
            Debug.LogWarning(
                $"[StorySelectionBootstrap] Branch target not found. " +
                $"owner='{ownerEpisodeId}', kind='{kind}', target='{targetEpisodeId}'",
                this);
            return;
        }

        Debug.Log(
            $"[StorySelectionBootstrap] Branch/Attachment clicked. " +
            $"owner='{ownerEpisodeId}', kind='{kind}', target='{target.episodeId}', " +
            $"yarnStartNode='{target.yarnStartNode}', entryKey='{target.entryKey}'",
            this);

        // 실제 플레이 연결 지점.
        // episodePlayer.StartEpisode(target.yarnStartNode, target.entryKey);
    }

    private bool IsReady()
    {
        if (_lookup == null)
        {
            Debug.LogWarning("[StorySelectionBootstrap] Lookup is not initialized.", this);
            return false;
        }

        if (_progress == null)
        {
            Debug.LogWarning("[StorySelectionBootstrap] Progress is not initialized.", this);
            return false;
        }

        return true;
    }
}