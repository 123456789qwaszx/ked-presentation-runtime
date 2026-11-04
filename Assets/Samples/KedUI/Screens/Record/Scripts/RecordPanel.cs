using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 기록 — 플레이 중에 자주 여는 Panel. UIPF Page Owner — 탭 넷(저장 · 불러오기 · 팁 · 캐릭터)이 PageRoot 아래 Page 넷을
    /// UIManager.SwitchPage로 바꿔 낀다. 바깥(제목 · 탭마다 바뀌는 설명 · 뒤로)은 이 Panel이, 탭 안은 각 Page가 그린다.
    ///
    /// 나가는 버튼은 뒤로(←) 하나 — 한 층 위로(메뉴 · 타이틀). Esc도 같다.
    /// </summary>
    [UIViewKey("Ked.RecordPanel")]
    public sealed class RecordPanel : UIPanel<RecordPanel.Refs>, IUIPageOwner, ILocalizedView
    {
        // 탭 목록. 여는 길(메뉴 · 타이틀 · 차후 알림)은 이것으로 탭을 고른다.
        public enum Tab
        {
            Save,
            Load,
            Tips,
            Characters,
        }

        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,

            Back,
            BackBase,
            BackIcon,
            RecordTitle,
            RecordSubtitle,

            RecordTab_0,
            RecordTabText_0,
            RecordTab_1,
            RecordTabText_1,
            RecordTab_2,
            RecordTabText_2,
            RecordTab_3,
            RecordTabText_3,

            PageRoot,
        }

        public event Action BackClicked;
        public event Action<Tab> TabClicked;

        // Tab 순서.
        private static readonly Refs[] TabRefs = { Refs.RecordTab_0, Refs.RecordTab_1, Refs.RecordTab_2, Refs.RecordTab_3 };

        private readonly KedButton[] _tabs = new KedButton[TabRefs.Length];

        private Localizer _texts;
        private Tab _tab;

        public RectTransform PageRoot => View.Rect(Refs.PageRoot);

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.Back), () => BackClicked?.Invoke());

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

        // 저장은 재생 중에만, 저장 · 불러오기는 저장 슬롯이 있는 예제에서만.
        public void SetTabAvailable(Tab tab, bool available)
        {
            _tabs[(int)tab]?.SetInteractable(available);
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
            KedUIUtil.SetText(View.Text(Refs.RecordSubtitle), KedUIUtil.Text(_texts, SubtitleKey(_tab)));
        }

        private static string SubtitleKey(Tab tab) => tab switch
        {
            Tab.Save => "slots.save.subtitle",
            Tab.Load => "slots.load.subtitle",
            Tab.Tips => "record.tips.subtitle",
            _ => "record.characters.subtitle",
        };
    }
}
