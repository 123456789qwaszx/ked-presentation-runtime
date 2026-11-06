using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 기록의 저장 · 불러오기 탭이 함께 쓰는 몸. 슬롯 카드(SaveSlotCard) 6장을 격자로 보인다.
    /// 카드를 누르면 흐름이 바로 확인 창을 연다(실행 버튼은 두지 않는다). 무엇을 보일지(쪽 · 선택 · 카드)는 흐름이 정해
    /// Present로 넘긴다. 탭마다 다른 글자(안내)는 두 Page(SavePage · LoadPage)가 제 글자 표 키로 정한다 — 모델에는 뜻만 온다.
    /// </summary>
    public abstract class SaveSlotsPage : UIPage<SaveSlotsPage.Refs>, ILocalizedView
    {
        public const int SlotsPerPage = 6;

        public enum Refs
        {
            PageInfo,

            SlotGrid,
            Slot_1,
            Slot_2,
            Slot_3,
            Slot_4,
            Slot_5,
            Slot_6,

            SlotHint,
            PreviousPage,
            PreviousPageBase,
            PreviousPageIcon,
            CurrentPage,
            NextPage,
            NextPageBase,
            NextPageIcon,
        }

        // 아래 안내 줄이 무엇을 말할지.
        public enum Hint
        {
            Ready,
            NoSaves,
            Unreadable,
        }

        public sealed class Model
        {
            public Hint Hint;
            public int Page;        // 0부터
            public int PageCount;
            public bool CanPrevious;
            public bool CanNext;
            public SaveSlotCard.Model[] Cards;
            public int FocusCard = -1;
        }

        public event Action PreviousClicked;
        public event Action NextClicked;
        public event Action<int> CardClicked;

        private readonly List<SaveSlotCard> _cards = new(SlotsPerPage);

        private Model _model;
        private Localizer _texts;

        // 탭마다 다른 글자 키.
        protected abstract string ReadyHintKey { get; }

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.PreviousPage), () => PreviousClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.NextPage), () => NextClicked?.Invoke());

            Refs[] slots = { Refs.Slot_1, Refs.Slot_2, Refs.Slot_3, Refs.Slot_4, Refs.Slot_5, Refs.Slot_6 };

            for (int i = 0; i < slots.Length; i++)
            {
                SaveSlotCard card = View.Widget<SaveSlotCard>(slots[i]);
                if (card == null)
                    continue;

                card.EnsureInitialized();
                card.Index = i;
                card.Clicked += c => CardClicked?.Invoke(c.Index);

                // 카드 글자는 사용자 글자 크기를 따르지 않는다 — 크기가 정해진 카드라 커지면 겹친다. 긴 이름은 말줄임.
                _cards.Add(card);
            }
        }

        public void Present(Model model)
        {
            _model = model;
            Redraw();

            if (model != null && model.FocusCard >= 0 && model.FocusCard < _cards.Count)
                KedUIUtil.Focus(_cards[model.FocusCard].Selectable);
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

            KedUIUtil.SetText(View.Text(Refs.PageInfo),
                KedUIUtil.Format(_texts, "slots.pageInfo", KedUIUtil.TwoDigits(_model.Page + 1), KedUIUtil.TwoDigits(_model.PageCount)));
            KedUIUtil.SetText(View.Text(Refs.CurrentPage), PageNumbers(_model.Page, _model.PageCount));
            KedUIUtil.SetText(View.Text(Refs.SlotHint), Text(HintKey(_model.Hint)));

            KedUIUtil.SetButton(View.Button(Refs.PreviousPage), true, _model.CanPrevious);
            KedUIUtil.SetButton(View.Button(Refs.NextPage), true, _model.CanNext);

            for (int i = 0; i < _cards.Count; i++)
            {
                SaveSlotCard.Model card = _model.Cards != null && i < _model.Cards.Length ? _model.Cards[i] : null;

                KedUIUtil.SetActive(_cards[i], card != null);

                if (card != null)
                    _cards[i].Present(card, _texts);
            }
        }

        private string HintKey(Hint hint) => hint switch
        {
            Hint.Ready => ReadyHintKey,
            Hint.NoSaves => "slots.hint.none",
            _ => "slots.hint.unreadable",
        };

        // "1 2 3 4 5" — 지금 쪽만 청록. 쪽이 많으면 지금 쪽 둘레만.
        private static string PageNumbers(int page, int count)
        {
            const int window = 5;
            int start = Mathf.Clamp(page - window / 2, 0, Mathf.Max(0, count - window));
            int end = Mathf.Min(count, start + window);

            var parts = new List<string>(window);
            for (int p = start; p < end; p++)
            {
                string number = (p + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                parts.Add(p == page ? "<color=#13BDC5>" + number + "</color>" : number);
            }

            return string.Join("   ", parts);
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);
    }
}
