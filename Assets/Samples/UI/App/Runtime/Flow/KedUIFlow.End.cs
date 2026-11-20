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
            // 스토리 예제는 끝 카드에서 챕터 카드 화면으로 돌아간다(버튼 글자도 그렇게 바꾼다 — RefreshEnd).
            if (IsStoryExample)
            {
                AddBinding(panel,
                    p => p.TitleClicked += ReturnToStory,
                    p => p.TitleClicked -= ReturnToStory);
                return;
            }

            AddBinding(panel,
                p => p.TitleClicked += ReturnToTitle,
                p => p.TitleClicked -= ReturnToTitle);
        }

        #region Handlers

        // 챕터가 끝까지 갔다(SceneRunner → IChapterEndView). 끝은 재생 화면에서만 온다 —
        // 타이틀로 나가는 중이거나 이미 타이틀이면 띄울 자리가 없다.
        private void HandleChapterEnd(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
        {
            if (!_configured || chapter == null)
                return;

            if (_exiting || !IsPlayRoot)
            {
                UnityEngine.Debug.Log("[Ked UI] 재생 화면이 아니라 끝 카드를 띄우지 않는다.");
                return;
            }

            OpenEnd(chapter, lastEpisode, state);
        }

        #endregion

        #region Refresh

        private void RefreshEnd(
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
                ReturnLabel = IsStoryExample ? Text("story.end.return") : null,
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
