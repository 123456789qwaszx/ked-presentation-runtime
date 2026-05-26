using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings
{
    private int _currentChapterId = -1;

    private Action<string> _onEpisodeRequested;
    private Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkViewData> _onEpisodeLinkRequested;

    public void ConfigureEpisodeSelection(
        Action<string> onEpisodeRequested = null,
        Action<string, EpisodeNodeLinkSlot, EpisodeNodeLinkViewData> onEpisodeLinkRequested = null)
    {
        _onEpisodeRequested = onEpisodeRequested;
        _onEpisodeLinkRequested = onEpisodeLinkRequested;
    }

    public void GoToEpisodeSelection(int chapterId)
    {
        _currentChapterId = chapterId;

        UI.PushPanel<EpisodeSelectionPanel>(panel => { BindRoot(panel, BindEpisodeSelectionPanel); });
    }

    private void BindEpisodeSelectionPanel(EpisodeSelectionPanel panel)
    {
        if (panel == null)
            return;

        _ctx.Bind(panel,
            p => p.OnBackRequested += OnEpisodeSelectionBackRequested,
            p => p.OnBackRequested -= OnEpisodeSelectionBackRequested);

        panel.SetHandlers(
            onMain: OnEpisodeMainRequested,
            onLink: OnEpisodeLinkRequested);

        RefreshEpisodeSelectionPanel(panel);
    }

    private void RefreshEpisodeSelectionPanel(EpisodeSelectionPanel panel)
    {
        if (panel == null)
            return;

        EpisodeSelectionPanelModel model = CreateDebugEpisodeSelectionPanelModel(_currentChapterId);

        PlayerStateSnapshot state = CreateDebugPlayerStateSnapshot();

        panel.Present(model, state);
    }

    private void OnEpisodeMainRequested(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId))
            return;

        Debug.Log($"[VnScreenBindings] Episode main clicked: {episodeId}");

        _onEpisodeRequested?.Invoke(episodeId);
    }

    private void OnEpisodeLinkRequested(
        string ownerEpisodeId,
        EpisodeNodeLinkSlot slot,
        EpisodeNodeLinkViewData link)
    {
        if (string.IsNullOrEmpty(ownerEpisodeId))
            return;

        if (string.IsNullOrEmpty(link.TargetEpisodeId))
            return;

        _onEpisodeLinkRequested?.Invoke(ownerEpisodeId, slot, link);
    }

    private void OnEpisodeSelectionBackRequested()
    {
        GoToChapterSelection();
    }

    private EpisodeSelectionPanelModel CreateDebugEpisodeSelectionPanelModel(int chapterId)
    {
        ChapterMetaModel meta = new ChapterMetaModel(
            chapterIndex: $"CHAPTER {chapterId:00}",
            eraText: "STELLA ERA",
            chapterTitle: "테스트 에피소드 그래프");

        List<EpisodeNodeModel> nodes = new List<EpisodeNodeModel>
        {
            new EpisodeNodeModel(
                episodeId: "main05.01",
                role: EpisodeNodeRole.Main,
                indexText: "01",
                title: "첫 번째 방송",
                anchoredPos: new Vector2(0f, 0f),
                size: new Vector2(320f, 140f),
                completed: true),

            new EpisodeNodeModel(
                episodeId: "main05.02",
                role: EpisodeNodeRole.Main,
                indexText: "02",
                title: "갈라지는 반응",
                anchoredPos: new Vector2(420f, 0f),
                size: new Vector2(320f, 140f),
                isCurrent: true,
                selected: true,
                lowerLink: new EpisodeNodeLinkModel(
                    EpisodeLinkRole.Attachment,
                    "sub05.02A",
                    "비공개 기록",
                    true)),

            new EpisodeNodeModel(
                episodeId: "branch05.02U",
                role: EpisodeNodeRole.Branch,
                indexText: "B",
                title: "상단 루트",
                anchoredPos: new Vector2(840f, 180f),
                size: new Vector2(300f, 120f)),

            new EpisodeNodeModel(
                episodeId: "sub05.02A",
                role: EpisodeNodeRole.Attachment,
                indexText: "S",
                title: "부착 에피소드",
                anchoredPos: new Vector2(840f, -180f),
                size: new Vector2(300f, 120f)),

            new EpisodeNodeModel(
                episodeId: "ending05.good",
                role: EpisodeNodeRole.Ending,
                indexText: "E",
                title: "올바른 미래",
                anchoredPos: new Vector2(1260f, 0f),
                size: new Vector2(340f, 150f),
                locked: true,
                interactable: false)
        };

        EpisodeGraphModel graph = new EpisodeGraphModel(nodes);

        return new EpisodeSelectionPanelModel(
            chapterId,
            meta,
            graph,
            selectedEpisodeId: "main05.02");
    }

    private PlayerStateSnapshot CreateDebugPlayerStateSnapshot()
    {
        return new PlayerStateSnapshot(
            intuition: 30,
            analysis: 35,
            chaos: 30);
    }
}