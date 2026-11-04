namespace Ked.Samples.UI
{
    // 감상 — 앨범 · CG. 타이틀에서만 연다. 해금은 판정하지 않는다(프레임워크 범위 밖) — Page는 명세가 고정한 그대로라 묶을 것이 없다.
    public sealed partial class KedUIFlow
    {
        private ExtraPanel.Tab _extraTab;

        private void OpenExtra()
        {
            _ui.PushPanel<ExtraPanel>(Presentation<ExtraPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                SelectExtraTab(_extraTab);
            });
        }

        private void ShowExtraPage()
        {
            ExtraPanel owner = _ui.GetUI<ExtraPanel>();

            switch (_extraTab)
            {
                case ExtraPanel.Tab.Album:
                    _ui.SwitchPage<AlbumPage>(owner, Presentation<AlbumPage>());
                    break;
                case ExtraPanel.Tab.CG:
                    _ui.SwitchPage<CGPage>(owner, Presentation<CGPage>());
                    break;
            }
        }

        private void ApplyBindings(ExtraPanel panel)
        {
            AddBinding(panel,
                p => p.BackClicked += ClosePanel,
                p => p.BackClicked -= ClosePanel);

            AddBinding(panel,
                p => p.CloseClicked += CloseAllPanels,
                p => p.CloseClicked -= CloseAllPanels);

            AddBinding(panel,
                p => p.TabClicked += SelectExtraTab,
                p => p.TabClicked -= SelectExtraTab);
        }

        #region Handlers

        private void SelectExtraTab(ExtraPanel.Tab tab)
        {
            ExtraPanel panel = _ui.GetUI<ExtraPanel>();

            _extraTab = tab;
            panel.SetSelectedTab(tab);
            ShowExtraPage();
            panel.FocusTab(tab);
        }

        #endregion
    }
}
