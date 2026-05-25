using System.Collections.Generic;
using UnityEngine;

public enum EpisodeNodeRole
{
    Main,
    Branch,
    Attachment,
    Ending
}

public enum EpisodeNodeLinkSlot
{
    Main,
    Upper,
    Middle,
    Lower
}

public enum EpisodeLinkRole
{
    Branch,
    Attachment,
    Ending
}

public readonly struct EpisodeGraphModel
{
    public readonly IReadOnlyList<EpisodeNodeModel> Nodes;

    public EpisodeGraphModel(IReadOnlyList<EpisodeNodeModel> nodes)
    {
        Nodes = nodes;
    }

    public static EpisodeGraphModel Empty()
    {
        return new EpisodeGraphModel(System.Array.Empty<EpisodeNodeModel>());
    }
}

public readonly struct EpisodeNodeModel
{
    public readonly string EpisodeId;
    public readonly EpisodeNodeRole Role;

    public readonly string IndexText;
    public readonly string Title;

    public readonly Vector2 AnchoredPos;
    public readonly Vector2 Size;

    public readonly Sprite MainBg;
    public readonly Sprite MainIcon;
    public readonly Sprite UpperLinkBg;
    public readonly Sprite LowerLinkBg;

    public readonly bool Locked;
    public readonly bool Interactable;
    public readonly bool Selected;
    public readonly bool IsCurrent;
    public readonly bool Completed;

    public readonly EpisodeNodeLinkModel? UpperLink;
    public readonly EpisodeNodeLinkModel? LowerLink;

    public EpisodeNodeModel(
        string episodeId,
        EpisodeNodeRole role,
        string indexText,
        string title,
        Vector2 anchoredPos,
        Vector2 size,
        Sprite mainBg = null,
        Sprite mainIcon = null,
        Sprite upperLinkBg = null,
        Sprite lowerLinkBg = null,
        bool locked = false,
        bool interactable = true,
        bool selected = false,
        bool isCurrent = false,
        bool completed = false,
        EpisodeNodeLinkModel? upperLink = null,
        EpisodeNodeLinkModel? lowerLink = null)
    {
        EpisodeId = episodeId ?? "";
        Role = role;

        IndexText = indexText ?? "";
        Title = title ?? "";

        AnchoredPos = anchoredPos;
        Size = size;

        MainBg = mainBg;
        MainIcon = mainIcon;
        UpperLinkBg = upperLinkBg;
        LowerLinkBg = lowerLinkBg;

        Locked = locked;
        Interactable = interactable;
        Selected = selected;
        IsCurrent = isCurrent;
        Completed = completed;

        UpperLink = upperLink;
        LowerLink = lowerLink;
    }
}

public readonly struct EpisodeNodeLinkModel
{
    public readonly EpisodeLinkRole Role;
    public readonly string TargetEpisodeId;
    public readonly string DisplayTitle;
    public readonly bool Interactable;

    public EpisodeNodeLinkModel(
        EpisodeLinkRole role,
        string targetEpisodeId,
        string displayTitle,
        bool interactable = true)
    {
        Role = role;
        TargetEpisodeId = targetEpisodeId ?? "";
        DisplayTitle = displayTitle ?? "";
        Interactable = interactable;
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
        SelectedEpisodeId = selectedEpisodeId ?? "";
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
        ChapterIndex = chapterIndex ?? "";
        EraText = eraText ?? "";
        ChapterTitle = chapterTitle ?? "";
    }
}