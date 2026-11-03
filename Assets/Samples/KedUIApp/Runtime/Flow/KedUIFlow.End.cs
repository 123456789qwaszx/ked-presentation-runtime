using System.Collections.Generic;
using Ked.Progression;

namespace Ked.Samples.UI
{
    // 14 끝 카드 — 회차가 끝까지 갔을 때. 회차의 끝은 챕터의 끝이다(SceneRunner → IChapterEndView = KedUIHost → ShowChapterEnd)
    // — 마지막 장면을 확정(저장)한 뒤.
    // 재생 화면(마지막 장면)을 딤 아래 두고 Panel을 쌓는다 — Panel이 있으니 재생 입력은 UI가 쥔다(SyncInput).
    // 갈 곳은 타이틀 하나다. 길은 메뉴의 "타이틀로"와 같다(ReturnToTitle) — 끊을 것이 없으니 확인은 묻지 않는다.
    // Esc로 닫지 않는다(HandleCancel) — 닫으면 끝난 재생 화면에 남는다.
    // 여기서는 내용(챕터 이름 · 마지막 에피소드 · 스탯)만 모은다. 글자와 줄 모양은 화면(EpisodeEndPanel)이 정한다.
    public sealed partial class KedUIFlow
    {
        private readonly EpisodeEndPanel _endPanel;

        // 띄울 자리(재생 화면인지)는 ShowChapterEnd가 먼저 보았다.
        private void OpenEnd(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
        {
            StopFastForward();

            // 끝은 진행 입력으로만 온다 — 열린 Panel이 있을 수 없다. 있어도 끝 카드 하나만 남긴다.
            if (_ui.HasPanel)
                CloseAllPanels();

            // 카드 아래는 재생 화면이다. UI를 숨긴 채였으면 되돌린다.
            if (IsHudHidden)
                RestoreHud();

            var model = new EpisodeEndPanel.Model
            {
                Title = string.IsNullOrWhiteSpace(chapter.DisplayName) ? chapter.ChapterId : chapter.DisplayName,
                LastEpisode = lastEpisode?.Title,
                Stats = EndStats(chapter, state),
            };

            PushPanel<EpisodeEndPanel>(panel =>
            {
                BindView(panel, ApplyEndBindings);
                panel.Present(model);
            });
        }

        private void ApplyEndBindings(EpisodeEndPanel panel)
        {
            AddBinding(panel,
                p => p.TitleClicked += ReturnToTitle,
                p => p.TitleClicked -= ReturnToTitle);
        }

        // 정의 순서로, 값이 있는 스탯만. 이름이 없으면 키.
        private static List<EpisodeEndPanel.Stat> EndStats(ChapterDefinition chapter, ChapterState state)
        {
            var stats = new List<EpisodeEndPanel.Stat>();

            if (state == null)
                return stats;

            foreach (StatDefinition stat in chapter.Stats)
            {
                if (state.Stats.TryGetValue(stat.Key, out int value))
                    stats.Add(new EpisodeEndPanel.Stat(string.IsNullOrWhiteSpace(stat.DisplayName) ? stat.Key : stat.DisplayName, value));
            }

            return stats;
        }
    }
}
