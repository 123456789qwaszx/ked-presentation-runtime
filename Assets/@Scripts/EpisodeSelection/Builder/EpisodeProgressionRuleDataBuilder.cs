using System.Collections.Generic;

public sealed class EpisodeProgressionRuleDataBuilder
{
    public EpisodeProgressionRuleData Build(ChapterEpisodeProgressionSO progression)
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

            EpisodeNodeRuleData rule = new EpisodeNodeRuleData
            {
                EpisodeId = node.EpisodeId ?? "",
                Kind = node.Kind,
                VisibleConditions = CopyConditions(node.VisibleConditions),
                UnlockConditions = CopyConditions(node.UnlockConditions),
                IsChapterEndingCandidate = node.IsChapterEndingCandidate,
                EndingKey = node.EndingKey ?? ""
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
                TargetEpisodeId = option.TargetEpisodeId ?? "",
                ChoiceLabel = option.ChoiceLabel ?? "",
                Conditions = CopyConditions(option.Conditions),
                HideWhenLocked = option.HideWhenLocked,
                LockedReasonText = option.LockedReasonText ?? ""
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

            rule.Attachments.Add(new EpisodeAttachmentRuleData
            {
                AttachmentId = attachment.AttachmentId ?? "",
                ParentEpisodeId = attachment.ParentEpisodeId ?? "",
                Title = attachment.Title ?? "",
                IndexText = attachment.IndexText ?? "",
                Kind = attachment.Kind,
                DialogueEntryId = attachment.DialogueEntryId ?? "",
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

            data.AddEndingRule(new EpisodeEndingRuleData
            {
                EndingKey = endingRule.EndingKey ?? "",
                DisplayName = endingRule.DisplayName ?? "",
                Conditions = CopyConditions(endingRule.Conditions),
                UnlockNextChapter = endingRule.UnlockNextChapter,
                NextChapterId = endingRule.NextChapterId ?? ""
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
                Key = condition.Key ?? "",
                Op = condition.Op,
                IntValue = condition.IntValue,
                BoolValue = condition.BoolValue,
                StringValue = condition.StringValue ?? ""
            });
        }

        return result;
    }
}