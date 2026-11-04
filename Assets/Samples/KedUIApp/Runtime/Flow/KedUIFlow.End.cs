using System.Collections.Generic;
using Ked.Progression;

namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private void OpenEnd(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
        {
            StopFastForward();

            if (_ui.HasPanel)
                CloseAllPanels();

            if (_playScreen.IsHudHidden)
                RestoreHud();

            _ui.PushPanel<EpisodeEndPanel>(Presentation<EpisodeEndPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                RefreshEnd(panel, chapter, lastEpisode, state);
            });
        }

        private void ApplyBindings(EpisodeEndPanel panel)
        {
            AddBinding(panel,
                p => p.TitleClicked += ReturnToTitle,
                p => p.TitleClicked -= ReturnToTitle);
        }

        #region Refresh

        private static void RefreshEnd(
            EpisodeEndPanel panel, 
            ChapterDefinition chapter, 
            EpisodeNode lastEpisode, 
            ChapterState state)
        {
            panel.Present(new EpisodeEndPanel.Model
            {
                Title = string.IsNullOrWhiteSpace(chapter.DisplayName) 
                    ? chapter.ChapterId 
                    : chapter.DisplayName,
                LastEpisode = lastEpisode?.Title,
                Stats = EndStats(chapter, state),
            });
        }

        private static List<EpisodeEndPanel.Stat> EndStats(
            ChapterDefinition chapter, 
            ChapterState state)
        {
            List<EpisodeEndPanel.Stat> stats = new();

            foreach (StatDefinition stat in chapter.Stats)
            {
                if (state.Stats.TryGetValue(stat.Key, out int value))
                    stats.Add(
                        new EpisodeEndPanel.Stat(
                            string.IsNullOrWhiteSpace(stat.DisplayName) 
                                ? stat.Key
                                : stat.DisplayName,
                            value));
            }

            return stats;
        }

        #endregion
    }
}
