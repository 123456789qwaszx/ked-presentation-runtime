namespace Ked.Progression
{
    public static class ScenarioTransition
    {
        public static ScenarioAdvance Resolve(ChapterProgression chapter, ProgressionState state)
        {
            chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode node);

            return new ScenarioAdvance(node.EventKey);
        }
    }
}