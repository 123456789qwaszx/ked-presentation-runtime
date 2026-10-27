using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 14 끝 카드 — 회차가 끝까지 갔을 때. 끝을 정하는 것은 호스트다(KedUIHost.ShowPlaythroughEnd):
    //   연출 단독   노드가 끝까지 갔다(NodePlaythrough.Completed).
    //   진행 예제   챕터가 끝났다(SceneRunner.ChapterEnded) — 마지막 장면을 확정(저장)한 뒤.
    // 재생 화면(마지막 장면)을 딤 아래 두고 Panel을 쌓는다 — Panel이 있으니 재생 입력은 UI가 쥔다(SyncInput).
    // 갈 곳은 타이틀 하나다. 길은 메뉴의 "타이틀로"와 같다(ReturnToTitle) — 끊을 것이 없으니 확인은 묻지 않는다.
    public sealed partial class KedUIFlow
    {
        private const string StatSeparator = "   ·   ";
        private const int StatsPerLine = 3;

        private void SubscribeEnd()
        {
            _end.TitleClicked += ReturnToTitle;
        }

        public void ShowPlaythroughEnd(KedPlaythroughEnd end)
        {
            if (!_configured || end == null)
                return;

            // 끝은 재생 화면에서만 온다. 타이틀로 나가는 중이거나 이미 타이틀이면 띄울 자리가 없다.
            if (_exiting || !IsPlayRoot)
            {
                Debug.Log("[Ked UI] 재생 화면이 아니라 끝 카드를 띄우지 않는다.");
                return;
            }

            StopFastForward();

            // 끝은 진행 입력으로만 온다 — 열린 Panel이 있을 수 없다. 있어도 끝 카드 하나만 남긴다.
            if (_ui.HasPanel)
                CloseAllPanels();

            // 카드 아래는 재생 화면이다. UI를 숨긴 채였으면 되돌린다.
            if (_play.IsHudHidden)
                _play.SetHudHidden(false);

            EpisodeEndPanel.Model model = EndModel(end);
            PushPanel<EpisodeEndPanel>(panel => panel.Present(model));
        }

        private EpisodeEndPanel.Model EndModel(KedPlaythroughEnd end)
        {
            bool chapter = end.Kind == KedPlaythroughEnd.Unit.Chapter;

            string title = string.IsNullOrWhiteSpace(end.Title)
                ? Text(_titlePort?.Example?.TitleKey)
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
