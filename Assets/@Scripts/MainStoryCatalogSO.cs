using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MainStoryCatalog",
    menuName = "VN/Story/Main Story Catalog")]
public sealed class MainStoryCatalogSO : ScriptableObject
{
    public ChapterSpec[] chapters = Array.Empty<ChapterSpec>();
}

[Serializable]
public sealed class ChapterSpec
{
    public int chapterId;
    public string displayName = "";
    public string eraText = "";
    public EpisodeSpec[] episodes = Array.Empty<EpisodeSpec>();
}

[Serializable]
public sealed class EpisodeSpec
{
    [Header("Identity")]
    public string episodeId = "";
    public int order;
    public string displayName = "";

    [Header("Runtime Entry")]
    public string yarnStartNode = "";
    public string entryKey = "";

    [Header("Main Flow")]
    public string next = "";

    [Header("Branches")]
    public string branchUpperTo = "";
    public string branchMiddleTo = "";
    public string branchLowerTo = "";

    [Header("Attachments")]
    public string attachmentLowerTo = "";

    [Header("Attachment Requirements")]
    public int attachmentIntuitionMin = -1;
    public int attachmentAnalysisMin = -1;
    public int attachmentChaosMin = -1;

    [Header("Ending")]
    public bool isEnding;
    public string endingTitle = "";
}