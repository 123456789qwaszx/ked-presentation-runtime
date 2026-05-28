using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeProgressionRuleData
{
    private readonly Dictionary<string, EpisodeNodeRuleData> _nodeRules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EpisodeEndingRuleData> _endingRules = new(StringComparer.Ordinal);

    public List<EpisodeNodeRuleData> NodeRules = new();
    public List<EpisodeEndingRuleData> EndingRules = new();

    public void AddNodeRule(EpisodeNodeRuleData rule)
    {
        if (rule == null)
            return;

        if (string.IsNullOrEmpty(rule.EpisodeId))
            return;

        _nodeRules[rule.EpisodeId] = rule;
        NodeRules.Add(rule);
    }

    public EpisodeNodeRuleData GetNodeRule(string episodeId)
    {
        return _nodeRules[episodeId];
    }

    public void AddEndingRule(EpisodeEndingRuleData rule)
    {
        if (rule == null)
            return;

        if (string.IsNullOrEmpty(rule.EndingKey))
            return;

        _endingRules[rule.EndingKey] = rule;
        EndingRules.Add(rule);
    }

    public EpisodeEndingRuleData GetEndingRule(string endingKey)
    {
        return _endingRules[endingKey];
    }
}

[Serializable]
public sealed class EpisodeNodeRuleData
{
    public string EpisodeId;
    public EpisodeNodeKind Kind;

    public List<EpisodeCondition> VisibleConditions = new List<EpisodeCondition>();
    public List<EpisodeCondition> UnlockConditions = new List<EpisodeCondition>();

    public List<EpisodeNextOptionData> NextOptions = new List<EpisodeNextOptionData>();
    public List<EpisodeAttachmentRuleData> Attachments = new List<EpisodeAttachmentRuleData>();

    public bool IsChapterEndingCandidate;
    public string EndingKey;
}

[Serializable]
public sealed class EpisodeNextOptionData
{
    public string TargetEpisodeId;
    public string ChoiceLabel;

    public List<EpisodeCondition> Conditions = new List<EpisodeCondition>();

    public bool HideWhenLocked;
    public string LockedReasonText;
}

[Serializable]
public sealed class EpisodeAttachmentRuleData
{
    public string AttachmentId;
    public string ParentEpisodeId;

    public string Title;
    public string IndexText;

    public EpisodeAttachmentKind Kind;
    public string DialogueEntryId;

    public List<EpisodeCondition> VisibleConditions = new List<EpisodeCondition>();
    public List<EpisodeCondition> UnlockConditions = new List<EpisodeCondition>();

    public bool IsRepeatable;
}

[Serializable]
public sealed class EpisodeEndingRuleData
{
    public string EndingKey;
    public string DisplayName;

    public List<EpisodeCondition> Conditions = new List<EpisodeCondition>();

    public bool UnlockNextChapter;
    public string NextChapterId;
}