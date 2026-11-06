using System;
using System.Collections.Generic;
using System.Text;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 14 끝 카드 — 챕터가 끝까지 갔을 때. UIPF Panel이라 재생 화면(마지막 장면)이 딤 아래 그대로 보인다.
    /// 머리글 · 이름 · 설명 한 줄 · 결과(스탯 — 없으면 감춘다)와 버튼 하나 "타이틀로" — 다음 회차가 없다.
    ///
    /// 끝을 정하는 것도, 언제 여는지도 앱이 정한다. 모델에는 내용(챕터 이름 · 마지막 에피소드 · 스탯)만 오고,
    /// 머리글 · 안내 문구 · 스탯 줄 모양은 이 화면이 제 글자 표 키로 정한다(ILocalizedView).
    /// 닫기 버튼이 없다 — 닫으면 끝난 재생 화면에 남는다(앱도 Esc로 닫지 않는다).
    /// 버튼은 한 번만 받는다(타이틀로 나가는 동안 다시 눌러도 사건이 또 나가지 않는다).
    /// </summary>
    [UIViewKey("Ked.EpisodeEndPanel")]
    public sealed class EpisodeEndPanel : UIPanel<EpisodeEndPanel.Refs>, ILocalizedView
    {
        public enum Refs
        {
            SafeAreaRoot,
            EndDim,
            EndCard,
            EndKicker,
            EndAccent,
            EndTitle,
            EndDivider,
            EndBody,
            EndResultHeading,
            EndResult,
            EndTitleReturn,
            EndTitleReturnLabel,
        }

        private const string StatSeparator = "   ·   ";
        private const int StatsPerLine = 3;

        public readonly struct Stat
        {
            public readonly string Name;
            public readonly int Value;

            public Stat(string name, int value)
            {
                Name = name;
                Value = value;
            }
        }

        public sealed class Model
        {
            /// <summary>끝난 챕터의 이름.</summary>
            public string Title;

            /// <summary>마지막으로 본 에피소드의 이름. 비면 끝까지 진행했다는 안내.</summary>
            public string LastEpisode;

            /// <summary>끝난 때의 스탯(정의 순서). 비면 결과 칸(제목 포함)을 감춘다.</summary>
            public IReadOnlyList<Stat> Stats = Array.Empty<Stat>();
        }

        public event Action TitleClicked;

        private bool _pending;

        private Model _model;
        private Localizer _texts;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.EndTitleReturn), HandleTitle);

            Styled.AddText(View.Text(Refs.EndBody), UITextRole.Body, UITextSizePolicy.Prefab);
            Styled.AddText(View.Text(Refs.EndResult), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Model model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Redraw();

            _pending = true;
            KedUIUtil.SetButton(View.Button(Refs.EndTitleReturn), true, true);
            KedUIUtil.Focus(View.Button(Refs.EndTitleReturn));
        }

        public void Relocalize(Localizer localizer)
        {
            _texts = localizer;
            Redraw();
        }

        private void Redraw()
        {
            if (_model == null)
                return;

            KedUIUtil.SetText(View.Text(Refs.EndKicker), KedUIUtil.Text(_texts, "end.chapter.kicker"));
            KedUIUtil.SetText(View.Text(Refs.EndTitle), _model.Title);
            KedUIUtil.SetText(View.Text(Refs.EndBody), string.IsNullOrWhiteSpace(_model.LastEpisode)
                ? KedUIUtil.Text(_texts, "end.chapter.body")
                : KedUIUtil.Format(_texts, "end.lastEpisode", _model.LastEpisode));

            string result = StatsText(_model.Stats);
            bool hasResult = result != null;
            KedUIUtil.SetActive(View.Text(Refs.EndResultHeading), hasResult);
            KedUIUtil.SetActive(View.Text(Refs.EndResult), hasResult);
            KedUIUtil.SetText(View.Text(Refs.EndResult), hasResult ? result : string.Empty);
        }

        // "성실성 2   ·   요령있음 1" — 한 줄에 셋까지, 넘치면 다음 줄로.
        private string StatsText(IReadOnlyList<Stat> stats)
        {
            if (stats == null || stats.Count == 0)
                return null;

            var sb = new StringBuilder();

            for (int i = 0; i < stats.Count; i++)
            {
                if (i > 0)
                    sb.Append(i % StatsPerLine == 0 ? "\n" : StatSeparator);

                sb.Append(KedUIUtil.Format(_texts, "end.stat", stats[i].Name, stats[i].Value));
            }

            return sb.ToString();
        }

        private void HandleTitle()
        {
            if (!_pending)
                return;

            _pending = false;
            KedUIUtil.SetButton(View.Button(Refs.EndTitleReturn), true, false);

            TitleClicked?.Invoke();
        }

        private void OnDisable()
        {
            _pending = false;
        }
    }
}
