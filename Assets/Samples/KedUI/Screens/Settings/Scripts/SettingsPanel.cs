using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 07 설정(+ 11 대사 · 12 재생 · 13 화면·조작). UIPF Panel이자 Page Owner — 탭 넷이 PageRoot 아래의 Page 넷을
    /// UIManager.SwitchPage로 바꿔 낀다. 바깥(제목·탭·기본값 복원·적용 안내)은 이 Panel이, 탭 안은 각 Page가 그린다.
    /// </summary>
    [UIViewKey("Ked.SettingsPanel")]
    public sealed class SettingsPanel : UIPanel<SettingsPanel.Refs>, IUIPageOwner
    {
        // 탭 목록. 화면 상태(SettingsState)도 이것으로 탭을 고른다.
        public enum Tab
        {
            Audio,
            Text,
            Playback,
            Display,
        }

        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,
            SettingsSurface,
            SettingsTitle,
            SettingsClose,
            SettingsCloseBase,
            SettingsCloseIcon,

            SettingsTabs,
            SettingsTab_0,
            SettingsTabText_0,
            SettingsTab_1,
            SettingsTabText_1,
            SettingsTab_2,
            SettingsTabText_2,
            SettingsTab_3,
            SettingsTabText_3,

            PageRoot,

            ApplyNote,
            ResetSettings,
            ResetSettingsLabel,
        }

        public event Action CloseClicked;
        public event Action<Tab> TabClicked;
        public event Action ResetClicked;

        // Tab 순서.
        private static readonly Refs[] TabRefs = { Refs.SettingsTab_0, Refs.SettingsTab_1, Refs.SettingsTab_2, Refs.SettingsTab_3 };

        private readonly KedButton[] _tabs = new KedButton[TabRefs.Length];

        public RectTransform PageRoot => View.Rect(Refs.PageRoot);

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.SettingsClose), () => CloseClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.ResetSettings), () => ResetClicked?.Invoke());

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
            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i]?.SetSelectedLook(i == (int)tab);
        }

        /// <summary>기본값 복원이 지금 탭에 되돌릴 값을 갖는가.</summary>
        public void SetResetAvailable(bool available)
        {
            KedUIUtil.SetButton(View.Button(Refs.ResetSettings), true, available);
        }

        public void FocusTab(Tab tab)
        {
            int index = (int)tab;
            if (index >= 0 && index < _tabs.Length && _tabs[index] != null)
                KedUIUtil.Focus(_tabs[index].Button);
        }
    }
}
