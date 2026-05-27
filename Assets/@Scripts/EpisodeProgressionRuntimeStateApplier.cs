public sealed class EpisodeProgressionRuntimeStateApplier
{
    public void Apply(
        ChapterEpisodeProgressionSO progression,
        EpisodeSelectionRuntimeState state)
    {
        if (progression == null || state == null)
            return;

        state.VisibleEpisodeIds.Clear();
        state.LockedEpisodeIds.Clear();

        EpisodeConditionEvaluator evaluator = new EpisodeConditionEvaluator(state);

        ApplyNodes(progression, state, evaluator);
        ApplyAttachments(progression, state, evaluator);
    }

    private void ApplyNodes(
        ChapterEpisodeProgressionSO progression,
        EpisodeSelectionRuntimeState state,
        EpisodeConditionEvaluator evaluator)
    {
        if (progression.Nodes == null)
            return;

        for (int i = 0; i < progression.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = progression.Nodes[i];

            if (node == null || string.IsNullOrEmpty(node.EpisodeId))
                continue;

            bool visible = evaluator.AreMet(node.VisibleConditions);

            if (!visible)
                continue;

            state.VisibleEpisodeIds.Add(node.EpisodeId);

            bool unlocked = evaluator.AreMet(node.UnlockConditions);

            if (!unlocked)
                state.LockedEpisodeIds.Add(node.EpisodeId);
        }
    }

    private void ApplyAttachments(
        ChapterEpisodeProgressionSO progression,
        EpisodeSelectionRuntimeState state,
        EpisodeConditionEvaluator evaluator)
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

                bool visible = evaluator.AreMet(attachment.VisibleConditions);

                if (!visible)
                    continue;

                state.VisibleEpisodeIds.Add(attachment.AttachmentId);

                bool unlocked = evaluator.AreMet(attachment.UnlockConditions);

                if (!unlocked)
                    state.LockedEpisodeIds.Add(attachment.AttachmentId);
            }
        }
    }
}