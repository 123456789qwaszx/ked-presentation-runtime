using System.Collections.Generic;
using UnityEngine;

public enum LinkKind
{
    BranchUpper,
    BranchMiddle,
    BranchLower,
    AttachmentLower
}

public enum EpisodeNodeKind
{
    Main,
    Branch,
    BranchChain,
    Attachment,
    Ending
}

public readonly struct ChapterButtonCardModel
{
    public readonly int ChapterId;

    public readonly string IndexText;
    public readonly string ChapterIndexLabel;
    public readonly string ChapterTitle;
    public readonly string EpisodeHeading;

    public readonly Sprite Bg;
    public readonly Sprite BgOverlay;
    public readonly Sprite ChapterIndexLabelSprite;
    public readonly Sprite EpisodeHeadingLabelSprite;
    public readonly Sprite TitleIcon;

    public readonly bool Interactable;
    public readonly bool Locked;

    public ChapterButtonCardModel(
        int chapterId,
        string indexText,
        string chapterIndexLabel,
        string chapterTitle,
        string episodeHeading,
        Sprite bg = null,
        Sprite bgOverlay = null,
        Sprite chapterIndexLabelSprite = null,
        Sprite episodeHeadingLabelSprite = null,
        Sprite titleIcon = null,
        bool interactable = true,
        bool locked = false)
    {
        ChapterId = chapterId;

        IndexText = indexText;
        ChapterIndexLabel = chapterIndexLabel;
        ChapterTitle = chapterTitle;
        EpisodeHeading = episodeHeading;

        Bg = bg;
        BgOverlay = bgOverlay;
        ChapterIndexLabelSprite = chapterIndexLabelSprite;
        EpisodeHeadingLabelSprite = episodeHeadingLabelSprite;
        TitleIcon = titleIcon;

        Interactable = interactable;
        Locked = locked;
    }

    public static ChapterButtonCardModel Empty()
    {
        return new ChapterButtonCardModel(
            chapterId: -1,
            indexText: "",
            chapterIndexLabel: "",
            chapterTitle: "",
            episodeHeading: "",
            interactable: false,
            locked: true
        );
    }
}

public readonly struct EpisodeGraphModel
{
    public readonly IReadOnlyList<EpisodeNodeModel> Nodes;

    public EpisodeGraphModel(IReadOnlyList<EpisodeNodeModel> nodes)
    {
        Nodes = nodes;
    }
}

public readonly struct EpisodeNodeModel
{
    public readonly string EpisodeId;
    public readonly EpisodeNodeKind Kind;

    public readonly string IndexText;
    public readonly string Title;
    public readonly Vector2 AnchoredPos;

    public readonly bool Locked;
    public readonly bool Interactable;
    public readonly bool Selected;
    public readonly bool IsCurrent;
    public readonly bool Completed;

    public readonly EpisodeAttachmentModel? UpperAttachment;
    public readonly EpisodeAttachmentModel? LowerAttachment;

    public EpisodeNodeModel(
        string episodeId,
        EpisodeNodeKind kind,
        string indexText,
        string title,
        Vector2 anchoredPos,
        bool locked = false,
        bool interactable = true,
        bool selected = false,
        bool isCurrent = false,
        bool completed = false,
        EpisodeAttachmentModel? upperAttachment = null,
        EpisodeAttachmentModel? lowerAttachment = null)
    {
        EpisodeId = episodeId;
        Kind = kind;

        IndexText = indexText;
        Title = title;
        AnchoredPos = anchoredPos;

        Locked = locked;
        Interactable = interactable;
        Selected = selected;
        IsCurrent = isCurrent;
        Completed = completed;

        UpperAttachment = upperAttachment;
        LowerAttachment = lowerAttachment;
    }
}

public readonly struct EpisodeAttachmentModel
{
    public readonly string HostEpisodeId;
    public readonly string DisplayTitle;
    public readonly bool IsInteractable;

    public EpisodeAttachmentModel(
        string hostEpisodeId,
        string displayTitle,
        bool isInteractable = true)
    {
        HostEpisodeId = hostEpisodeId;
        DisplayTitle = displayTitle;
        IsInteractable = isInteractable;
    }
}

public readonly struct EpisodeSelectionPanelModel
{
    public readonly int ChapterId;
    public readonly ChapterMetaModel ChapterMeta;
    public readonly EpisodeGraphModel Graph;
    public readonly string SelectedEpisodeId;

    public EpisodeSelectionPanelModel(
        int chapterId,
        ChapterMetaModel chapterMeta,
        EpisodeGraphModel graph,
        string selectedEpisodeId = "")
    {
        ChapterId = chapterId;
        ChapterMeta = chapterMeta;
        Graph = graph;
        SelectedEpisodeId = selectedEpisodeId;
    }
}

public readonly struct ChapterMetaModel
{
    public readonly string ChapterIndex;
    public readonly string EraText;
    public readonly string ChapterTitle;

    public ChapterMetaModel(
        string chapterIndex,
        string eraText,
        string chapterTitle)
    {
        ChapterIndex = chapterIndex;
        EraText = eraText;
        ChapterTitle = chapterTitle;
    }
}