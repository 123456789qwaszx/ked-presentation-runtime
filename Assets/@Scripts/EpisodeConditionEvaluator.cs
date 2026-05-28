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
        _runtimeModel.State.VisibleEpisodeIds.Clear();
        _runtimeModel.State.LockedEpisodeIds.Clear();

        ApplyNodeAvailability();
        ApplyAttachmentAvailability();
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

    private void ApplyNodeAvailability()
    {
        List<EpisodeNodeRuleData> nodeRules = _runtimeModel.ProgressionRules.NodeRules;

        for (int i = 0; i < nodeRules.Count; i++)
        {
            EpisodeNodeRuleData rule = nodeRules[i];

            if (rule?.Kind != EpisodeNodeKind.Main)
                continue;

            ApplyAvailability(rule.EpisodeId, rule.VisibleConditions, rule.UnlockConditions);
        }
    }

    private void ApplyAttachmentAvailability()
    {
        List<EpisodeNodeRuleData> nodeRules = _runtimeModel.ProgressionRules.NodeRules;

        for (int i = 0; i < nodeRules.Count; i++)
        {
            EpisodeNodeRuleData rule = nodeRules[i];

            if (rule?.Attachments == null)
                continue;

            for (int j = 0; j < rule.Attachments.Count; j++)
            {
                EpisodeAttachmentRuleData attachment = rule.Attachments[j];

                if (attachment == null)
                    continue;

                ApplyAvailability(
                    attachment.AttachmentId,
                    attachment.VisibleConditions,
                    attachment.UnlockConditions);
            }
        }
    }

    private void ApplyAvailability(
        string episodeId,
        List<EpisodeCondition> visibleConditions,
        List<EpisodeCondition> unlockConditions)
    {

        bool visible = AreMet(visibleConditions);

        if (!visible)
            return;

        _runtimeModel.State.VisibleEpisodeIds.Add(episodeId);

        bool reachable = _runtimeModel.State.ReachableEpisodeIds.Count == 0 ||
                         _runtimeModel.State.ReachableEpisodeIds.Contains(episodeId);

        bool unlocked = AreMet(unlockConditions);

        if (!reachable || !unlocked)
            _runtimeModel.State.LockedEpisodeIds.Add(episodeId);
    }

    private bool IsMet(EpisodeCondition condition)
    {
        if (condition == null)
            return true;

        switch (condition.Kind)
        {
            case EpisodeConditionKind.Flag:
                return EvaluateFlag(condition);

            case EpisodeConditionKind.Stat:
                return EvaluateStat(condition);

            case EpisodeConditionKind.EpisodeCleared:
                return EvaluateExists(
                    _runtimeModel.State.ClearedEpisodeIds.Contains(condition.Key),
                    condition.Op);

            case EpisodeConditionKind.ChapterCleared:
                return EvaluateExists(
                    _runtimeModel.State.ClearedChapterIds.Contains(condition.Key),
                    condition.Op);

            case EpisodeConditionKind.Token:
                return EvaluateExists(
                    _runtimeModel.State.Tokens.Contains(condition.Key),
                    condition.Op);

            default:
                return false;
        }
    }

    private bool EvaluateFlag(EpisodeCondition condition)
    {
        bool exists = _runtimeModel.State.Flags.TryGetValue(condition.Key, out bool value);

        switch (condition.Op)
        {
            case EpisodeCompareOp.Exists:
                return exists;

            case EpisodeCompareOp.NotExists:
                return !exists;

            case EpisodeCompareOp.Equal:
                return exists && value == condition.BoolValue;

            case EpisodeCompareOp.NotEqual:
                return !exists || value != condition.BoolValue;

            default:
                return false;
        }
    }

    private bool EvaluateStat(EpisodeCondition condition)
    {
        bool exists = _runtimeModel.State.Stats.TryGetValue(condition.Key, out int value);

        switch (condition.Op)
        {
            case EpisodeCompareOp.Exists:
                return exists;

            case EpisodeCompareOp.NotExists:
                return !exists;

            case EpisodeCompareOp.Equal:
                return exists && value == condition.IntValue;

            case EpisodeCompareOp.NotEqual:
                return !exists || value != condition.IntValue;

            case EpisodeCompareOp.GreaterOrEqual:
                return exists && value >= condition.IntValue;

            case EpisodeCompareOp.LessOrEqual:
                return exists && value <= condition.IntValue;

            default:
                return false;
        }
    }

    private bool EvaluateExists(bool exists, EpisodeCompareOp op)
    {
        switch (op)
        {
            case EpisodeCompareOp.Exists:
                return exists;

            case EpisodeCompareOp.NotExists:
                return !exists;

            case EpisodeCompareOp.Equal:
                return exists;

            case EpisodeCompareOp.NotEqual:
                return !exists;

            default:
                return false;
        }
    }
}