using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 기록 · 팁 탭. 왼쪽 목록에서 고르면 오른쪽에 그 팁의 본문이 보인다.
    /// 팁 내용은 빌드 명세의 고정 내용이라 프리팹에 다 들어 있다 — 여기서는 고른 줄을 칠하고 그 본문만 켠다.
    /// </summary>
    [UIViewKey("Ked.TipsPage")]
    public sealed class TipsPage : UIPage<TipsPage.Refs>
    {
        public const int TipCount = 7;

        public enum Refs
        {
            TipList,
            TipRow_0,
            TipRow_1,
            TipRow_2,
            TipRow_3,
            TipRow_4,
            TipRow_5,
            TipRow_6,

            TipDetail,
            TipDetail_0,
            TipDetail_1,
            TipDetail_2,
            TipDetail_3,
            TipDetail_4,
            TipDetail_5,
            TipDetail_6,
        }

        public sealed class Model
        {
            public int Selected;
        }

        public event Action<int> TipClicked;

        private static readonly Refs[] RowRefs =
            { Refs.TipRow_0, Refs.TipRow_1, Refs.TipRow_2, Refs.TipRow_3, Refs.TipRow_4, Refs.TipRow_5, Refs.TipRow_6 };

        private static readonly Refs[] DetailRefs =
            { Refs.TipDetail_0, Refs.TipDetail_1, Refs.TipDetail_2, Refs.TipDetail_3, Refs.TipDetail_4, Refs.TipDetail_5, Refs.TipDetail_6 };

        private readonly KedButton[] _rows = new KedButton[TipCount];
        private readonly RectTransform[] _details = new RectTransform[TipCount];

        protected override void OnInitialize()
        {
            for (int i = 0; i < TipCount; i++)
            {
                int index = i;
                Button button = View.Button(RowRefs[i]);
                KedUIUtil.OnClick(button, () => TipClicked?.Invoke(index));

                if (button != null)
                    button.TryGetComponent(out _rows[i]);

                _details[i] = View.Rect(DetailRefs[i]);
            }
        }

        public void Present(Model model)
        {
            for (int i = 0; i < TipCount; i++)
            {
                _rows[i]?.SetSelectedLook(i == model.Selected);
                KedUIUtil.SetActive(_details[i], i == model.Selected);
            }
        }

        public Selectable FirstSelectable => _rows[0]?.Button;
    }
}
