using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 스토리 — 챕터 카드가 가로로 늘어선 화면. 타이틀에서 여는 Panel이다(스토리 예제의 [처음부터]).
    /// 카드는 가로 ScrollRect 안의 HorizontalLayoutGroup에 견본(ChapterCardTemplate)을 복제해 놓는다 —
    /// 챕터가 몇 개든 흐름이 넘긴 만큼 선다. 카드를 누르면 그 챕터를 처음부터 재생한다(흐름이 한다).
    /// 잠김 · 클리어 표시는 없다 — 모든 카드가 열려 있다.
    /// 나가는 버튼은 뒤로(←) 하나 — 타이틀로. Esc도 같다.
    /// </summary>
    [UIViewKey("Ked.StoryPanel")]
    public sealed class StoryPanel : UIPanel<StoryPanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,

            Back,
            BackBase,
            BackIcon,
            StoryTitle,
            StorySubtitle,

            ChapterScroll,
            ChapterViewport,
            ChapterContent,
            ChapterCardTemplate,
            StoryEmpty,
        }

        public sealed class Model
        {
            public string Subtitle;
            public IReadOnlyList<StoryChapterCard.Model> Chapters;
        }

        public event Action BackClicked;

        /// <summary>누른 카드의 순서(0부터 — Model.Chapters의 자리).</summary>
        public event Action<int> ChapterClicked;

        private readonly List<StoryChapterCard> _cards = new();
        private StoryChapterCard _template;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.Back), () => BackClicked?.Invoke());

            _template = View.Widget<StoryChapterCard>(Refs.ChapterCardTemplate);
            if (_template != null)
                _template.gameObject.SetActive(false);
        }

        public void Present(Model model)
        {
            RectTransform content = View.Rect(Refs.ChapterContent);

            int count = model?.Chapters?.Count ?? 0;
            EnsureCards(count, content);

            for (int i = 0; i < _cards.Count; i++)
            {
                bool used = i < count;
                KedUIUtil.SetActive(_cards[i], used);

                if (used)
                    _cards[i].Present(model.Chapters[i]);
            }

            KedUIUtil.SetText(View.Text(Refs.StorySubtitle), model?.Subtitle ?? string.Empty);
            KedUIUtil.SetActive(View.Rect(Refs.StoryEmpty), count == 0);

            // 카드 너비를 계산한 뒤 맨 앞(첫 챕터)으로.
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            ScrollRect scroll = View.Component<ScrollRect>(Refs.ChapterScroll);
            if (scroll != null)
                scroll.horizontalNormalizedPosition = 0f;

            // 키보드로 바로 고를 수 있게 첫 카드에 둔다. 카드가 없으면 뒤로.
            if (count > 0)
                KedUIUtil.Focus(_cards[0].Button);
            else
                KedUIUtil.Focus(View.Button(Refs.Back));
        }

        private void EnsureCards(int count, RectTransform content)
        {
            if (_template == null || content == null)
                return;

            while (_cards.Count < count)
            {
                StoryChapterCard card = Instantiate(_template, content);
                card.name = "ChapterCard_" + _cards.Count;
                card.gameObject.SetActive(true);
                card.EnsureInitialized();
                card.Index = _cards.Count;
                card.Clicked += c => ChapterClicked?.Invoke(c.Index);

                _cards.Add(card);
            }
        }
    }
}
