using System;
using System.Collections.Generic;

public sealed class EpisodeSelectionRuntimeModel
{
    private readonly ChapterEpisodeProgressionCatalogSO _progressionCatalog;
    private readonly EpisodeProgressionGraphDataBuilder _graphDataBuilder;

    public int CurrentChapterId = -1;

    public EpisodeChapterRuntimeData CurrentChapter { get; private set; }
    public EpisodeGraphData CurrentGraphData { get; private set; }
    public EpisodeProgressionRuleData ProgressionRules { get; private set; }
    public EpisodeSelectionStateData State { get; private set; } = new EpisodeSelectionStateData();

    public EpisodeSelectionRuntimeModel(
        ChapterEpisodeProgressionCatalogSO progressionCatalog,
        EpisodeProgressionGraphDataBuilder graphDataBuilder)
    {
        _progressionCatalog = progressionCatalog;
        _graphDataBuilder = graphDataBuilder;
    }

    public bool OpenChapter(int chapterId)
    {
        if (_progressionCatalog == null)
            return false;

        if (_graphDataBuilder == null)
            return false;

        if (!_progressionCatalog.TryGetProgression(
                chapterId,
                out ChapterEpisodeProgressionSO progression))
        {
            return false;
        }

        CurrentChapterId = chapterId;

        BuildRuntimeData(progression);

        if (CurrentChapter == null)
            return false;

        State.ResetForChapter(CurrentChapter.StartEpisodeId);
        return true;
    }

    public void SelectEpisode(string episodeId)
    {
        State.SelectEpisode(episodeId);
    }

    public void CompleteEpisode(string episodeId)
    {
        State.CompleteEpisode(episodeId);
    }

    public bool IsEpisodeLocked(string episodeId)
    {
        return State.IsEpisodeLocked(episodeId);
    }

    public bool TryGetSelectedEpisodeId(out string episodeId)
    {
        episodeId = State.SelectedEpisodeId;
        return !string.IsNullOrEmpty(episodeId);
    }

    public bool TryFindNode(
        string episodeId,
        out EpisodeGraphNodeData node)
    {
        node = null;

        if (CurrentGraphData == null)
            return false;

        node = CurrentGraphData.FindNode(episodeId);
        return node != null;
    }

    public bool TryGetDialogueEntryId(
        string episodeId,
        out string dialogueEntryId)
    {
        dialogueEntryId = "";

        if (CurrentChapter == null)
            return false;

        return CurrentChapter.TryGetDialogueEntryId(
            episodeId,
            out dialogueEntryId);
    }

    public bool TryGetNodeRule(
        string episodeId,
        out EpisodeNodeRuleData rule)
    {
        rule = null;

        if (ProgressionRules == null)
            return false;

        return ProgressionRules.TryGetNodeRule(episodeId, out rule);
    }

    private void BuildRuntimeData(ChapterEpisodeProgressionSO progression)
    {
        CurrentChapter = BuildChapterRuntimeData(progression);
        CurrentGraphData = _graphDataBuilder.Build(progression);
        ProgressionRules = BuildProgressionRules(progression);
    }

    private EpisodeChapterRuntimeData BuildChapterRuntimeData(
        ChapterEpisodeProgressionSO progression)
    {
        EpisodeChapterRuntimeData data = new EpisodeChapterRuntimeData();

        if (progression == null)
            return data;

        data.ChapterId = progression.ChapterId ?? "";
        data.DisplayName = progression.DisplayName ?? "";
        data.StartEpisodeId = progression.StartEpisodeId ?? "";

        if (progression.Nodes != null)
        {
            for (int i = 0; i < progression.Nodes.Count; i++)
            {
                EpisodeNodeDefinition node = progression.Nodes[i];

                if (node == null)
                    continue;

                data.AddDialogueEntry(new EpisodeDialogueEntryData
                {
                    EpisodeId = node.EpisodeId ?? "",
                    Kind = node.Kind,
                    DialogueEntryId = node.DialogueEntryId ?? ""
                });

                if (node.Attachments == null)
                    continue;

                for (int j = 0; j < node.Attachments.Count; j++)
                {
                    EpisodeAttachmentDefinition attachment = node.Attachments[j];

                    if (attachment == null)
                        continue;

                    data.AddDialogueEntry(new EpisodeDialogueEntryData
                    {
                        EpisodeId = attachment.AttachmentId ?? "",
                        Kind = EpisodeNodeKind.Attachment,
                        DialogueEntryId = attachment.DialogueEntryId ?? ""
                    });
                }
            }
        }

        return data;
    }

    private EpisodeProgressionRuleData BuildProgressionRules(
        ChapterEpisodeProgressionSO progression)
    {
        EpisodeProgressionRuleData data = new EpisodeProgressionRuleData();

        if (progression == null)
            return data;

        BuildNodeRules(progression, data);
        BuildEndingRules(progression, data);

        return data;
    }

    private void BuildNodeRules(
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

            CopyNextOptions(node, rule);
            CopyAttachments(node, rule);

            data.AddNodeRule(rule);
        }
    }

    private void CopyNextOptions(
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

    private void CopyAttachments(
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

    private void BuildEndingRules(
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

    public EpisodeSelectionRuntimeModel CloneRuntimeValuesOnly()
    {
        EpisodeSelectionRuntimeModel clone = new EpisodeSelectionRuntimeModel(
            _progressionCatalog,
            _graphDataBuilder)
        {
            CurrentChapterId = CurrentChapterId,
            CurrentChapter = CurrentChapter,
            CurrentGraphData = CurrentGraphData,
            ProgressionRules = ProgressionRules,
            State = State.Clone()
        };

        return clone;
    }
}