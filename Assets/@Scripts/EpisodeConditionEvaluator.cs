using System.Collections.Generic;

public sealed class EpisodeConditionEvaluator
{
    private readonly EpisodeSelectionRuntimeState _state;

    public EpisodeConditionEvaluator(EpisodeSelectionRuntimeState state)
    {
        _state = state;
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

    private bool IsMet(EpisodeCondition condition)
    {
        if (condition == null)
            return true;

        switch (condition.Kind)
        {
            case EpisodeConditionKind.EpisodeCleared:
                return EvaluateExists(
                    _state.ClearedEpisodeIds.Contains(condition.Key),
                    condition.Op);

            default:
                // 지금 기존 RuntimeState에는 Flag / Stat / Token / ChapterCleared가 아직 없다.
                // 우선은 미지원 조건은 false로 두는 게 안전하다.
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