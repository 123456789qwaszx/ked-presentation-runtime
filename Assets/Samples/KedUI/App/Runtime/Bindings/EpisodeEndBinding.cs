using System.Collections.Generic;
using System.Text;

namespace Ked.Samples.UI
{
    // 14 끝 카드 — 회차가 끝까지 갔을 때. 끝을 정하는 것은 호스트다(KedUIHost.ShowPlaythroughEnd):
    //   연출 단독   노드가 끝까지 갔다(NodePlaythrough → IEpisodeEndView).
    //   진행 예제   챕터가 끝났다(SceneRunner → IChapterEndView) — 마지막 장면을 확정(저장)한 뒤.
    // 재생 화면(마지막 장면)을 딤 아래 두고 Panel을 쌓는다 — Panel이 있으니 재생 입력은 UI가 쥔다(SyncInput).
    // 갈 곳은 타이틀 하나다. 길은 메뉴의 "타이틀로"와 같다(ReturnToTitle) — 끊을 것이 없으니 확인은 묻지 않는다.
    // Esc로 닫지 않는다 — 닫으면 끝난 재생 화면에 남는다.
    internal sealed class EpisodeEndBinding : ScreenBinding
    {
        private const string StatSeparator = "   ·   ";
        private const int StatsPerLine = 3;

        public EpisodeEndBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            Own(flow.Require<EpisodeEndPanel>());
        }

        // 띄울 자리(재생 화면인지)는 흐름이 먼저 보았다.
        public void Open(KedPlaythroughEnd end)
        {
            Services.StopFastForward();

            // 끝은 진행 입력으로만 온다 — 열린 Panel이 있을 수 없다. 있어도 끝 카드 하나만 남긴다.
            if (Flow.HasPanel)
                Flow.CloseAllPanels();

            // 카드 아래는 재생 화면이다. UI를 숨긴 채였으면 되돌린다.
            if (Flow.IsHudHidden)
                Flow.RestoreHud();

            EpisodeEndPanel.Model model = BuildModel(end);

            Flow.PushPanel<EpisodeEndPanel>(panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(model);
            });
        }

        public override void Back(UIBase view)
        {
        }

        private void ApplyBindings(EpisodeEndPanel panel)
        {
            AddBinding(panel,
                p => p.TitleClicked += Flow.ReturnToTitle,
                p => p.TitleClicked -= Flow.ReturnToTitle);
        }

        private EpisodeEndPanel.Model BuildModel(KedPlaythroughEnd end)
        {
            bool chapter = end.Kind == KedPlaythroughEnd.EndKind.Chapter;

            string title = string.IsNullOrWhiteSpace(end.Title)
                ? Text(Services.Title?.Example?.TitleKey)
                : end.Title;

            string body = string.IsNullOrWhiteSpace(end.LastEpisode)
                ? Text(chapter ? "end.chapter.body" : "end.episode.body")
                : Format("end.lastEpisode", end.LastEpisode);

            return new EpisodeEndPanel.Model
            {
                Kicker = Text(chapter ? "end.chapter.kicker" : "end.episode.kicker"),
                Title = title,
                Body = body,
                Result = StatsText(end.Stats),
            };
        }

        // "성실성 2   ·   요령있음 1" — 한 줄에 셋까지, 넘치면 다음 줄로.
        private string StatsText(IReadOnlyList<KedPlaythroughEnd.Stat> stats)
        {
            if (stats == null || stats.Count == 0)
                return null;

            var sb = new StringBuilder();

            for (int i = 0; i < stats.Count; i++)
            {
                if (i > 0)
                    sb.Append(i % StatsPerLine == 0 ? "\n" : StatSeparator);

                sb.Append(Format("end.stat", stats[i].Name, stats[i].Value));
            }

            return sb.ToString();
        }
    }
}
