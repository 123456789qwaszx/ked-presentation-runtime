using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 저장·불러오기가 함께 쓰는 슬롯 카드(시안 05·06의 save_slot). UIPF 위젯 — Page(SavePage · LoadPage)의 Ref 하나에
    /// 붙은 UIBase로, UIManager에 등록하지 않는다. 카드 안의 이름은 카드 안에서만 찾는다.
    ///
    /// 그림 창: 지금 저장은 화면을 찍지 않는다. 그래서 save_slot 그림의 밝은 자리(알려진 자리 그림)를 그대로 두고,
    /// 그 위에 실제 메타데이터(장면·플레이 시간)를 적는다. 화면 캡처는 별도 작업이다.
    /// 모델에는 뜻(번호 · 저장 시각 · 플레이 초 · 새 칸인가)만 온다 — 글자와 형식은 카드가 정한다. 글자 표는 Page(SaveSlotsPage)가 넘긴다.
    /// </summary>
    [UIViewKey("Ked.SaveSlotCard")]
    public sealed class SaveSlotCard : UIBase<SaveSlotCard.Refs>
    {
        public enum Refs
        {
            CardButton,
            CardThumbnail,
            CardChapter,
            CardPlayTime,
            CardNumber,
            CardNumberText,
            CardTitle,
            CardDate,
            CardEmpty,
            CardEmptyHint,
            CardSelected,
        }

        public sealed class Model
        {
            public bool Filled;
            public int Number;              // 1부터
            public string Title;            // 콘텐츠 글자(저장 이름 또는 그 줄의 대사)
            public DateTime SavedAtUtc;     // 모르면 DateTime.MinValue
            public string Chapter;
            public int PlaySeconds;
            public bool IsNewSlot;          // 빈 칸 중 "새로 저장"할 칸
            public bool Selectable;
            public bool Selected;
        }

        // 누르면 흐름이 바로 확인 창을 연다.
        public event Action<SaveSlotCard> Clicked;

        public int Index { get; set; }

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.CardButton), () => Clicked?.Invoke(this));
        }

        public void Present(Model model, Localizer texts)
        {
            bool filled = model.Filled;

            KedUIUtil.SetActive(View.Rect(Refs.CardNumber), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardTitle), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardDate), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardChapter), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardPlayTime), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardEmpty), !filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardEmptyHint), !filled && model.IsNewSlot);

            KedUIUtil.SetText(View.Text(Refs.CardNumberText), filled ? KedUIUtil.TwoDigits(model.Number) : string.Empty);
            KedUIUtil.SetText(View.Text(Refs.CardTitle), model.Title);
            KedUIUtil.SetText(View.Text(Refs.CardDate), FormatSavedAt(model.SavedAtUtc));
            KedUIUtil.SetText(View.Text(Refs.CardChapter), model.Chapter);
            KedUIUtil.SetText(View.Text(Refs.CardPlayTime), KedUIUtil.Format(texts, "slots.playTime", FormatPlayTime(model.PlaySeconds)));
            KedUIUtil.SetText(View.Text(Refs.CardEmpty), KedUIUtil.Text(texts, "slots.empty"));
            KedUIUtil.SetText(View.Text(Refs.CardEmptyHint), model.IsNewSlot ? KedUIUtil.Text(texts, "slots.empty.hint") : string.Empty);

            KedUIUtil.SetButton(View.Button(Refs.CardButton), true, model.Selectable);
            KedUIUtil.SetActive(View.Rect(Refs.CardSelected), model.Selected);
        }

        public Selectable Selectable => View.Button(Refs.CardButton);

        private static string FormatSavedAt(DateTime utc)
        {
            return utc == DateTime.MinValue
                ? string.Empty
                : utc.ToLocalTime().ToString("yyyy.MM.dd  HH:mm", CultureInfo.InvariantCulture);
        }

        private static string FormatPlayTime(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
            return ((int)time.TotalHours).ToString("00") + ":" + time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00");
        }
    }
}
