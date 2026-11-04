using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 감상 — 타이틀에서 여는 Panel. UIPF Page Owner — 탭 둘(앨범 · CG)이 PageRoot 아래 Page 둘을 바꿔 낀다.
    /// 해금은 프레임워크 범위 밖이라 판정하지 않는다 — 잠김 · 열림은 빌드 명세가 정한 그대로 보인다.
    /// 뒤로(←) · 닫기(×) · Esc 모두 타이틀로.
    /// </summary>
    [UIViewKey("Ked.ExtraPanel")]
    public sealed class ExtraPanel : UIPanel<ExtraPanel.Refs>, IUIPageOwner, ILocalizedView
    {
        public enum Tab
        {
            Album,
            CG,
        }

        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,

            Back,
            BackBase,
            BackIcon,
            ExtraTitle,
            ExtraSubtitle,
            Close,
            CloseBase,
            CloseIcon,

            ExtraTab_0,
            ExtraTabText_0,
            ExtraTab_1,
            ExtraTabText_1,

            PageRoot,
        }

        public event Action BackClicked;
        public event Action CloseClicked;
        public event Action<Tab> TabClicked;

        // Tab 순서.
        private static readonly Refs[] TabRefs = { Refs.ExtraTab_0, Refs.ExtraTab_1 };

        private readonly KedButton[] _tabs = new KedButton[TabRefs.Length];

        private Localizer _texts;
        private Tab _tab;

        public RectTransform PageRoot => View.Rect(Refs.PageRoot);

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.Back), () => BackClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.Close), () => CloseClicked?.Invoke());

            for (int i = 0; i < TabRefs.Length; i++)
            {
                var tab = (Tab)i;
                UnityEngine.UI.Button button = View.Button(TabRefs[i]);
                KedUIUtil.OnClick(button, () => TabClicked?.Invoke(tab));

                if (button != null)
                    button.TryGetComponent(out _tabs[i]);
            }
        }

        public void SetSelectedTab(Tab tab)
        {
            _tab = tab;

            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i]?.SetSelectedLook(i == (int)tab);

            Redraw();
        }

        public void FocusTab(Tab tab)
        {
            KedButton button = _tabs[(int)tab];
            if (button != null)
                KedUIUtil.Focus(button.Button);
        }

        public void Relocalize(Localizer localizer)
        {
            _texts = localizer;
            Redraw();
        }

        private void Redraw()
        {
            string key = _tab == Tab.Album ? "extra.album.subtitle" : "extra.cg.subtitle";
            KedUIUtil.SetText(View.Text(Refs.ExtraSubtitle), KedUIUtil.Text(_texts, key));
        }
    }
}
