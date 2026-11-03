using System.Text;
using Ked.Progression;

namespace Ked.Samples.UI
{
    // 14 끝 카드 — 회차가 끝까지 갔을 때. 회차의 끝은 챕터의 끝이다(SceneRunner → IChapterEndView = KedUIHost → ShowChapterEnd)
    // — 마지막 장면을 확정(저장)한 뒤.
    // 재생 화면(마지막 장면)을 딤 아래 두고 Panel을 쌓는다 — Panel이 있으니 재생 입력은 UI가 쥔다(SyncInput).
    // 갈 곳은 타이틀 하나다. 길은 메뉴의 "타이틀로"와 같다(ReturnToTitle) — 끊을 것이 없으니 확인은 묻지 않는다.
    // Esc로 닫지 않는다(HandleCancel) — 닫으면 끝난 재생 화면에 남는다.
    public sealed partial class KedUIFlow
    {
        private const string EndStatSeparator = "   ·   ";
        private const int EndStatsPerLine = 3;

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

            EpisodeEndPanel.Model model = BuildEndModel(chapter, lastEpisode, state);

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

        // 글자는 콘텐츠의 이름(챕터·에피소드·스탯 표시 이름)이다 — 챕터 이름이 없으면 예제 카드 제목으로 채운다.
        private EpisodeEndPanel.Model BuildEndModel(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
        {
            string title = !string.IsNullOrWhiteSpace(chapter.DisplayName)
                ? chapter.DisplayName
                : Text(_example?.TitleKey) ?? chapter.ChapterId;

            string body = string.IsNullOrWhiteSpace(lastEpisode?.Title)
                ? Text("end.chapter.body")
                : Format("end.lastEpisode", lastEpisode.Title);

            return new EpisodeEndPanel.Model
            {
                Kicker = Text("end.chapter.kicker"),
                Title = title,
                Body = body,
                Result = EndStatsText(chapter, state),
            };
        }

        // "성실성 2   ·   요령있음 1" — 정의 순서로, 한 줄에 셋까지, 넘치면 다음 줄로.
        private string EndStatsText(ChapterDefinition chapter, ChapterState state)
        {
            if (state == null)
                return null;

            var sb = new StringBuilder();
            int shown = 0;

            foreach (StatDefinition stat in chapter.Stats)
            {
                if (!state.Stats.TryGetValue(stat.Key, out int value))
                    continue;

                if (shown > 0)
                    sb.Append(shown % EndStatsPerLine == 0 ? "\n" : EndStatSeparator);

                string name = string.IsNullOrWhiteSpace(stat.DisplayName) ? stat.Key : stat.DisplayName;
                sb.Append(Format("end.stat", name, value));
                shown++;
            }

            return shown > 0 ? sb.ToString() : null;
        }
    }
}
