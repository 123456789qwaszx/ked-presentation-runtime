using System.Collections.Generic;

public sealed class EpisodeConditionEvaluator
{
    private readonly EpisodeSelectionRuntimeModel _runtimeModel;

    public EpisodeConditionEvaluator(EpisodeSelectionRuntimeModel runtimeModel)
    {
        _runtimeModel = runtimeModel;
    }

    public void RebuildAvailabilityState()
    {
        if (_runtimeModel == null)
            return;

        ChapterEpisodeProgressionSO progression = _runtimeModel.CurrentProgression;

        if (progression == null)
            return;

        _runtimeModel.VisibleEpisodeIds.Clear();
        _runtimeModel.LockedEpisodeIds.Clear();

        ApplyNodeAvailability(progression);
        ApplyAttachmentAvailability(progression);
    }

    public bool AreMet(List<EpisodeCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            if (!IsMet(conditions[i]))
                return false;
        }

        return true;
    }

    private void ApplyNodeAvailability(ChapterEpisodeProgressionSO progression)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null || string.IsNullOrEmpty(node.EpisodeId))
                continue;

            bool visible = AreMet(node.VisibleConditions);

            if (!visible)
                continue;

            _runtimeModel.VisibleEpisodeIds.Add(node.EpisodeId);

            bool unlocked = AreMet(node.UnlockConditions);

            if (!unlocked)
                _runtimeModel.LockedEpisodeIds.Add(node.EpisodeId);
        }
    }

    private void ApplyAttachmentAvailability(ChapterEpisodeProgressionSO progression)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null || node.Attachments == null)
                continue;

            for (int j = 0; j < node.Attachments.Count; j++)
            {
                EpisodeAttachmentDefinition attachment = node.Attachments[j];

                if (attachment == null || string.IsNullOrEmpty(attachment.AttachmentId))
                    continue;

                bool visible = AreMet(attachment.VisibleConditions);

                if (!visible)
                    continue;

                _runtimeModel.VisibleEpisodeIds.Add(attachment.AttachmentId);

                bool unlocked = AreMet(attachment.UnlockConditions);

                if (!unlocked)
                    _runtimeModel.LockedEpisodeIds.Add(attachment.AttachmentId);
            }
        }
    }

    private bool IsMet(EpisodeCondition condition)
    {
        if (condition == null)
            return true;

        if (_runtimeModel == null)
            return false;

        switch (condition.Kind)
        {
            case EpisodeConditionKind.Flag:
                return EvaluateFlag(condition);

            case EpisodeConditionKind.Stat:
                return EvaluateStat(condition);

            case EpisodeConditionKind.EpisodeCleared:
                return EvaluateExists(
                    _runtimeModel.ClearedEpisodeIds.Contains(condition.Key),
                    condition.Op);

            case EpisodeConditionKind.ChapterCleared:
                return EvaluateExists(
                    _runtimeModel.ClearedChapterIds.Contains(condition.Key),
                    condition.Op);

            case EpisodeConditionKind.Token:
                return EvaluateExists(
                    _runtimeModel.Tokens.Contains(condition.Key),
                    condition.Op);

            default:
                return false;
        }
    }

    private bool EvaluateFlag(EpisodeCondition condition)
    {
        bool exists = _runtimeModel.Flags.TryGetValue(condition.Key, out bool actualValue);

        switch (condition.Op)
        {
            case EpisodeCompareOp.Exists:
                return exists;

            case EpisodeCompareOp.NotExists:
                return !exists;

            case EpisodeCompareOp.Equal:
                return exists && actualValue == condition.BoolValue;

            case EpisodeCompareOp.NotEqual:
                return !exists || actualValue != condition.BoolValue;

            default:
                return false;
        }
    }

    private bool EvaluateStat(EpisodeCondition condition)
    {
        bool exists = _runtimeModel.Stats.TryGetValue(condition.Key, out int actualValue);

        switch (condition.Op)
        {
            case EpisodeCompareOp.Exists:
                return exists;

            case EpisodeCompareOp.NotExists:
                return !exists;

            case EpisodeCompareOp.Equal:
                return exists && actualValue == condition.IntValue;

            case EpisodeCompareOp.NotEqual:
                return !exists || actualValue != condition.IntValue;

            case EpisodeCompareOp.GreaterOrEqual:
                return exists && actualValue >= condition.IntValue;

            case EpisodeCompareOp.LessOrEqual:
                return exists && actualValue <= condition.IntValue;

            default:
                return false;
        }
    }

    private bool EvaluateExists(bool exists, EpisodeCompareOp op)
    {
        switch (op)
        {
            case EpisodeCompareOp.Exists:
            case EpisodeCompareOp.Equal:
                return exists;

            case EpisodeCompareOp.NotExists:
            case EpisodeCompareOp.NotEqual:
                return !exists;

            default:
                return false;
        }
    }
}