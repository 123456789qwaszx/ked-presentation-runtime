using System.Collections.Generic;
using Ked.Progression;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 스토리 — 챕터 카드 화면. 스토리 예제의 타이틀 [처음부터]가 연다.
    // 카드는 호스트가 넘긴 시나리오(스토리 폴더의 챕터들)에서 만든다. 카드를 누르면 그 챕터를 처음부터 재생한다 —
    // 챕터는 서로 독립이다(스탯은 그 챕터의 초기값부터, 잠김 · 클리어 표시 없음).
    public sealed partial class KedUIFlow
    {
        // 스토리 예제일 때만 있다. 카드 순서 = Chapters 순서(호스트가 ChapterId로 줄 세웠다).
        private ScenarioDefinition _story;

        // 챕터 끝 카드에서 나갈 때 — 타이틀을 거쳐 챕터 카드 화면으로 돌아간다.
        private bool _reopenStory;

        private bool IsStoryExample => _story != null;

        private void OpenStory()
        {
            if (_ui.GetUI<StoryPanel>() == null)
            {
                // 화면 프리팹은 빌더의 산출물이다 — 이 화면이 더해진 뒤 빌드하지 않았으면 캔버스에 없다.
                Debug.LogError(
                    "[Ked UI] 챕터 카드 화면(StoryPanel)이 캔버스에 없다 — 빌더(Tools/Ked UI/Build Sample UI)를 다시 실행한다. " +
                    "지금은 첫 챕터를 바로 시작한다.");

                _launcher?.StartNew();
                return;
            }

            _ui.PushPanel<StoryPanel>(Presentation<StoryPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                RefreshStory(panel);
            });
        }

        private void ApplyBindings(StoryPanel panel)
        {
            AddBinding(panel,
                p => p.BackClicked += ClosePanel,
                p => p.BackClicked -= ClosePanel);

            AddBinding(panel,
                p => p.ChapterClicked += HandleStoryChapterClicked,
                p => p.ChapterClicked -= HandleStoryChapterClicked);
        }

        #region Handlers

        private void HandleStoryChapterClicked(int index)
        {
            if (_launcher == null || _story == null || index < 0 || index >= _story.Chapters.Count)
                return;

            StopFastForward();
            CloseAllPanels();

            _launcher.StartChapter(_story.Chapters[index].ChapterId);
        }

        // 스토리 예제의 챕터 끝 — 다음에 고를 것은 다른 챕터다.
        private void ReturnToStory()
        {
            _reopenStory = true;
            ReturnToTitle();
        }

        #endregion

        #region Refresh

        private void RefreshStory(StoryPanel panel)
        {
            var cards = new List<StoryChapterCard.Model>(_story.Chapters.Count);

            for (int i = 0; i < _story.Chapters.Count; i++)
            {
                ChapterDefinition chapter = _story.Chapters[i];

                cards.Add(new StoryChapterCard.Model
                {
                    Number = i + 1,
                    Name = ChapterName(chapter),
                    Info = KedUIUtil.Format(_texts, "story.card.info", SceneCount(chapter), chapter.Nodes.Count),
                });
            }

            panel.Present(new StoryPanel.Model
            {
                Subtitle = KedUIUtil.Format(_texts, "story.subtitle", cards.Count),
                Chapters = cards,
            });
        }

        private static string ChapterName(ChapterDefinition chapter) =>
            string.IsNullOrWhiteSpace(chapter.DisplayName) ? chapter.ChapterId : chapter.DisplayName;

        private static int SceneCount(ChapterDefinition chapter)
        {
            var scenes = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (EpisodeNode node in chapter.Nodes)
                scenes.Add(chapter.SceneIdOf(node.EpisodeId));

            return scenes.Count;
        }

        #endregion
    }
}
