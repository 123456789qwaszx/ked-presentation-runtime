using System.Collections.Generic;

public sealed class EpisodeProgressionRuleDataBuilder
{
    public Dictionary<string, EpisodeProgressionRuleData> Build(
        ChapterEpisodeProgressionCatalogSO catalog)
    {
        Dictionary<string, EpisodeProgressionRuleData> result =
            new Dictionary<string, EpisodeProgressionRuleData>();

        if (catalog == null)
            return result;

        AddChapterRuleData(catalog, result);

        return result;
    }

    private void AddChapterRuleData(
        ChapterEpisodeProgressionCatalogSO catalog,
        Dictionary<string, EpisodeProgressionRuleData> result)
    {
        foreach (KeyValuePair<string, ChapterEpisodeProgressionSO> pair in catalog.EnumerateProgressions())
        {
            string chapterId = pair.Key;
            ChapterEpisodeProgressionSO progression = pair.Value;

            if (progression == null)
                continue;

            result[chapterId] = BuildChapterRuleData(progression);
        }
    }

    private EpisodeProgressionRuleData BuildChapterRuleData(
        ChapterEpisodeProgressionSO progression)
    {
        EpisodeProgressionRuleData data = new EpisodeProgressionRuleData();

        if (progression == null)
            return data;

        AddNodeRules(progression, data);
        AddEndingRules(progression, data);

        return data;
    }

    private void AddNodeRules(
        ChapterEpisodeProgressionSO progression,
        EpisodeProgressionRuleData data)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null)
                continue;

            if (string.IsNullOrEmpty(node.EpisodeId))
                continue;

            EpisodeNodeRuleData rule = new EpisodeNodeRuleData
            {
                EpisodeId = node.EpisodeId,
                Kind = node.Kind,
                VisibleConditions = CopyConditions(node.VisibleConditions),
                UnlockConditions = CopyConditions(node.UnlockConditions),
                IsChapterEndingCandidate = node.IsChapterEndingCandidate,
                EndingKey = node.EndingKey ?? string.Empty
            };

            AddNextOptions(node, rule);
            AddAttachments(node, rule);

            data.AddNodeRule(rule);
        }
    }

    private void AddNextOptions(
        EpisodeNodeDefinition node,
        EpisodeNodeRuleData rule)
    {
        if (node.NextOptions == null)
            return;

        for (int i = 0; i < node.NextOptions.Count; i++)
        {
            EpisodeNextOption option = node.NextOptions[i];

            if (option == null)
                continue;

            rule.NextOptions.Add(new EpisodeNextOptionData
            {
                TargetEpisodeId = option.TargetEpisodeId ?? string.Empty,
                ChoiceLabel = option.ChoiceLabel ?? string.Empty,
                Conditions = CopyConditions(option.Conditions),
                HideWhenLocked = option.HideWhenLocked,
                LockedReasonText = option.LockedReasonText ?? string.Empty
            });
        }
    }

    private void AddAttachments(
        EpisodeNodeDefinition node,
        EpisodeNodeRuleData rule)
    {
        if (node.Attachments == null)
            return;

        for (int i = 0; i < node.Attachments.Count; i++)
        {
            EpisodeAttachmentDefinition attachment = node.Attachments[i];

            if (attachment == null)
                continue;

            if (string.IsNullOrEmpty(attachment.AttachmentId))
                continue;

            rule.Attachments.Add(new EpisodeAttachmentRuleData
            {
                AttachmentId = attachment.AttachmentId,
                ParentEpisodeId = attachment.ParentEpisodeId ?? string.Empty,
                Title = attachment.Title ?? string.Empty,
                IndexText = attachment.IndexText ?? string.Empty,
                Kind = attachment.Kind,
                DialogueEntryId = attachment.DialogueEntryId ?? string.Empty,
                VisibleConditions = CopyConditions(attachment.VisibleConditions),
                UnlockConditions = CopyConditions(attachment.UnlockConditions),
                IsRepeatable = attachment.IsRepeatable
            });
        }
    }

    private void AddEndingRules(
        ChapterEpisodeProgressionSO progression,
        EpisodeProgressionRuleData data)
    {
        if (progression.EndingRules == null)
            return;

        for (int i = 0; i < progression.EndingRules.Count; i++)
        {
            ChapterEndingRule endingRule = progression.EndingRules[i];

            if (endingRule == null)
                continue;

            if (string.IsNullOrEmpty(endingRule.EndingKey))
                continue;

            data.AddEndingRule(new EpisodeEndingRuleData
            {
                EndingKey = endingRule.EndingKey,
                DisplayName = endingRule.DisplayName ?? string.Empty,
                Conditions = CopyConditions(endingRule.Conditions),
                UnlockNextChapter = endingRule.UnlockNextChapter,
                NextChapterId = endingRule.NextChapterId ?? string.Empty
            });
        }
    }

    private static List<EpisodeCondition> CopyConditions(
        List<EpisodeCondition> source)
    {
        List<EpisodeCondition> result = new List<EpisodeCondition>();

        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            EpisodeCondition condition = source[i];

            if (condition == null)
                continue;

            result.Add(new EpisodeCondition
            {
                Kind = condition.Kind,
                Key = condition.Key ?? string.Empty,
                Op = condition.Op,
                IntValue = condition.IntValue,
                BoolValue = condition.BoolValue,
                StringValue = condition.StringValue ?? string.Empty
            });
        }

        return result;
    }
}